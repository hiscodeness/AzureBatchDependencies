using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Azure.Batch.Apps.Cloud;

namespace AzureBatchNiftiProcessing
{
    /// <summary>
    /// Processes a task.
    /// </summary>
    public class AzureBatchNiftiProcessingTaskProcessor : ParallelTaskProcessor
    {
        /// <summary>
        /// Executes the external process for processing the task
        /// </summary>
        /// <param name="task">The task to be processed.</param>
        /// <param name="settings">Contains information about the processing request.</param>
        /// <returns>The result of task processing.</returns>
        protected override TaskProcessResult RunExternalTaskProcess(ITask task,
            TaskExecutionSettings settings)
        {
            switch (task.TaskId)
            {
                case TaskIds.Reslice:
                    return RunResliceTask(task);
                case TaskIds.SkullStrip:
                    return RunSkullStripTask(task);
            }

            throw new ArgumentException(string.Format("No such task: {0}.", task), "task");
        }

        private TaskProcessResult RunResliceTask(ITask task)
        {
            var originalInputFileName = task.RequiredFiles[0].Name;
            var inputFile = Path.Combine(LocalStoragePath, originalInputFileName);
            var outputFile = Path.Combine(LocalStoragePath, "resliced.nii");

            var process = new ExternalProcess
            {
                CommandPath = ExecutablePath(@"mri-processing\niftiInit.bat"),
                Arguments =
                    string.Format("\"{0}\" \"{1}\"", inputFile.Replace(".nii", string.Empty),
                        outputFile.Replace(".nii", string.Empty)),
                WorkingDirectory = ExecutablePath("mri-processing")
            };
            process.EnvironmentVariables["PATH"] = string.Format("{0};{1}", "%PATH%", ExecutablePath(@"mri-processing\bin"));

            var processOutput = process.Run();
            return new TaskProcessResult
            {
                OutputFiles =
                    new[]
                    {new TaskOutputFile {FileName = outputFile, Kind = TaskOutputFileKind.Output}},
                ProcessorOutput = "--- STDOUT --- " + processOutput.StandardOutput + "--- STDERR --- " + processOutput.StandardError,
                Success =
                    processOutput.ExitCode == 0
                        ? TaskProcessSuccess.Succeeded
                        : TaskProcessSuccess.PermanentFailure
            };
        }

        private TaskProcessResult RunSkullStripTask(ITask task)
        {
            var originalInputFileName = task.RequiredFiles[0].Name;

            // inputFile should be the output from RunResliceTask, not the original input file!!!
            var inputFile = Path.Combine(LocalStoragePath, originalInputFileName);
            var outputFile = Path.Combine(LocalStoragePath, "skull-stripped.nii");
            
            var process = new ExternalProcess
            {
                CommandPath = ExecutablePath(@"mri-processing\skullStrip.bat"),
                Arguments =
                    string.Format("\"{0}\" \"{1}\"", inputFile.Replace(".nii", string.Empty),
                        outputFile.Replace(".nii", string.Empty)),
                WorkingDirectory = ExecutablePath("mri-processing")
            };
            process.EnvironmentVariables["PATH"] = string.Format("{0};{1}", "%PATH%", ExecutablePath(@"mri-processing\bin"));

            var processOutput = process.Run();
            return new TaskProcessResult
            {
                OutputFiles =
                    new[] { new TaskOutputFile { FileName = outputFile, Kind = TaskOutputFileKind.Output } },
                ProcessorOutput = "--- STDOUT --- " + processOutput.StandardOutput + "--- STDERR --- " + processOutput.StandardError,
                Success =
                    processOutput.ExitCode == 0
                        ? TaskProcessSuccess.Succeeded
                        : TaskProcessSuccess.PermanentFailure
            };
        }

        protected override JobResult RunExternalMergeProcess(
            ITask mergeTask,
            TaskExecutionSettings settings)
        {
            var completionFile = LocalPath("completion.txt");

            File.WriteAllText(completionFile, "done");

            return new JobResult
            {
                OutputFile = completionFile
            };
        }
    }
}
