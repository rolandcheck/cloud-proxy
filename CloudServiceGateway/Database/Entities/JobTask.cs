using System;

namespace CloudServiceGateway.Database.Entities
{
    public class JobTask : BaseEntity
    {
        public TaskType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string RequestId { get; set; }
    }
}