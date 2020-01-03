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
    }
}
