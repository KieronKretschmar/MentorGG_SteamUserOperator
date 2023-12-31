using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SteamUserOperator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamUserOperatorTests
{
    [TestClass]
    public class ValveApiTests
    {
        private readonly string STEAM_API_KEY;

        public ValveApiTests()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = builder.Build();

            STEAM_API_KEY = config.GetValue<string>("STEAM_API_KEY");
            if (STEAM_API_KEY == null)
                throw new ArgumentNullException("The environment variable STEAM_API_KEY has not been set. Use Package Manager console.");
        }

        /// <summary>
        /// Tests QueryUsers with just one steamId at a time
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task QuerySingleUsers()
        {
            var valveApi = GetValveApi();
            foreach (var steamId in SteamUserOperatorTestHelper.GetRealSteamIds())
            {
                var response = await valveApi.QueryUsers(new List<long> { steamId });
                var user = response.Single();
                Assert.AreEqual(steamId, user.SteamId);
                Assert.IsFalse(string.IsNullOrEmpty(user.SteamName));
                Assert.IsFalse(string.IsNullOrEmpty(user.ImageUrl));
            }
        }

        /// <summary>
        /// Tests QueryUsers with multiple steamIds at once
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task QueryMultipleRealUsers()
        {
            // Create users
            var steamIds = SteamUserOperatorTestHelper.GetRealSteamIds();

            // Call QueryUsers
            var valveApi = GetValveApi();
            var users = await valveApi.QueryUsers(steamIds);

            // Assert that the response is correct
            Assert.AreEqual(users.Count, steamIds.Count);
            foreach (var user in users)
            {
                Assert.IsTrue(steamIds.Any(x => x == user.SteamId));
                Assert.IsFalse(string.IsNullOrEmpty(user.SteamName));
                Assert.IsFalse(string.IsNullOrEmpty(user.ImageUrl));
            }
        }

        private ValveApi GetValveApi()
        {
            var logMock = new Mock<ILogger<ValveApi>>().Object;
            return new ValveApi(logMock, STEAM_API_KEY);
        }
    }
}
