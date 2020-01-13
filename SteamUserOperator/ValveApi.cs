using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteamUserOperator
{
    public interface IValveApi
    {
        Task<List<SteamUser>> QueryUsers(List<long> steamIds);
    }

    /// <summary>
    /// Communicates with Valve's data api.
    /// 
    /// Requires environment variables: ["STEAM_API_KEY"]
    /// </summary>
    public class ValveApi : IValveApi
    {
        private readonly ILogger<ValveApi> _logger;
        private HttpClient Client { get; }
        private readonly string apiKey;
        private const int MaxSteamIdsPerQuery = 100;
        private const int MaxDailyApiCalls = 100000;
        private static int thisDay { get; set; } = DateTime.Now.Day;
        private static int callsThisDay { get; set; } = 0;

        public ValveApi(ILogger<ValveApi> logger, IConfiguration configuration)
        {
            _logger = logger;
            Client = new HttpClient();
            apiKey = configuration.GetValue<string>("STEAM_API_KEY");
        }

        /// <summary>
        /// Queries the Valve Api for multiple users.
        /// This implementation is more efficient than querying 1-by-1.
        /// 
        /// See https://developer.valvesoftware.com/wiki/Steam_Web_API#GetPlayerSummaries_.28v0002.29 for more info
        /// </summary>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        public async Task<List<SteamUser>> QueryUsers(List<long> steamIds)
        {
            List<SteamUser> steamUsers;

            // Split query into smaller chunks if it exceeds the max number of steamids per query 
            if(steamIds.Count > MaxSteamIdsPerQuery)
            {
                // Split into chunks
                var chunks = steamIds
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / MaxSteamIdsPerQuery)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                // Call this method for each chunk and return aggregated result
                steamUsers = new List<SteamUser>();
                foreach (var chunk in chunks)
                {
                    steamUsers.AddRange(await QueryUsers(chunk));
                }
                return steamUsers;
            }
            else
            {
                // Query Api
                var queryUrl = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={String.Join(',', steamIds)}";
                var response = await Client.GetAsync(queryUrl);
                CountApiCall();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Valve Api returned Status [ {response.StatusCode} ] [ {response.ReasonPhrase} ] with content [ {response.Content} ]");
                }

                var json = await response.Content.ReadAsStringAsync();

                // Extract data from json response
                try
                {
                    var userInfosArray = (JArray)JObject.Parse(json)["response"]["players"];

                    steamUsers = userInfosArray
                        .Select(x => new SteamUser
                        {
                            SteamId = x["steamid"].ToObject<long>(),
                            ImageUrl = x["avatar"].ToObject<string>(),
                            SteamName = x["personaname"].ToObject<string>()
                        })
                        .ToList();
                    return steamUsers;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not parse response from Valve Api. Json: [ {json} ]");
                    throw;
                }
            }
        }

        /// <summary>
        /// Increments the number of Api calls by one, resets them if a new day started, and logs related information.
        /// </summary>
        private void CountApiCall()
        {
            callsThisDay++;

            // Log if limit is reached
            if (callsThisDay >= MaxDailyApiCalls)
            {
                _logger.LogError($"Reached limit for api calls {callsThisDay}/{MaxDailyApiCalls} at {24 - DateTime.Now.Hour} hours left today.");
            }
            
            // If this is the first call of a new day, update thisDay and log last days' calls.
            if (DateTime.Now.Day != thisDay)
            {
                var msg = $"The Steam Api was called {callsThisDay}/{MaxDailyApiCalls} times today.";
                _logger.LogInformation(msg);

                // Also log as warning if close to limit
                if((double) callsThisDay / MaxDailyApiCalls > 0.75)
                {
                    _logger.LogWarning(msg);
                }

                thisDay = DateTime.Now.Day;
                callsThisDay = 0;
            }
        }
    }
}
