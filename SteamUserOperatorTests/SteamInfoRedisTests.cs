using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SteamUserOperator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamUserOperatorTests
{
    [TestClass]
    public class SteamInfoRedisTests
    {
        public readonly string REDIS_URI;
        public readonly long EXPIRE_AFTER_DAYS;

        public SteamInfoRedisTests()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = builder.Build();

            REDIS_URI = config.GetValue<string>("REDIS_URI");
            if (REDIS_URI == null)
                throw new ArgumentNullException("The environment variable REDIS_URI has not been set. Use Package Manager console.");

            EXPIRE_AFTER_DAYS = 1; // No need to load from environment in tests 

        }

        /// <summary>
        /// Tests SetSteamUsers and GetSteamUsers with multiple users and verifies that they work as expected.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SetAndGetUserInfos()
        {
            var logMock = new Mock<ILogger<SteamInfoRedis>>().Object;
            var redis = new SteamInfoRedis(logMock, REDIS_URI, EXPIRE_AFTER_DAYS);

            // Create user with random properties
            // Note: The steamId does not belong to users and thus their cached data can be manipulated even 
            // on the production redis server without real-world repercussions
            var users = SteamUserOperatorTestHelper.GetRealTestSteamIds()
                .Select(steamId => new SteamUser
                {
                    SteamId = steamId,
                    ImageUrl = RandomString(10),
                    SteamName = RandomString(12),
                })
                .ToList();

            // Insert users to cache
            await redis.SetSteamUsers(users);

            // Load users from redis cache
            var cachedUsers = await redis.GetSteamUsers(users.Select(x=>x.SteamId).ToList());

            // Check if data returned from cache is identical
            foreach (var user in users)
            {
                // Assert equality by comparing serialized representations
                var cachedJson = JsonConvert.SerializeObject(cachedUsers.Single(x => x.SteamId == user.SteamId));
                var localJson = JsonConvert.SerializeObject(user);
                Assert.AreEqual(localJson, cachedJson);
            }
        }

        private static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
