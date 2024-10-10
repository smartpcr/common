namespace Common.Storage.Tests.Steps;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Reqnroll;
using Xunit.Abstractions;

[Binding]
public class BlockBlobSteps
{
    private readonly ITestOutputHelper _output;
    private readonly ScenarioContext _context;

    public BlockBlobSteps(ITestOutputHelper output, ScenarioContext context)
    {
        _output = output;
        _context = context;
    }

    [Given(@"a new blob with uri ""(.*)""")]
    public async Task GivenBlobUri(string blobUri)
    {
        var parts = blobUri.Split('/');
        var containerName = parts[^2];
        var blobName = parts[^1];
        var blobClient = _context.Get<CloudBlobClient>("BlobClient");
        var container = blobClient.GetContainerReference(containerName);
        _output.WriteLine($"Ensure container {containerName} is created");
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlockBlobReference(blobName);
        _context.Set(blob, "Blob");
    }

    [When(@"I create blob with content ""(.*)""")]
    public async Task WhenICreateBlobWithContent(string blobContent)
    {
        var blob = _context.Get<CloudBlockBlob>("Blob");
        await blob.UploadTextAsync(blobContent);
    }

    [Then(@"I should be able to fetch blob attributes")]
    public async Task ThenIShouldBeAbleToFetchBlobAttributes()
    {
        var blob = _context.Get<CloudBlockBlob>("Blob");
        await blob.FetchAttributesAsync();
    }

    [Then(@"the block type should be ""(.*)""")]
    public void ThenTheBlockTypeShouldBe(string blockBlob)
    {
        var blob = _context.Get<CloudBlockBlob>("Blob");
        var expected = blockBlob == "BlockBlob" ? BlobType.BlockBlob : BlobType.PageBlob;
        blob.BlobType.Should().Be(expected);
    }

    [Then(@"I should be able to download blob")]
    public async Task ThenIShouldBeAbleToDownloadBlob()
    {
        var blob = _context.Get<CloudBlockBlob>("Blob");
        var blobContent = await blob.DownloadTextAsync();
        _output.WriteLine($"Blob content: {blobContent}");
        _context.Set(blobContent, "BlobContent");
    }

    [Then(@"the downloaded content should be ""(.*)""")]
    public void ThenTheDownloadedContentShouldBe(string expectedBlobContent)
    {
        var actualBlobContent = _context.Get<string>("BlobContent");
        actualBlobContent.Should().Be(expectedBlobContent);
    }

    [Then(@"the blob uri should be ""(.*)""")]
    public void ThenTheBlobUriShouldBe(string expectedBlobUrl)
    {
        var blob = _context.Get<CloudBlockBlob>("Blob");
        blob.Uri.ToString().Should().Be(expectedBlobUrl);
    }

    [Given(@"the following page blobs and their size")]
    public void GivenTheFollowingPageBlobsAndTheirSize(Table table)
    {
        var pageBlobDefinitions = new List<(Uri blobUri, string containerName, string blobName, long size)>();
        foreach (var row in table.Rows)
        {
            var blobUri = new Uri(row["BlobUri"]);
            var segments = blobUri.AbsolutePath.TrimStart('/').Split('/');
            if (segments.Length >= 2)
            {
                var containerName = segments[0];
                var blobName = string.Join("/", segments, 1, segments.Length - 1);
                var sizeMatch = Regex.Match(row["Size"].Trim(), @"^(\d+)([KMG])$");
                var size = sizeMatch is { Success: true }
                    ? sizeMatch.Groups[2].Value switch
                    {
                        "K" => long.Parse(sizeMatch.Groups[1].Value) * 1024,
                        "M" => long.Parse(sizeMatch.Groups[1].Value) * 1024 * 1024,
                        "G" => long.Parse(sizeMatch.Groups[1].Value) * 1024 * 1024 * 1024,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                    : long.Parse(row["Size"]);
                var emulatedBlobUri = new Uri($"http://127.0.0.1:10000/devstoreaccount1/{containerName}/{blobName}");
                pageBlobDefinitions.Add((emulatedBlobUri, containerName, blobName, size));
            }
        }

        _context.Set(pageBlobDefinitions, "PageBlobDefinitions");
    }

    [When(@"I ensure page blobs are created")]
    public async Task WhenIEnsurePageBlobsAreCreated()
    {
        var pageBlobs = new List<CloudPageBlob>();
        var pageBlobDefinitions = _context.Get<List<(Uri blobUri, string containerName, string blobName, long size)>>("PageBlobDefinitions");
        var blobClient = _context.Get<CloudBlobClient>("BlobClient");
        foreach (var (blobUri, containerName, blobName, size) in pageBlobDefinitions)
        {
            var container = blobClient.GetContainerReference(containerName);
            if (!await container.ExistsAsync())
            {
                _output.WriteLine($"creating container {containerName}");
                await container.CreateIfNotExistsAsync();
                _output.WriteLine($"setting container {containerName} permissions");
                await container.SetPermissionsAsync(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Container
                });
            }

            var blob = container.GetPageBlobReference(blobName);
            if (!await blob.ExistsAsync())
            {
                this._output.WriteLine($"creating page blob {blobUri} with size {size}");
                _output.WriteLine($"Ensure page blob {blobName} is created");
                _output.WriteLine($"Ensure page blob {blobName} is created");
                await blob.CreateAsync(size);
            }

            pageBlobs.Add(blob);
        }

        _context.Set(pageBlobs, "PageBlobs");
    }

    [Then(@"I should be able to fetch attributes of page blobs")]
    public async Task ThenIShouldBeAbleToFetchAttributesOfPageBlobs()
    {
        var pageBlobs = _context.Get<List<CloudPageBlob>>("PageBlobs");
        foreach (var pageBlob in pageBlobs)
        {
            var blob = new CloudBlob(pageBlob.Uri);
            await blob.FetchAttributesAsync();
        }
    }

    [Then(@"page blobs should have the following properties")]
    public void ThenPageBlobsShouldHaveTheFollowingProperties(Table table)
    {
        var pageBlobs = _context.Get<List<CloudPageBlob>>("PageBlobs");
        foreach (var row in table.Rows)
        {
            var containerName = row["ContainerName"];
            var blobName = row["BlobName"];
            var size = long.Parse(row["Size"]);
            var expectedBlobType = Enum.TryParse(row["BlobType"], out BlobType blobType) ? blobType : BlobType.PageBlob;
            var pageBlob = pageBlobs.Find(b => b.Container.Name == containerName && b.Name == blobName);
            pageBlob.Should().NotBeNull();
            pageBlob!.Properties.Length.Should().Be(size);
            pageBlob.Properties.BlobType.Should().Be(expectedBlobType);
        }
    }
}