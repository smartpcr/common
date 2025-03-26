// -----------------------------------------------------------------------
// <copyright file="BlobCacheSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache.Tests.Steps
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Hooks;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Reqnroll;
    using Storage;

    [Binding]
    public class BlobCacheSteps
    {
        private readonly ScenarioContext context;
        private readonly IReqnrollOutputHelper outputHelper;
        private readonly CloudBlobClient blobClient;
        private readonly BlobCache blobCache;

        public BlobCacheSteps(ScenarioContext context, FeatureContext featureContext, IReqnrollOutputHelper outputHelper)
        {
            this.context = context;
            this.outputHelper = outputHelper;
            var serviceProvider = context.Get<IServiceProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            this.blobClient = featureContext.Get<CloudBlobClient>();
            Environment.SetEnvironmentVariable("StorageEmulatorConnectionString", BlobStorageHook.LocalStorageConnection, EnvironmentVariableTarget.Process);
            this.blobCache = new BlobCache(serviceProvider, loggerFactory, new BlobStorageSettings()
            {
                Account = "account",
                Container = "container",
                AuthMode = StorageAuthMode.ConnectionStringFromEnvironment,
                ConnectionName = "StorageEmulatorConnectionString"
            });
        }

        [TechTalk.SpecFlow.Given(@"blob storage is running")]
        public async Task GivenBlobStorageIsRunning()
        {
            try
            {
                var containerClient = this.blobClient.GetContainerReference("testcontainer");
                var containerExists = await containerClient.ExistsAsync();
                containerExists.Should().BeTrue();
            }
            catch (Exception ex)
            {
                this.outputHelper.WriteLine(ex.Message);
                throw;
            }
        }

        [TechTalk.SpecFlow.Given(@"a product")]
        public void GivenAProduct(Table table)
        {
            var product = table.CreateInstance<Product>();
            this.context.Set(product, "product");
        }

        [TechTalk.SpecFlow.When(@"I store product in blob cache with key ""(\w+)""")]
        public async Task WhenIStoreProductInBlobCacheWithKey(string key)
        {
            var product = this.context.Get<Product>("product");
            var serializer = new JsonSerializer();
            var productJson = JsonConvert.SerializeObject(product);
            var productBytes = System.Text.Encoding.UTF8.GetBytes(productJson);
            await this.blobCache.SetAsync(key, productBytes, new DistributedCacheEntryOptions());
        }

        [TechTalk.SpecFlow.Then(@"I should be able to retrieve product from blob cache with key ""(\w+)""")]
        public async Task ThenIShouldBeAbleToRetrieveProductFromBlobCacheWithKey(string key)
        {
            var product = this.context.Get<Product>("product");
            var productBytes = await this.blobCache.GetAsync(key);
            var productJson = System.Text.Encoding.UTF8.GetString(productBytes!);
            var retrievedProduct = JsonConvert.DeserializeObject<Product>(productJson);
            retrievedProduct.Should().BeEquivalentTo(product);
        }
    }

    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }
}
