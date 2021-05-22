using System.Collections.Generic;

namespace CloudServiceGateway.Configuration
{
    public class ServerConf
    {
        public IReadOnlyCollection<HostConf> DownstreamHostAndPorts { get; set; }
    }
}