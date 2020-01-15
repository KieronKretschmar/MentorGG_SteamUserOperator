using SteamUserOperator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SteamUserOperatorTests
{
    public static class SteamUserOperatorTestHelper
    {
        public static SteamUser GetRandomUser()
        {
            return new SteamUser
            {
                SteamId = (long)new Random().Next(1, 99999999),
                ImageUrl = "randomUrl_" + new Random().Next(1, 99999999),
                SteamName = "randomName_" + new Random().Next(1, 99999999),
            };
        }

        public static List<long> GetRealSteamIds()
        {
            return new List<long>
            {
                76561198033880857,
                76561197992897571,
                76561197994256276,
                76561198034944406,
                76561197985194911,
                76561198042587946,
                76561198142175587,
                76561198853642003,
                76561198433063343,
                76561198166019050,
                76561198044966222,
            };
        }

        /// <summary>
        /// Returns list of steamaccounts that are not used by real people (e.g. dev accounts)
        /// </summary>
        /// <returns></returns>
        public static List<long> GetRealTestSteamIds()
        {
            return new List<long>
            {
                76561199011630801,
            };
        }
    }
}
