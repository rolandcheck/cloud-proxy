using System.Collections.Generic;
using Ocelot.Values;

namespace CloudServiceGateway.Models
{
    public class ServerStatus
    {
        public Service Service { get; set; }
        public ServiceLoadData LoadData { get; set; }
        public List<ServerTask> Tasks { get; set; }
    }
}