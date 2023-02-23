using CCHP.VTS.Events;
using Xunit.Abstractions;

namespace NATS.Agent.Tests;

public class IMessageReaderTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageReaderTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    [Fact]
    public async Task TestSubscribeShipTrackEvent() {
        var subs = factory.GetService<IMessageReader>();
        Assert.NotNull(subs);
        var channel = await subs.GetChannelAsync<ShipTrackEvent>("ais.fusion.report.ShipTrackEvent");
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
        for (int i = 0; i < 10; i++) {
            var e = await channel.ReadAsync(token);
            output.WriteJson(e);
        }
    }
}
