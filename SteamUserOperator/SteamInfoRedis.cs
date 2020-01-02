using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Communicates with the redis cache for SteamUsers.
        /// 
        /// Requires environment variables: ["REDIS_URI"]
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public SteamInfoRedis(ILogger<SteamInfoRedis> logger, IConfiguration configuration)
        {
            _logger = logger;
            var apikey = configuration.GetValue<string>("REDIS_URI");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Looks for entries with the given steamIds in the redis cache, and returns a list of all that were found.
        /// </summary>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        public async Task<List<SteamUser>> GetSteamUsers(List<long> steamIds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Inserts given users into the redis cache.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public async Task SetSteamUsers(List<SteamUser> steamUsers)
        {
            throw new NotImplementedException();
        }

    }
}
