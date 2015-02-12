// -----------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//    Copyright 2014 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick.Console.Client.Auth;
using ImageMagick.Console.Client.Utilities;

using Microsoft.Azure.Batch.Apps;

namespace ImageMagick.Console.Client
{
    using Microsoft.WindowsAzure;
    using Console = System.Console;

    public static class Program
    {

        /// <summary>
        /// Program entry point.Run main application.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
            ProgramCore(".").Wait();
            return Exit(0);
        }

        /// <summary>
        /// Returns from the application, pausing for user input
        /// </summary>
        /// <param name="exitCode"></param>
        /// <returns></returns>
        private static int Exit(int exitCode)
        {
            // Pause the program  
            Console.WriteLine("\nCompleted. Press Enter to exit.");
            Console.ReadLine();

            return exitCode;
        }

        /// <summary>
        /// Program core. Obtain credentials, submit images for processing and save output
        /// </summary>
        /// <param name="outputDirectory"></param>
        private static async Task ProgramCore(string outputDirectory)
        {
            TokenCloudCredentials authToken = CreateAuthenticationToken();

            var batchAppsServiceEndpoint = ConfigurationManager.AppSettings["BatchAppsServiceUrl"];

            // Create Batch Apps Client 
            using (var client = new BatchAppsClient(batchAppsServiceEndpoint, authToken))
            {
                var userInputFilePaths = new List<UserFile>(new[]
                {
                    new UserFile(client, @"C:\Users\Jussi\Desktop\brain.nii"),
                    new UserFile(client, "resliced.nii")
                });

                // Creates a list of Userfiles needed for submitting a job.
                //userInputFilePaths = new UserFileBuilder(client).LoadFromDirectory(sourceDirectory).ToList();

                var job = await SubmitJob(client, userInputFilePaths);

                await MonitorJob(job, outputDirectory);
            }
        }

        private static TokenCloudCredentials CreateAuthenticationToken()
        {
            // parse in principal keys needed for authorization 
            var user = AuthenticationUserCredential.Parse(
                ConfigurationManager.AppSettings["UnattendedAccountId"],
                ConfigurationManager.AppSettings["UnattendedAccountKey"]);

            // Create AAD token 
            var authToken = TokenCloudCredentialsUtils.GetAuthenticationToken(user);
            return authToken;
        }


        /// <summary>
        /// Submits a job to the service deployment.  
        /// </summary>
        /// <param name="client">BatchApps client</param>
        /// <param name="userInputFilePaths">The list of UserFiles you want to process.</param>
        /// <param name="outputDirectory">Folder to store the output files in</param>
        /// <returns></returns>
        private static async Task<IJob> SubmitJob(BatchAppsClient client, List<UserFile> userInputFilePaths)
        {
            var jobSpec = CreateImageMagickResizeJobSpec(userInputFilePaths);

            Console.WriteLine("-----Submitting Job-----");

            var job = await client.Jobs.SubmitAsync(jobSpec);

            return job;
        }

        private static JobSubmission CreateImageMagickResizeJobSpec(List<UserFile> userInputFilePaths)
        {
            return new JobSubmission
            {
                Name = "AzureBatchNiftiProcessing Test", // Name of the Job
                Type = "AzureBatchNiftiProcessing", // The Task Splitter will split a job based on the Type you specify. 
                RequiredFiles = userInputFilePaths, // The files that are needed for the Job
                Parameters = new Dictionary<string, string>(),
                InstanceCount = userInputFilePaths.Count, // 1 Virtual Machine instance per file for optimal performance
            };
        }

        private static async Task MonitorJob(IJob jobResponse, string outputDirectory)
        {
            Console.Write("\n-----Starting Job-----\n");

            // Downloads all of the task outputs as the job is running.
            bool jobSucceeded = await RunJob(jobResponse, outputDirectory);

            if (jobSucceeded)
            {
                Console.Write("-----Job successfully completed-----");

                /*// Uncomment this to download the final job output.                   
                   Console.Write("\n\n-----Downloading Final Job Output-----\n");
                   await DownloadJobOutput(job, outputDirectory);                
                */
            }
            else
            {
                Console.Error.WriteLine("\n\n-----------Job has failed-------------");
                await PrintJobLogs(jobResponse);
            }
        }

        /// <summary>
        /// Downloads the task outputs as they complete.
        /// </summary>
        /// <param name="job">Job we want to download</param>
        /// <param name="outputDirectory">Folder to store the output files in</param>
        /// <remarks>If the Job fails it will print out all the log messages.</remarks>
        private static async Task<bool> RunJob(IJob job, string outputDirectory)
        {
            Console.WriteLine("Job Id: " + job.Id + "\n");

            // Used for downloading tasks.
            var downloadedTasks = new List<int>();

            // Wait for the job to be completed.
            while (job.Status == JobStatus.NotStarted || job.Status == JobStatus.InProgress)
            {
                Thread.Sleep(5000);

                // Refreshes the current job's progress. 
                await job.UpdateAsync();

                PrintJobStatus(job);

                // If the job fails print out all the logs.
                if (job.HasFailed())
                {
                    return false;
                }

                await DownloadOutputsOfUndownloadedTasks(job, outputDirectory, downloadedTasks);
            }

            return job.Status == JobStatus.Complete;
        }

        private static async Task DownloadOutputsOfUndownloadedTasks(IJob job, string outputDirectory, List<int> downloadedTasks)
        {
            // Gets all the intermediate outputs of the job.
            var taskOutputs = await job.GetIntermediateOutputsAsync();

            var taskOutputsToProcess = taskOutputs
                .Where(output => output.Kind == OutputKind.TaskOutput);

            foreach (var output in taskOutputsToProcess)
            {
                // Download only outputs we haven't previously downloaded
                if (!downloadedTasks.Contains(output.TaskId))
                {
                    // Add the task to 
                    downloadedTasks.Add(output.TaskId);
                    Console.WriteLine("Downloading: " + output.Name);

                    // Downloads the task to your local machine.
                    var task = await job.GetFileAsync(output.Name);

                    string outputFileName = Path.Combine(outputDirectory, output.Name);

                    // Saves the stream 
                    await task.SaveToFileAsync(outputFileName);
                }
            }
        }

        private static async Task PrintJobLogs(IJob job)
        {
            Console.Error.WriteLine("----------------logs------------------");

            foreach (var log in await job.GetLogAsync())
            {
                Console.Error.WriteLine("TaskId:    " + log.TaskId);
                Console.Error.WriteLine("Timestamp: " + log.Timestamp);
                Console.Error.WriteLine("Text:      " + log.Text);
                Console.Error.WriteLine("-------------------------------------");
            }
        }

        private static void PrintJobStatus(IJob job)
        {
            if (job.Status == JobStatus.NotStarted)
            {
                Console.WriteLine("Waiting for compute resource...");
            }
            else
            {
                Console.WriteLine("Percent complete: " + job.PercentComplete);
            }
        }


        /// <summary>
        ///  Streams the task output to a file. 
        /// </summary>
        /// <param name="job">Job we want to download</param>
        /// <param name="outputDirectory">Folder to store the output files in</param>
        /// <returns></returns>
        private static async Task DownloadJobOutput(IJob job, string outputDirectory)
        {
            // Gets the created file name of at the end of job.
            var jobFinalOutputName = await job.GetOutputFileNameAsync();

            // Gets the file stream of the final job output.
            var stream = await job.GetOutputAsync();

            string outputFileName = Path.Combine(outputDirectory, jobFinalOutputName);
            await stream.SaveToFileAsync(outputFileName);
        }
    }
}
