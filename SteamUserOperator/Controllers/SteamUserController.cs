using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SteamUserOperator;

namespace SteamInfoGatherer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SteamUserController : ControllerBase
    {

        /// <summary>
        /// Attempts to retrieve the user's info from redis if available. If not, queries Valve's api instead and adds data to cache.
        /// 
        /// GET: api/User/<steamId>
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        [HttpGet("{steamId}")]
        public async Task<ActionResult<SteamUser>> GetUser(long steamId)
        {
            throw new NotImplementedException();
        }
    }
}