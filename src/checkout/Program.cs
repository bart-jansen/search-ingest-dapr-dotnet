using Dapr.Client;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        const string pubsubName = "orderpubsub";
        const string topicName = "orders";

        using var client = new DaprClientBuilder().Build();
        var orderPublisher = new OrderPublisher(client);

        for (int i = 1; i <= 10; i++)
        {
            var order = new Order(i);
            await orderPublisher.PublishOrderAsync(order, pubsubName, topicName);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}