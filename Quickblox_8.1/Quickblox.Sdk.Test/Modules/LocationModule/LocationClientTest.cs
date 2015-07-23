﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Quickblox.Sdk.Hmacsha;
using Quickblox.Sdk.Modules.LocationModule.Models;
using Quickblox.Sdk.Modules.LocationModule.Requests;

namespace Quickblox.Sdk.Test.Modules.LocationModule
{
    [TestClass]
    public class LocationClientTest
    {
        private QuickbloxClient client;

        [TestInitialize]
        public async Task TestInitialize()
        {
            this.client = new QuickbloxClient(GlobalConstant.ApiBaseEndPoint, GlobalConstant.ChatEndpoint, new HmacSha1CryptographicProvider());
            var sessionResponse = await this.client.CoreClient.CreateSessionWithLoginAsync(GlobalConstant.ApplicationId, GlobalConstant.AuthorizationKey, GlobalConstant.AuthorizationSecret, "Test654321", "Test12345");
            client.Token = sessionResponse.Result.Session.Token;
        }

        [TestMethod]
        public async Task CreateGeoDataSuccessTest()
        {
            var geoData = new CreateGeoDataRequest();
            geoData.GeoData = new Location()
            {
                Latitude = 49.9935,
                Longtitude = 36.23038
            };

            var response = await this.client.LocationClient.CreateGeoDataAsync(geoData);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
            Assert.IsNotNull(response.Result);
        }

        [TestMethod]
        public async Task CreateGeoDataWithStatusSuccessTest()
        {
            var geoData = new CreateGeoDataRequest();
            geoData.GeoData = new Location()
            {
                Latitude = 49.9935,
                Longtitude = 36.23038,
                Status = "Hello world"
            };

            var response = await this.client.LocationClient.CreateGeoDataAsync(geoData);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
            Assert.IsNotNull(response.Result);
        }

        [TestMethod]
        public async Task CreateGeoDataWithPushDataSuccessTest()
        {
            var geoData = new CreateGeoDataWithPushRequest();
            geoData.GeoData = new PushLocation()
            {
                Latitude = 49.9935,
                Longtitude = 36.23038,
                Status = "Hello world",
                PushEnvironment = PushEnvironment.development,
                Radius = 150,
                PushMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test push method"))
            };

            var response = await this.client.LocationClient.CreatePushGeoDataAsync(geoData);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
            Assert.IsNotNull(response.Result);
        }

        [TestMethod]
        public async Task CreateGeoDataAndUpdateSuccessTest()
        {
            var geoData = new CreateGeoDataRequest();
            geoData.GeoData = new Location()
            {
                Latitude = 49.9935,
                Longtitude = 36.23038,
                Status = "TestUpdate"
            };

            var response = await this.client.LocationClient.CreateGeoDataAsync(geoData);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
            Assert.IsNotNull(response.Result);

            var updateData = new UpdateGeoDataRequest();
            updateData.GeoData = new Location()
            {
                Latitude = 13.9935,
                Longtitude = 25.345,
                Status = "TestUpdate2"
            };

            var updateResponse = await this.client.LocationClient.UpdateGeoDataAsync(response.Result.GeoDatum.Id, updateData);
            Assert.AreEqual(updateResponse.StatusCode, HttpStatusCode.OK);
            Assert.IsNotNull(updateResponse.Result);
        }

        [TestMethod]
        public async Task GetGeoDataByIdSuccessTest()
        {
            var geoData = new CreateGeoDataRequest();
            geoData.GeoData = new Location()
            {
                Latitude = 49.9935,
                Longtitude = 36.23038,
                Status = "Test get by id"
            };

            var response = await this.client.LocationClient.CreateGeoDataAsync(geoData);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
            Assert.IsNotNull(response.Result);


            var getResponse = await this.client.LocationClient.GetGeoDataByIdAsync(response.Result.GeoDatum.Id);
            Assert.AreEqual(getResponse.StatusCode, HttpStatusCode.OK);
            Assert.IsNotNull(getResponse.Result);
            Assert.AreEqual(geoData.GeoData.Status, getResponse.Result.GeoData.Status);
        }

        [TestMethod]
        public async Task FindGeoDataSuccessTest()
        {
            var findFilterRequest = new FindGeoDataRequest();
            findFilterRequest.UserId = 2701456;
            var getResponse = await this.client.LocationClient.FindGeoDataAsync(findFilterRequest);
            Assert.AreEqual(getResponse.StatusCode, HttpStatusCode.OK);
            Assert.IsNotNull(getResponse.Result);
        }

        [TestMethod]
        public async Task DeleteGeoDataByIdSuccessTest()
        {
            var geoData = new CreateGeoDataRequest();
            geoData.GeoData = new Location()
            {
                Latitude = 49.9935,
                Longtitude = 36.23038,
                Status = "Test delete by id"
            };

            var response = await this.client.LocationClient.CreateGeoDataAsync(geoData);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
            Assert.IsNotNull(response.Result);

            // DELETE
            var deleteResponse = await this.client.LocationClient.DeleteGeoDataById(response.Result.GeoDatum.Id);
            Assert.AreEqual(deleteResponse.StatusCode, HttpStatusCode.OK);

            // Check if presents
            var getResponse = await this.client.LocationClient.GetGeoDataByIdAsync(response.Result.GeoDatum.Id);
            Assert.AreNotEqual(getResponse.StatusCode, HttpStatusCode.OK);
        }
    }
}