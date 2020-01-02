using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamUserOperator
{
    public interface IValveApi
    {
        Task<SteamUser> QueryUser(long steamId);
        Task<List<SteamUser>> QueryUsers(List<long> steamIds);
    }

    /// <summary>
    /// Communicates with Valve's data api.
    /// 
    /// Requires environment variables: ["VALVE_API_KEY"]
    /// </summary>
    public class ValveApi : IValveApi
    {
        private readonly ILogger<ValveApi> _logger;

        public static int CallsThisDay { get; set; }

        public ValveApi(ILogger<ValveApi> logger, IConfiguration configuration)
        {
            _logger = logger;
            var apikey = configuration.GetValue<string>("VALVE_API_KEY");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries the Valve Api for a single user.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public async Task<SteamUser> QueryUser(long steamId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries the Valve Api for multiple users.
        /// This implementation is more efficient than querying 1-by-1.
        /// </summary>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        public async Task<List<SteamUser>> QueryUsers(List<long> steamIds)
        {
            throw new NotImplementedException();
        }
    }
}
