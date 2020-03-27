using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace SteamUserOperator.Controllers
{
    [Route("users")]
    [ApiController]
    public class SteamUsersController : ControllerBase
    {
        public const char Seperator = ',';
        private readonly ILogger<SteamUsersController> _logger;
        private readonly ISteamInfoRedis _redis;
        private readonly IValveApi _valveApi;

        public SteamUsersController(ILogger<SteamUsersController> logger, ISteamInfoRedis redis, IValveApi valveApi)
        {
            _logger = logger;
            _redis = redis;
            _valveApi = valveApi;
        }

        /// <summary>
        /// Attempts to retrieve the users' infos from redis if available. If not, queries Valve's api instead and adds data to cache.
        /// This implementation is more efficient than querying 1-by-1.
        /// 
        /// GET: /users?steamIds=XXXXXXXXXXXXXXXXX,XXXXXXXXXXXXXXXXX,XXXXXXXXXXXXXXXXX
        /// </summary>
        /// <param name="steamIds">Multiple steamIds concatenated to a string, seperated by semicolons</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<SteamUser>>> GetUsers(string steamIds)
        {
            if (steamIds == null)
            {
                return BadRequest("SteamIds must not equal null");
            }

            _logger.LogInformation($"Received GetUsers request for steamIds [ {steamIds} ]");

            // Parse steamIds
            var success = TryParseSteamIdsCsv(steamIds, out var steamIdsList, out var unknownIds);
            if (!success)
            {
                _logger.LogInformation($"Could not parse steamIds [ {steamIds} ]. Returning 400.");
                return BadRequest();
            }

            // Get cached userinfos from redis
            // Note: Simply checking whether the users are cached and only then querying them might be more performant
            var steamUsers = await _redis.GetSteamUsers(steamIdsList);

            // If all infos of all users were found in redis, return them directly
            if (steamUsers.Count == steamIdsList.Count + unknownIds.Count)
            {
                _logger.LogInformation("Retrieved sufficient information from Redis, Returning cached data");
                return steamUsers;
            }

            // Query Valve API Refresh all users
            steamUsers = await _valveApi.QueryUsers(steamIdsList);

            // Insert into cache
            await _redis.SetSteamUsers(steamUsers);

            // Make the assumption that all unknownIds are Bots
            var unknownSteamUsers = CreateBotUsers(unknownIds);
            steamUsers.AddRange(unknownSteamUsers);

            return steamUsers;
        }

        /// <summary>
        /// Creates a list of SteamUsers for Bots.
        /// </summary>
        private List<SteamUser> CreateBotUsers(List<long> ids)
        {
            var botUsers = new List<SteamUser>();
            foreach (var id in ids)
            {
                // Log when the the value NOT is negative as Bots should always have negative SteamId values.
                if (id > 0)
                {
                    _logger.LogWarning($"Received a positive value when creating Bot Users! Id [ {id} ]");
                }

                SteamUser botUser = new SteamUser();
                botUser.SteamId = id;
                botUser.SteamName = "Bot";
                botUsers.Add(botUser);
            }
            return botUsers;
        }


        /// <summary>
        /// Tries to parse a string of steamIds seperated by SEPERATOR.
        /// </summary>
        /// <param name="steamIdsCsv"></param>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        private bool TryParseSteamIdsCsv(string steamIdsCsv, out List<long> steamIds, out List<long> unknownIds)
        {
            steamIds = new List<long>();
            unknownIds = new List<long>();
            foreach (var steamIdString in steamIdsCsv.Split(Seperator))
            {
                bool parseResult = long.TryParse(steamIdString, out long parsedValue);
                if (parseResult)
                {
                    // Ensure the parsedValue is positive and the length is 17
                    if (steamIdString.Length == 17 && parsedValue > 0)
                    {
                        steamIds.Add(parsedValue);
                    }   
                    else
                    {   
                        unknownIds.Add(parsedValue);
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
