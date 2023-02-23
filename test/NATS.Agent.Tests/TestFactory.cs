using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NATS.Agent.Tests;
public class TestFactory {

    private static readonly Action<NatsOptions> NatsOptionsBuilder = opt => {
        opt.Username = "guest";
        opt.Password = "guest";
        opt.Servers = new[] { "nats://192.168.1.27:4222" };
        opt.Exchanges = new[] { "ais.fusion.report" };
    };


    public TestFactory() : this(s=>s.AddMessageAgent(NatsOptionsBuilder)) { }

    public TestFactory(Action<IServiceCollection> configServices) {
        var services = new ServiceCollection();
        services.AddLogging();
        configServices?.Invoke(services);
        this.Services = services.BuildServiceProvider();

        this.Scope = Services.CreateScope();
    }

    public TService GetService<TService>() where TService : notnull {
        return Services.GetRequiredService<TService>();
    }

    public IServiceScope Newscope() {
        this.Scope = Services.CreateScope();
        return Scope;
    }

    public IServiceScope Scope { get; private set; }

    public IServiceProvider Services { get; private set; }
}