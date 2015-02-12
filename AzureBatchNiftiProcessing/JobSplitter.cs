using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Batch.Apps.Cloud;

namespace AzureBatchNiftiProcessing
{
    /// <summary>
    /// Splits a job into tasks.
    /// </summary>
    public class AzureBatchNiftiProcessingJobSplitter : JobSplitter
    {
        /// <summary>
        /// Splits a job into more granular tasks to be processed in parallel.
        /// </summary>
        /// <param name="job">The job to be split.</param>
        /// <param name="settings">Contains information and services about the split request.</param>
        /// <returns>A sequence of tasks to be run on compute nodes.</returns>
        protected override IEnumerable<TaskSpecifier> Split(IJob job, JobSplitSettings settings)
        {
            var reorientTask = new TaskSpecifier
            {
                TaskId = TaskIds.Reslice,
                RequiredFiles = job.Files.Take(1).ToList(),
                Parameters = job.Parameters,                
            };

            var myFileSpecifier = new MyFileSpecifier
            {
                Name = "resliced.nii"
            };

            var skullStripTask = new TaskSpecifier
            {
                TaskId = TaskIds.SkullStrip,
                RequiredFiles = new IFileSpecifier[]{myFileSpecifier}.ToList(),
                Parameters = job.Parameters,
                DependsOn = TaskDependency.OnId(TaskIds.Reslice)
            };

            return new List<TaskSpecifier> {reorientTask, skullStripTask};
        }
    }

    public class MyFileSpecifier : IFileSpecifier
    {
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public string OriginalPath { get; set; }
        public string Hash { get; set; }
    }
}
