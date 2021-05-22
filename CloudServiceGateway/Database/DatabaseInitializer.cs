using System;
using System.Linq;
using CloudServiceGateway.Configuration;
using CloudServiceGateway.Database.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CloudServiceGateway.Database
{
    public static class DatabaseInitializer
    {
        public static void Initialize(IServiceProvider provider, IOptions<RoutesConf> configuration)
        {
            using var serviceScope = provider.CreateScope();
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<CsContext>();
            dbContext.Database.EnsureCreated();
            
            if (!dbContext.Servers.Any())
            {
                var servers = configuration.Value.Routes.SelectMany(x => x.DownstreamHostAndPorts).Select(x =>
                    new Server{Host = x.Host, Scheme = x.Scheme, Port = x.Port});
                
                dbContext.AddRange(servers);
                dbContext.SaveChanges();
            }
        }
    }
}