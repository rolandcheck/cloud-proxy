using System.Collections.Generic;

namespace CloudServiceGateway.Configuration
{
    public class RoutesConf
    {
        public IReadOnlyCollection<ServerConf> Routes { get; set; }
    }
}