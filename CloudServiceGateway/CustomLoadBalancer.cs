using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudServiceGateway.Database;
using CloudServiceGateway.Database.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
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

        public async Task<Response<ServiceHostAndPort>> Lease(HttpContext context)
        {
            // todo: consider using body lenght in job weight
            var bodyLength = context.Response.ContentLength;

            var dbContext = _serviceProvider.GetRequiredService<CsContext>();

            var servers = dbContext.Servers.ToDictionary(x=>x, x=> new List<JobTask>());

            foreach (var serverJobTask in dbContext.ServerJobTasks
                .Where(x => !x.Task.EndTime.HasValue)
                .Include(x => x.Server)
                .Include(x => x.Task))
            {
                servers[serverJobTask.Server].Add(serverJobTask.Task);
            }
            
            
            var server = await ChooseServer(servers) ?? dbContext.Servers.FirstOrDefault();
            
            var data = GetRequestId();
            
            var type = GetRequestType(context);
            var task = new JobTask()
            {
                StartTime = DateTime.Now,
                Type = type,
                RequestId = data,
            };
            var link = new ServerJobTask
            {
                Task = task,
                Server = server,
            };

            await dbContext.Tasks.AddAsync(task);
            await dbContext.ServerJobTasks.AddAsync(link);
            await dbContext.SaveChangesAsync();

            await Task.Delay(5000);


            var hostAndPort = new ServiceHostAndPort(server.Host, server.Port, server.Scheme);

            var message = $"in {hostAndPort.Scheme}://{hostAndPort.DownstreamHost}: {hostAndPort.DownstreamPort}";
            _logger.LogInformation(message);


            var result = new OkResponse<ServiceHostAndPort>(hostAndPort);
            return result;
        }

        private string GetRequestId()
        {
            var requestScoped = _serviceProvider.GetRequiredService<IRequestScopedDataRepository>();
            var data = requestScoped.Get<string>("RequestId").Data;
            return data;
        }

        private static TaskType GetRequestType(HttpContext context)
        {
            if (context.Request.Path.Value.Contains("create"))
            {
                return TaskType.Upload;
            }

            if (context.Request.Path.Value.Contains("download"))
            {
                return TaskType.Download;
            }

            return TaskType.None;
        }

        private Task<Server> ChooseServer(Dictionary<Server, List<JobTask>> lookup)
        {
            var server = ChooseInternal(lookup);

            return Task.FromResult(server);
        }

        private Server ChooseInternal(Dictionary<Server, List<JobTask>> lookup)
        {
            var orderedWeights = lookup
                .Select(x => new {x.Key, Weight = x.Value.Sum(task => _taskDifficulties[task.Type])})
                .OrderBy(x => x.Weight)
                .ToList();

            var chosen = orderedWeights.FirstOrDefault()?.Key;

            return chosen;
        }

        private readonly Dictionary<TaskType, float> _taskDifficulties = new Dictionary<TaskType, float>()
        {
            {TaskType.None, .1f},
            {TaskType.Download, .4f},
            {TaskType.Upload, .4f},
            {TaskType.Edit, .1f},
        };


        public void Release(ServiceHostAndPort hostAndPort)
        {
            var dbContext = _serviceProvider.GetRequiredService<CsContext>();
            var requestScoped = _serviceProvider.GetRequiredService<IRequestScopedDataRepository>();
            var data = requestScoped.Get<string>("RequestId").Data;


            var jobTask = dbContext
                .ServerJobTasks
                .Include(x => x.Task)
                .Include(x => x.Server)
                .FirstOrDefault(x => x.Task.RequestId == data);

            if (jobTask != null)
            {
                jobTask.Task.EndTime = DateTime.Now;
                dbContext.SaveChanges();
            }

            _logger.LogInformation(
                $"release: {hostAndPort.Scheme}://{hostAndPort.DownstreamHost}: {hostAndPort.DownstreamPort}");
        }
    }
}
