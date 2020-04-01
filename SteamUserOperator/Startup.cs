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
            
            #region Valve API
            var STEAM_API_KEY = GetRequiredEnvironmentVariable<string>(Configuration, "STEAM_API_KEY");
            services.AddSingleton<IValveApi, ValveApi>(services =>
            {
                return new ValveApi(services.GetService<ILogger<ValveApi>>(), STEAM_API_KEY);
            });
            #endregion

            #region Redis
            var REDIS_CONFIGURATION_STRING = GetRequiredEnvironmentVariable<string>(Configuration, "REDIS_CONFIGURATION_STRING");
            var EXPIRE_AFTER_DAYS = GetRequiredEnvironmentVariable<long>(Configuration, "EXPIRE_AFTER_DAYS");

            // Add ConnectionMultiplexer as singleton as it is made to be reused
            // see https://stackexchange.github.io/StackExchange.Redis/Basics.html
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(REDIS_CONFIGURATION_STRING));
            services.AddScoped<ISteamInfoRedis, SteamInfoRedis>(services =>
            {
                return new SteamInfoRedis(
				services.GetService<ILogger<SteamInfoRedis>>(),
				services.GetRequiredService<IConnectionMultiplexer>(),
				EXPIRE_AFTER_DAYS);
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



        /// <summary>
        /// Attempt to retrieve an Environment Variable
        /// Throws ArgumentNullException is not found.
        /// </summary>
        /// <typeparam name="T">Type to retreive</typeparam>
        private static T GetRequiredEnvironmentVariable<T>(IConfiguration config, string key)
        {
            T value = config.GetValue<T>(key);
            if (value == null)
            {
                throw new ArgumentNullException(
                    $"{key} is missing, Configure the `{key}` environment variable.");
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Attempt to retrieve an Environment Variable
        /// Returns default value if not found.
        /// </summary>
        /// <typeparam name="T">Type to retreive</typeparam>
        private static T GetOptionalEnvironmentVariable<T>(IConfiguration config, string key, T defaultValue)
        {
            var stringValue = config.GetSection(key).Value;
            try
            {
                T value = (T)Convert.ChangeType(stringValue, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                return value;
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine($"Env var [ {key} ] not specified. Defaulting to [ {defaultValue} ]");
                return defaultValue;
            }
        }
    }
}
