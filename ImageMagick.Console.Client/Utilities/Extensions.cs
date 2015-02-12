// -----------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Microsoft">
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

using System.IO;
using System.Threading.Tasks;

using Microsoft.Azure.Batch.Apps;

namespace ImageMagick.Console.Client.Utilities
{
    public static class Extensions
    {
        /// <summary>
        /// Checks the job Status to see if the job has failed.
        /// </summary>
        /// <param name="jobResponse">Job</param>
        /// <returns>True. If the job fails</returns>
        public static bool HasFailed(this IJob jobResponse)
        {
            return jobResponse.Status == JobStatus.Error ||
                jobResponse.Status == JobStatus.Cancelled ||
                jobResponse.Status == JobStatus.OnHold ||
                jobResponse.Status == JobStatus.Cancelling;
        }

        /// <summary>
        /// Creates a file from a Stream in the a directory you specify.
        /// Overwrites any existing file with the same filename
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="filePath">FilePath of where you want to create the file</param>
        public static async Task SaveToFileAsync(this Stream stream, string filePath)
        {
            using (var output = new FileStream(filePath.Trim(), FileMode.Create))
            {
                await stream.CopyToAsync(output);
            }
        }
    }
}
