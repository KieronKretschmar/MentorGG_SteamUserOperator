using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SteamUserOperator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SteamUsersController : ControllerBase
    {
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
            throw new NotImplementedException();
        }

    }
}