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
using StackExchange.Redis;

namespace SteamUserOperator
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
            services.AddLogging(services =>
            {
                services.AddConsole(o =>
                {
                    o.TimestampFormat = "[yyyy-MM-dd HH:mm:ss zzz] ";
                });
                services.AddDebug();
            });

            #region Read environment variables
            var STEAM_API_KEY = Configuration.GetValue<string>("STEAM_API_KEY");
            if (STEAM_API_KEY == null)
                throw new ArgumentNullException("The environment variable STEAM_API_KEY has not been set.");

            // GetValue<long>() throws exception if env var is not configured correctly or not set.
            var EXPIRE_AFTER_DAYS = Configuration.GetValue<long>("EXPIRE_AFTER_DAYS");
            #endregion

            #region Add services
            services.AddSingleton<IValveApi, ValveApi>(services =>
            {
                return new ValveApi(services.GetService<ILogger<ValveApi>>(), STEAM_API_KEY);
            });
            #endregion


            #region Redis
            var REDIS_CONFIGURATION_STRING = Configuration.GetValue<string>("REDIS_CONFIGURATION_STRING");
            // Add ConnectionMultiplexer as singleton as it is made to be reused
            // see https://stackexchange.github.io/StackExchange.Redis/Basics.html
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(REDIS_CONFIGURATION_STRING));
            services.AddTransient<ISteamInfoRedis, SteamInfoRedis>(services =>
            {
                return new SteamInfoRedis(services.GetService<ILogger<SteamInfoRedis>>(), services.GetRequiredService<IConnectionMultiplexer>(), EXPIRE_AFTER_DAYS);
            });
            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
