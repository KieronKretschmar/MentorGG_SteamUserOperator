﻿using System;
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
    [Route("api/[controller]")]
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
        /// GET: api/SteamUsers?steamIds=XXXXXXXXXXXXXXXXX,XXXXXXXXXXXXXXXXX,XXXXXXXXXXXXXXXXX
        /// </summary>
        /// <param name="steamIds">Multiple steamIds concatenated to a string, seperated by semicolons</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<List<SteamUser>>> GetUsers(string steamIds)
        {
            _logger.LogInformation($"Received GetUsers request for steamIds [ {steamIds} ]");

            // Parse steamIds
            var success = TryParseSteamIdsCsv(steamIds, out var steamIdsList);
            if (!success)
            {
                _logger.LogInformation($"Could not parse steamIds [ {steamIds} ]. Returning 400.");

                return BadRequest();
            }

            // Get cached userinfos from redis
            // Note: Simply checking whether the users are cached and only then querying them might be more performant
            var steamUsers = await _redis.GetSteamUsers(steamIdsList);

            // If all infos of all users were found in redis, return them directly
            if (steamUsers.Count == steamIdsList.Count)
            {
                return steamUsers;
            }

            // Query Valve API Refresh all users
            steamUsers = await _valveApi.QueryUsers(steamIdsList);

            // Insert into cache
            await _redis.SetSteamUsers(steamUsers);

            return steamUsers;
        }

        /// <summary>
        /// Tries to parse a string of steamIds seperated by SEPERATOR.
        /// </summary>
        /// <param name="steamIdsCsv"></param>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        private bool TryParseSteamIdsCsv(string steamIdsCsv, out List<long> steamIds)
        {
            steamIds = new List<long>();
            foreach (var steamIdString in steamIdsCsv.Split(Seperator))
            {
                var isSteamId = long.TryParse(steamIdString, out long steamId) && steamIdString.Length == 17;
                if (isSteamId)
                {
                    steamIds.Add(steamId);
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