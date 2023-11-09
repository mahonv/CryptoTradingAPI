using System.Text.Json;
using Confluent.Kafka;

namespace CryptoTrading.Infrastructure;

public class BrokerConfig
{
    public string BaseUrl { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class BrokerProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;

    public BrokerProducer(BrokerConfig config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config.BaseUrl,
            AllowAutoCreateTopics = true
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
            .SetValueSerializer(Serializers.Utf8).Build();
    }

    public async Task Publish<T>(string topic, CancellationToken cancellationToken, params T[] data)
    {
        foreach (var d in data)
        {
            await _producer.ProduceAsync(topic, new Message<string, string>()
            {
                Key = Guid.NewGuid().ToString("B"),
                Value = JsonSerializer.Serialize(d)
            }, cancellationToken);
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}