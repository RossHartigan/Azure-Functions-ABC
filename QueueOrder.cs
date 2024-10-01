using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using Newtonsoft.Json;

public class QueueOrder
{
    private readonly ILogger<QueueOrder> _logger;
    private readonly QueueClient _queueClient;

    public QueueOrder(ILogger<QueueOrder> logger)
    {
        _logger = logger;
        _queueClient = new QueueClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "order-processing");
    }

    [Function("QueueOrder")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Queuing order message.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var orderMessage = requestBody;

        if (string.IsNullOrEmpty(orderMessage))
        {
            return new BadRequestObjectResult("Invalid order data.");
        }

        await _queueClient.CreateIfNotExistsAsync();
        await _queueClient.SendMessageAsync(orderMessage);

        return new OkObjectResult("Order message queued successfully.");
    }
}
