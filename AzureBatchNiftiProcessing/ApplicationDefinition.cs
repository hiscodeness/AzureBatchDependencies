using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Batch.Apps.Cloud;

namespace AzureBatchNiftiProcessing
{
    public class ApplicationDefinition
    {
        public static readonly CloudApplication Application = new ParallelCloudApplication
            {
                ApplicationName = "AzureBatchNiftiProcessing",
                JobType = "AzureBatchNiftiProcessing",
                JobSplitterType = typeof(AzureBatchNiftiProcessingJobSplitter),
                TaskProcessorType = typeof(AzureBatchNiftiProcessingTaskProcessor)
            };

    }
}
