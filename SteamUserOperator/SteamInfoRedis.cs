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
        private readonly TimeSpan _expireAfter;
        private IDatabase cache;

        /// <summary>
        /// Communicates with the redis cache for SteamUsers.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public SteamInfoRedis(ILogger<SteamInfoRedis> logger, IConnectionMultiplexer connectionMultiplexer, long expireAfterDays)
        {
            _logger = logger;
            _expireAfter = TimeSpan.FromDays(expireAfterDays);
            cache = connectionMultiplexer.GetDatabase();
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
                if(!await cache.StringSetAsync(key, value, expiry: _expireAfter))
                {
                    _logger.LogError($"Could not set redis entry for user with key [ {key} ] and value [ {value} ]");
                    continue;
                }
            }
        }
    }
}
