
using NATS.Client;
using System.Text;
using System.Text.Json;

namespace NATS.Agent;

internal class NATSMessagePublisher : IMessagePublisher {
    private readonly IConnection connection;

    public NATSMessagePublisher(IConnection connection) {
        this.connection = connection;
    }

    public Task PublishAsync<T>(string topic, T? payload, JsonSerializerOptions? options = null, bool retain = false, CancellationToken cancellationToken = default) where T : class {
        var data = SerializeExtensions.Serialize<T>(payload, options);
        connection.Publish(topic, data);

        return Task.CompletedTask;
    }

    public Task PublishStringAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default) {
        var data = Encoding.UTF8.GetBytes(payload);
        connection.Publish(topic, data);

        return Task.CompletedTask;
    }
}
