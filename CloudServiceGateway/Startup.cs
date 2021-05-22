using System;
using CloudServiceGateway.Configuration;
using CloudServiceGateway.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery.Providers;

namespace CloudServiceGateway
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            
            services.Configure<RoutesConf>(_configuration);

            services.AddDbContext<CsContext>(opt =>
            {
                var connectionString = _configuration.GetConnectionString("default");
                if (string.IsNullOrEmpty(connectionString))
                {
                    opt.UseInMemoryDatabase("test");
                }
                else
                {
                    throw new Exception("Database provider is not set up");
                }
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);
            
            services.AddOcelot()
                .AddCustomLoadBalancer(LoadBalancerFactoryFunc);

        }

        private CustomLoadBalancer LoadBalancerFactoryFunc(IServiceProvider serviceProvider,DownstreamRoute route, IServiceDiscoveryProvider serviceDiscoveryProvider)
        {
            return new CustomLoadBalancer(serviceDiscoveryProvider.Get, serviceProvider);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider, IOptions<RoutesConf> options)
        {
            if (env.IsDevelopment())
            {
                DatabaseInitializer.Initialize(provider, options);
                app.UseDeveloperExceptionPage();
            }

            app.UseOcelot();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
        }
    }
}
