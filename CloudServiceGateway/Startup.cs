using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery.Providers;

namespace CloudServiceGateway
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddTransient<ICoolService, CoolService>();
            
            services.AddOcelot()
                .AddCustomLoadBalancer(LoadBalancerFactoryFunc);

        }

        private CustomLoadBalancer LoadBalancerFactoryFunc(IServiceProvider serviceProvider,DownstreamRoute route, IServiceDiscoveryProvider serviceDiscoveryProvider)
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return new CustomLoadBalancer(serviceDiscoveryProvider.Get, serviceProvider);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
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

    public interface ICoolService
    {
        string Get() => "hello";
    }

    class CoolService : ICoolService
    {
    }
}
