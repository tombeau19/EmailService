using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Swagger;

namespace BrontoTransactionalEndpoint
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var logFilePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "bronto.log");
            var sqliteFilePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "logs.sqlite");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .WriteTo.Async(al =>
                {
                    al.File(logFilePath, rollingInterval: RollingInterval.Day);
                    al.SQLite(sqliteFilePath);
                    al.Console();
                })
                .Enrich.FromLogContext()
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Version = "v1",
                        Title = "BrontoTransactAPI",
                        Description = "Sends transactional emails through the Bronto Platform. Also, updates contacts in Bronto",
                        Contact = new Contact() { Name = "SuiteSquad", Email = "t.beauregard@hmwallace.com" }
                    });

                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrontoTransactionalEndpoint.xml");
                c.IncludeXmlComments(filePath);
                c.DescribeAllEnumsAsStrings();
                c.DescribeStringEnumsInCamelCase();

                c.OrderActionsBy(api => api.GroupName);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "BrontoTransact API V1");
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
