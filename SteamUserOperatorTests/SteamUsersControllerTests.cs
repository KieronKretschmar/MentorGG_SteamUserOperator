using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SteamUserOperator;
using SteamUserOperator.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamUserOperatorTests
{
    [TestClass]
    public class SteamUsersControllerTests
    {
        private readonly string STEAM_API_KEY;

        public readonly string REDIS_URI;
        public readonly long EXPIRE_AFTER_DAYS;

        public SteamUsersControllerTests()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = builder.Build();

            STEAM_API_KEY = config.GetValue<string>("STEAM_API_KEY");
            if (STEAM_API_KEY == null)
                throw new ArgumentNullException("The environment variable STEAM_API_KEY has not been set. Use Package Manager console.");

            REDIS_URI = config.GetValue<string>("REDIS_URI");
            if (REDIS_URI == null)
                throw new ArgumentNullException("The environment variable REDIS_URI has not been set. Use Package Manager console.");

            EXPIRE_AFTER_DAYS = 1; // No need to load from environment in tests 
        }

        /// <summary>
        /// Tests GET: api/SteamUsers with non-mocked dependencies and a single real steamid, and asserts that plausible data is returned. 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetSingleRealUser()
        {
            foreach (var steamId in SteamUserOperatorTestHelper.GetRealSteamIds())
            {
                var steamUsersController = GetUsersController();

                var response = await steamUsersController.GetUsers(steamId.ToString());

                var user = response.Value.Single();
                Assert.AreEqual(steamId, user.SteamId);
                Assert.IsFalse(string.IsNullOrEmpty(user.SteamName));
                Assert.IsFalse(string.IsNullOrEmpty(user.ImageUrl));
            }
        }

        /// <summary>
        /// Tests GET: api/SteamUsers with non-mocked dependencies and multiple real steamids, and asserts that plausible data is returned. 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetMultipleRealUsers()
        {
            var steamUsersController = GetUsersController();

            // Create users
            var steamIds = SteamUserOperatorTestHelper.GetRealSteamIds();
            var steamIdsCsv = String.Join(SteamUsersController.Seperator, steamIds);

            // Call controller
            var response = await steamUsersController.GetUsers(steamIdsCsv);

            Assert.AreEqual(response.Value.Count, steamIds.Count);

            foreach (var user in response.Value)
            {
                Assert.IsTrue(steamIds.Any(x => x == user.SteamId));
                Assert.IsFalse(string.IsNullOrEmpty(user.SteamName));
                Assert.IsFalse(string.IsNullOrEmpty(user.ImageUrl));
            }
        }

        /// <summary>
        /// Tests GET: api/SteamUsers and asserts that one attempt is made to load data from cache. 
        /// Also asserts that in case no cached data is found, valves api will be queried and updated data is inserted to redis cache.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task UsersUpdatedIfNotCached()
        {
            // Get steamIds
            var steamIds = SteamUserOperatorTestHelper.GetRealSteamIds();

            // Create valveApiMock, mocked to return (dummy) data for each steamId
            var valveApiMock = new Mock<IValveApi>();
            var queryUsersReturn = steamIds
                .Select(x => new SteamUser { SteamId = x, ImageUrl = "ImageUrl", SteamName = "SteamName" })
                .ToList();
            valveApiMock
                .Setup(x => x.QueryUsers(It.Is<List<long>>(x => x.All(y => steamIds.Contains(y)))))
                .Returns(Task.FromResult(queryUsersReturn));

            // Create redisMock, mocked to behave as if no cached data is found
            var redisMock = new Mock<ISteamInfoRedis>();
            redisMock
                .Setup(x => x.GetSteamUsers(It.IsAny<List<long>>()))
                .Returns(Task.FromResult(new List<SteamUser>()));

            // Create controller with mocked dependencies
            var controllerLogMock = new Mock<ILogger<SteamUsersController>>().Object;
            var steamUsersController = new SteamUsersController(controllerLogMock, redisMock.Object, valveApiMock.Object);

            // Call GetUsers for all users
            var steamIdsCsv = String.Join(SteamUsersController.Seperator, steamIds);
            await steamUsersController.GetUsers(steamIdsCsv);

            // Verify that redis GetSteamUsers was called to attempt loading all users from cache
            redisMock.Verify(x => x.GetSteamUsers(It.Is<List<long>>(x => x.All(y=> steamIds.Contains(y)))), Times.Once);

            // As redisMock is mocked not return them from cache, expect call to load them from ValveApi
            valveApiMock.Verify(x => x.QueryUsers(It.Is<List<long>>(x => x.All(y => steamIds.Contains(y)))), Times.Once);

            // Verify that SetSteamUsers was called once for all users
            redisMock.Verify(x => x.SetSteamUsers(It.Is<List<SteamUser>>(x => x.All(y => steamIds.Contains(y.SteamId)))), Times.Once);

        }

        /// <summary>
        /// Tests GET: api/SteamUsers and asserts that no attempt is done to load data from valves api if cached 
        /// data is available and no unnecessary update call to redis is performed. 
        /// </summary>
        /// <returns></returns>
        public async Task ApiNotCalledIfUserInCache()
        {
            // Create random user
            var user = SteamUserOperatorTestHelper.GetRandomUser();

            // Create redisMock, mocked to behave as if the user was cached
            var redisMock = new Mock<ISteamInfoRedis>();
            redisMock
                .Setup(x => x.GetSteamUsers(It.IsAny<List<long>>()))
                .Returns(Task.FromResult(new List<SteamUser> { user}));

            // Create valveApiMock
            var valveApiMock = new Mock<IValveApi>();

            // Create controller with mocked dependencies
            var controllerLogMock = new Mock<ILogger<SteamUsersController>>().Object;
            var steamUsersController = new SteamUsersController(controllerLogMock, redisMock.Object, valveApiMock.Object);

            // Call GetUsers for the user
            await steamUsersController.GetUsers(steamIds: user.SteamId.ToString());

            // Verify that no attempt was made to query user from ValveApi
            valveApiMock.Verify(x => x.QueryUsers(It.IsAny<List<long>>()), Times.Never);

            // Verify that no unnecessary call was made to update the user's redis entry 
            redisMock.Verify(x => x.SetSteamUsers(It.IsAny<List<SteamUser>>()), Times.Never);
        }


        private SteamUsersController GetUsersController()
        {
            // Create steamInfoRedis
            var redis = GetSteamInfoRedis();

            // Create valveApi
            var valveApi = GetValveApi();

            // Create steamUsersController
            var controllerLog = new Mock<ILogger<SteamUsersController>>().Object;
            var steamUsersController = new SteamUsersController(controllerLog, redis, valveApi);

            return steamUsersController;
        }

        private ValveApi GetValveApi()
        {
            var logMock = new Mock<ILogger<ValveApi>>().Object;
            return new ValveApi(logMock, STEAM_API_KEY);
        }

        private SteamInfoRedis GetSteamInfoRedis()
        {
            var logMock = new Mock<ILogger<SteamInfoRedis>>().Object;
            return new SteamInfoRedis(logMock, REDIS_URI, EXPIRE_AFTER_DAYS);
        }
    }
}
