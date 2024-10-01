using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;

public class AddProductToTable
{
    private readonly ILogger<AddProductToTable> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public AddProductToTable(ILogger<AddProductToTable> logger)
    {
        _logger = logger;
        _tableServiceClient = new TableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
    }

    [Function("AddProductToTable")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Processing product data for Table Storage.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var product = JsonConvert.DeserializeObject<ProductEntity>(requestBody);

        if (string.IsNullOrEmpty(product?.Name) || product.Price <= 0)
        {
            return new BadRequestObjectResult("Invalid product data.");
        }

        var tableClient = _tableServiceClient.GetTableClient("ProductInformation");
        await tableClient.CreateIfNotExistsAsync();

        product.PartitionKey = "1";
        product.RowKey = Guid.NewGuid().ToString();

        await tableClient.AddEntityAsync(product);
        return new OkObjectResult($"Product {product.Name} added successfully.");
    }
}

public class ProductEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public Azure.ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
}
