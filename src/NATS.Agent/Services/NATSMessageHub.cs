
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NATS.Agent;

internal class NATSMessageHub : NATSMessagePublisher, IMessageHub {
    private readonly IConnection connection;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly ILogger<NATSMessageHub> logger;
    private readonly IDictionary<string, IDisposable> subjectMap = new Dictionary<string, IDisposable>();
    private readonly IDictionary<Regex, Func<Msg, Task>> processMap = new Dictionary<Regex, Func<Msg, Task>>();
    private bool _isDisposed = false;

    public NATSMessageHub(IConnection connection, IOptions<JsonOptions> jsonOptions, ILogger<NATSMessageHub> logger) : base(connection) {
        this.connection = connection;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
        this.logger = logger;
    }

    public void Dispose() {
        if (_isDisposed) {
            return;
        }
        this._isDisposed = true;
        connection.Dispose();
        foreach (var item in subjectMap.Values) {
            item.Dispose();
        }
    }

    private Regex BuildTopicPattern(string topic) {
        var pattern = topic
                        .Replace(".", "\\.")
                        .Replace("*", "(.+)");
        logger.LogTrace("build topic match pattern '{topic}' => '{pattern}'", topic, pattern);
        return new Regex(pattern, RegexOptions.Compiled);
    }

    ///<summary>
    /// 构造 消息订阅
    ///</summary>
    private Subject<MessageArgs<T>> BuildSubject<T>(string topic) where T : class {
        var pattern = BuildTopicPattern(topic);
        var subject = new Subject<MessageArgs<T>>();
        var convert = serializerOptions.GetDeserializer<T>();
        processMap.Add(pattern, msg => {
            try {
                subject.OnNext(new MessageArgs<T>() {
                    Topic = msg.Subject,
                    Payload = msg.Data == null ? null : convert(msg.Data)
                });
            } catch (JsonException ex) {
                logger.LogWarning(ex, "订阅 {topic} 解析 {type} 发生异常,{msg}", topic, typeof(T).Name, ex.Message);
                logger.LogInformation("source:{payload}", Encoding.UTF8.GetString(msg.Data));
            }
            return Task.CompletedTask;
        });
        return subject;
    }

    public IObservable<MessageArgs<T>> GetSubject<T>(string topic) where T : class {
        if (this.subjectMap.TryGetValue(topic, out var disposable)) {
            return (IObservable<MessageArgs<T>>)disposable;
        }
        var subject = BuildSubject<T>(topic);
        subjectMap.Add(topic, subject);
        return subject;
    }

    public Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class {
        if (this.subjectMap.TryGetValue(topic, out var disposable)) {
            return Task.FromResult((IObservable<MessageArgs<T>>)disposable);
        }
        var subs = connection.SubscribeAsync(topic);
        subs.MessageHandler += MessageHandler;
        subs.Start();
        return Task.FromResult(GetSubject<T>(topic));
    }

    private void MessageHandler(object? sender, MsgHandlerEventArgs e) {
        var msg = e.Message;
        foreach (var kv in processMap) {
            if (kv.Key.IsMatch(msg.Subject)) {
                kv.Value(msg);
                return;
            }
        }
    }
}


internal readonly struct TokenOf<T> { }