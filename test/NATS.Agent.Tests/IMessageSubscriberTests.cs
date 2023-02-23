using CCHP.VTS.Events;
using Xunit.Abstractions;

namespace NATS.Agent.Tests;

public class IMessageSubscriberTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageSubscriberTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    [Fact]
    public async Task TestSubscribe() {
        var subs = factory.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);
        var s = await subs.SubscribeAsync<string>("ais.fusion.report.ShipTrackEvent");
        var count = 0;
        s.Subscribe(data => {
            count++;
            output.WriteJson(data);
        });
        await Task.Delay(1000 * 10);
        Assert.True(count > 0);
    }

    [Fact]
    public async Task TestSubscribeShipTrackEvent() {
        var subs = factory.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);
        var s = await subs.SubscribeAsync<ShipTrackEvent>("ais.fusion.report.ShipTrackEvent");
        var count = 0;
        s.Subscribe(data => {
            count++;
            output.WriteJson(data);
        });
        await Task.Delay(1000 * 10);
        Assert.True(count > 0);
    }
}