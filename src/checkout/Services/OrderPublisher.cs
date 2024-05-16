using Dapr.Client;
using System;
using System.Threading.Tasks;

public class OrderPublisher
{
    private readonly DaprClient _client;

    public OrderPublisher(DaprClient client)
    {
        _client = client;
    }

    public async Task PublishOrderAsync(Order order, string pubsubName, string topicName)
    {
        try
        {
            await _client.PublishEventAsync(pubsubName, topicName, order);
            Console.WriteLine("Published data: " + order);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}