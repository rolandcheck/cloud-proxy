using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudServiceGateway.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace CloudServiceGateway
{
    public class CustomLoadBalancer : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _get;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CustomLoadBalancer> _logger;

        public CustomLoadBalancer(Func<Task<List<Service>>> get, IServiceProvider serviceProvider)
        {
            _get = get;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILogger<CustomLoadBalancer>>();
        }
            
        public async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
        {
            var hosts = await _get();
            var context = _serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            
            // todo: consider using body lenght in job weight
            var bodyLength = context.Response.ContentLength;

            var service = await Choose(hosts);
            
            //var service = hosts.First();
            var hostAndPort = service.HostAndPort;

            
            


            _logger.LogWarning($"in {hostAndPort.Scheme}://{hostAndPort.DownstreamHost}: {hostAndPort.DownstreamPort}");

            var baseUri = new UriBuilder(hostAndPort.DownstreamHost);
            
            var path = httpContext.Request.Path.ToUriComponent();

            baseUri.Path = path;
                
            var result = new OkResponse<ServiceHostAndPort>(hostAndPort);
            return result;
        }
        
        private async Task<Service> Choose(List<Service> hosts)
        {
            // fetch data from all hosts about their load
            var statuses = await GetStatuses(hosts);

            var service = ChooseInternal(statuses);

            return service;
        }

        private Service ChooseInternal(IReadOnlyCollection<ServerStatus> statuses)
        {
            return statuses.MaxBy(ComplexFormula).First().Service;
        }

        private int ComplexFormula(ServerStatus arg)
        {
            const int cpuMultiplier = 3;
            const int ramMultiplier = 1;
            const int storageMultiplier = 5;
            const int taskCountMultiplier = 10;

            return arg.LoadData.CpuUsage * cpuMultiplier +
                   arg.LoadData.RamUsage * ramMultiplier +
                   arg.LoadData.StorageUsage * storageMultiplier +
                   arg.Tasks.Count * taskCountMultiplier +
                   arg.Tasks.Sum(x => x.Weight);
        }

        private async Task<IReadOnlyCollection<ServerStatus>> GetStatuses(List<Service> hosts)
        {
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient();

            var statuses = new List<ServerStatus>();
            
            foreach (var service in hosts)
            {
                var uriBuilder = new UriBuilder
                {
                    Host = service.HostAndPort.DownstreamHost,
                    Port = service.HostAndPort.DownstreamPort,
                    Scheme = service.HostAndPort.Scheme,
                    Path = "/custom-health"
                };

                ServiceLoadData loadData;

                try
                {
                    loadData = await client.GetFromJsonAsync<ServiceLoadData>(uriBuilder.Uri).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    var rand = new Random();
                    loadData = new ServiceLoadData
                    {
                        CpuUsage = rand.Next(0, 100),
                        RamUsage = rand.Next(0, 100),
                        StorageUsage = rand.Next(0, 100),
                    };
                }

                
                
                var serverStatus = new ServerStatus()
                {
                    Service = service,
                    LoadData = loadData,
                    Tasks = await LoadTasks(service),
                };
                statuses.Add(serverStatus);
            }

            return statuses;
        }

        private Task<List<ServerTask>> LoadTasks(Service service)
        {
            return Task.FromResult(new List<ServerTask>());
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
            _logger.LogWarning($"release: {hostAndPort.Scheme}://{hostAndPort.DownstreamHost}: {hostAndPort.DownstreamPort}" );
        }
    }
}
