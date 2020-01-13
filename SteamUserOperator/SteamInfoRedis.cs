using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamUserOperator
{
    public interface ISteamInfoRedis
    {
        Task<List<SteamUser>> GetSteamUsers(List<long> steamIds);
        Task SetSteamUsers(List<SteamUser> steamUsers);
    }

    public class SteamInfoRedis : ISteamInfoRedis
    {
        private readonly ILogger<SteamInfoRedis> _logger;
        private static string redisUri;
        private readonly TimeSpan expireAfter = TimeSpan.FromDays(14);
        private IDatabase cache;

        /// <summary>
        /// Communicates with the redis cache for SteamUsers.
        /// 
        /// Requires environment variables: ["REDIS_URI", "EXPIRE_AFTER_DAYS"]
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public SteamInfoRedis(ILogger<SteamInfoRedis> logger, IConfiguration configuration)
        {
            _logger = logger;
            redisUri = configuration.GetValue<string>("REDIS_URI");

            // Set expireAfter from env variable, if provided
            if(long.TryParse(configuration.GetValue<string>("EXPIRE_AFTER_DAYS"), out var expireAfterDaysFromEnv))
            {
                expireAfter = TimeSpan.FromDays(expireAfterDaysFromEnv);
            }

            cache = lazyConnection.Value.GetDatabase();
        }


        /// <summary>
        /// Looks for entries with the given steamIds in the redis cache, and returns a list of all that were found.
        /// </summary>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        public async Task<List<SteamUser>> GetSteamUsers(List<long> steamIds)
        {
            // Query Redis
            var results = cache.StringGet(steamIds.Select(x => (RedisKey) x.ToString()).ToArray());

            // Create a user for each non-empty value returned from redis
            var users = new List<SteamUser>();
            foreach (var item in results)
            {
                if (!item.IsNullOrEmpty)
                {
                    var user = JsonConvert.DeserializeObject<SteamUser>(item.ToString());
                    users.Add(user);
                }
            }

            _logger.LogInformation($"Retrieved [ {users.Count} ] / [ {steamIds.Count} ] SteamUsers from redis.");

            return users;
        }

        /// <summary>
        /// Inserts given users into the redis cache.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public async Task SetSteamUsers(List<SteamUser> steamUsers)
        {
            foreach (var user in steamUsers)
            {
                var key = user.SteamId.ToString();
                var value = JsonConvert.SerializeObject(user);
                if(!await cache.StringSetAsync(key, value, expiry: expireAfter))
                {
                    _logger.LogError($"Could not set redis entry for user with key {key} and value {value}");
                    continue;
                }
            }
        }

        /// <summary>
        /// Provides a lazy connection to redis.
        /// 
        /// For more info see https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-dotnet-core-quickstart.
        /// </summary>
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(redisUri);
        });

        /// <summary>
        /// Provides a connection to redis.
        /// 
        /// For more info see https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-dotnet-core-quickstart.
        /// </summary>
        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
    }
}
