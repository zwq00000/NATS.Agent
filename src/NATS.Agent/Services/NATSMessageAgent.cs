
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace NATS.Agent;

internal class NATSMessageAgent : NATSMessagePublisher, IMessageAgent, IMessageReader {
    private readonly IConnection connection;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly ILogger<NATSMessageAgent> logger;
    private readonly IDictionary<string, IDisposable> subjectMap = new Dictionary<string, IDisposable>();
    private readonly IDictionary<Regex, Func<Msg, Task>> processMap = new Dictionary<Regex, Func<Msg, Task>>();
    private const int DefaultChannelCapacity = 10;

    public NATSMessageAgent(IConnection connection, IOptions<JsonOptions> jsonOptions, ILogger<NATSMessageAgent> logger) : base(connection) {
        this.connection = connection;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
        this.logger = logger;
    }

    public void Dispose() {
        connection.Dispose();
    }

    private Regex BuildTopicPattern(string topic) {
        var pattern = topic
                        .Replace(".", "\\.")
                        .Replace("*", "(.+)");
        logger.LogTrace("build topic match pattern '{topic}' => '{pattern}'", topic, pattern);
        return new Regex(pattern, RegexOptions.Compiled);
    }

    private Channel<MessageArgs<T>> BuildChannel<T>(string topic, int capacity = DefaultChannelCapacity) where T : class {
        var channel = Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        var pattern = BuildTopicPattern(topic);
        var convert = serializerOptions.GetDeserializer<T>();
        var subs = connection.SubscribeAsync(topic);
        subs.MessageHandler += async (_, args) => {
            var msg = args.Message;
            if (!pattern.IsMatch(topic)) {
                return;
            }
            await channel.Writer.WriteAsync(new MessageArgs<T>() {
                Topic = msg.Subject,
                Payload = msg.Data == null ? null : convert(msg.Data)
            });
        };
        return channel;
    }

    public Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class {
        var channel = Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        var pattern = BuildTopicPattern(topic);
        var convert = serializerOptions.GetDeserializer<T>();
        var subs = connection.SubscribeAsync(topic);
        subs.MessageHandler += async (_, args) => {
            var msg = args.Message;
            if (!pattern.IsMatch(topic)) {
                return;
            }
            await channel.Writer.WriteAsync(new MessageArgs<T>() {
                Topic = msg.Subject,
                Payload = msg.Data == null ? null : convert(msg.Data)
            });
        };
        subs.Start();
        return Task.FromResult(channel.Reader);
    }
}