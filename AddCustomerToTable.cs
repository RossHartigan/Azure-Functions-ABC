using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;

public class AddCustomerToTable
{
    private readonly ILogger<AddCustomerToTable> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public AddCustomerToTable(ILogger<AddCustomerToTable> logger)
    {
        _logger = logger;
        _tableServiceClient = new TableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
    }

    [Function("AddCustomerToTable")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Processing customer data for Table Storage.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var customer = JsonConvert.DeserializeObject<CustomerEntity>(requestBody);

        if (string.IsNullOrEmpty(customer?.Name) || string.IsNullOrEmpty(customer?.Email))
        {
            _logger.LogError("Invalid customer data.");
            return new BadRequestObjectResult("Invalid customer data.");
        }

        var tableClient = _tableServiceClient.GetTableClient("CustomerProfiles");
        await tableClient.CreateIfNotExistsAsync();

        customer.PartitionKey = "1";
        customer.RowKey = Guid.NewGuid().ToString();

        _logger.LogInformation($"Adding customer to table: Name={customer.Name}, Email={customer.Email}");

        try
        {
            await tableClient.AddEntityAsync(customer);
            _logger.LogInformation("Customer added to table successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding customer to table: {ex.Message}");
            return new StatusCodeResult(500); 
        }

        return new OkObjectResult($"Customer {customer.Name} added successfully.");
    }

}

public class CustomerEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public Azure.ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}
