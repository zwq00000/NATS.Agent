
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NATS.Client;

namespace NATS.Agent;

public static class ServiceExtensions {
    /// <summary>
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddMessageAgent(this IServiceCollection services,ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.Add(new ServiceDescriptor(typeof(IMessagePublisher), typeof(NATSMessagePublisher),lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageSubscriber), typeof(NATSMessageHub),lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageHub), typeof(NATSMessageHub),lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageReader), typeof(NATSMessageAgent),lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageAgent), typeof(NATSMessageAgent),lifetime));

        return services;
    }

    /// <summary>
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionBuilder"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddMessageAgent(this IServiceCollection services, Action<NatsOptions> optionBuilder,ServiceLifetime lifetime = ServiceLifetime.Transient) {

        services.AddNATSClient(optionBuilder);
        services.AddMessageAgent(lifetime);
        return services;
    }

    public static IServiceCollection AddNATSClient(this IServiceCollection services, Action<NatsOptions> optionBuilder) {
        if (optionBuilder == null) {
            throw new ArgumentNullException(nameof(optionBuilder));
        }
        services.AddOptions<NatsOptions>().Configure(optionBuilder);
        var factory = new NATS.Client.ConnectionFactory();

        //注册 默认 INATSClient,已经连接
        services.AddTransient<IConnection>(s => {
            var options = s.GetRequiredService<IOptions<NatsOptions>>();
            return options.Value.TryConnect(factory);
        });

        return services;
    }
}