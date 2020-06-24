using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Steeltoe.Management.CloudFoundry;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Hypermedia;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Info;

namespace PalTracker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var message = ExtractConfigValue("WELCOME_MESSAGE");
            services.AddSingleton(sp => new WelcomeMessage(message));

            var port = ExtractConfigValue("PORT");
            var limit = ExtractConfigValue("MEMORY_LIMIT");

            var index = ExtractConfigValue("CF_INSTANCE_INDEX");
            var address = ExtractConfigValue("CF_INSTANCE_ADDR");        
           services.AddSingleton(sp => new CloudFoundryInfo(port, limit, index, address));

            services.AddDbContext<TimeEntryContext>(options => options.UseMySql(Configuration));
             services.AddCloudFoundryActuators(Configuration, MediaTypeVersion.V2, ActuatorContext.ActuatorAndCloudFoundry);


           services.AddScoped<ITimeEntryRepository, MySqlTimeEntryRepository>();

           services.AddScoped<IHealthContributor, TimeEntryHealthContributor>();

           services.AddSingleton<IOperationCounter<TimeEntry>, OperationCounter<TimeEntry>>();
           
           services.AddSingleton<IInfoContributor, TimeEntryInfoContributor>();
        }

private string ExtractConfigValue(string key)
{
            var message = Configuration.GetValue<string>(key);
           if (string.IsNullOrEmpty(message))
           {
               throw new ApplicationException($"{key} not configured.");
           }

           return message;
}
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCloudFoundryActuators(MediaTypeVersion.V2, ActuatorContext.ActuatorAndCloudFoundry);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
