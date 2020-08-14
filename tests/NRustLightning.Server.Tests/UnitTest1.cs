using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetLightning.Payment;
using DotNetLightning.Serialize;
using DotNetLightning.Utils;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Core;
using NBitcoin;
using NRustLightning.Adaptors;
using NRustLightning.Infrastructure.JsonConverters;
using NRustLightning.Infrastructure.Models.Request;
using NRustLightning.Infrastructure.Models.Response;
using NRustLightning.Infrastructure.Networks;
using NRustLightning.RustLightningTypes;
using NRustLightning.Server.Tests.Support;
using Xunit;

namespace NRustLightning.Server.Tests
{
    public class UnitTest1 : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _fixture;
        public IServiceProvider sp;
        
        public UnitTest1(CustomWebApplicationFactory fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanConvertJsonTypes()
        {
            var invoice =
                "lnbc1p0vhtzvpp5akajlfqdj6ek7eeh4kae6gc05fz9j99n8jadatqt4fmlwwxwx4zsnp4q2uqg2j52gxtxg5d0v928h5pll95ynsaek2csgfg26tvuzydgjrwgdqhdehjqer9wd3hy6tsw35k7msna3vtx";
            var paymentRequest = PaymentRequest.Parse(invoice);
            var resp = new InvoiceResponse() { Invoice = paymentRequest.ResultValue };
            var j = JsonSerializer.Serialize(resp);
            JsonSerializer.Deserialize<InvoiceResponse>(j);
            var invoiceResponseRaw = "{\"invoice\":\"lnbc1p0vma42pp5t2v5ehyay3x9g8769gqkrhmdlqjq0kc8ksqfxu3xjw7s2y96jegqnp4q2uqg2j52gxtxg5d0v928h5pll95ynsaek2csgfg26tvuzydgjrwgdqhdehjqer9wd3hy6tsw35k7ms3xhenl\"}";
            JsonSerializer.Deserialize<InvoiceResponse>(invoiceResponseRaw);


            var conf = UserConfig.GetDefault();
            j = JsonSerializer.Serialize(conf);
            var v = JsonSerializer.Deserialize<UserConfig>(j);
            Assert.Equal(conf.ChannelOptions.AnnouncedChannel, v.ChannelOptions.AnnouncedChannel);
            
            var openChannelRequest = new OpenChannelRequest();
            j = JsonSerializer.Serialize(openChannelRequest);
            var conv = JsonSerializer.Deserialize<OpenChannelRequest>(j);
            Assert.Equal(openChannelRequest.OverrideConfig, conv.OverrideConfig);
            
            // with custom config
            openChannelRequest.OverrideConfig = UserConfig.GetDefault();
            j = JsonSerializer.Serialize(openChannelRequest);
            // Don't know why but we must specify option here.
            var opt = new JsonSerializerOptions();
            opt.Converters.Add(new NullableStructConverterFactory());
            conv = JsonSerializer.Deserialize<OpenChannelRequest>(j, opt);
            
            Assert.True(conv.OverrideConfig.HasValue);
            Assert.Equal(openChannelRequest.OverrideConfig.Value.ChannelOptions.AnnouncedChannel, conv.OverrideConfig.Value.ChannelOptions.AnnouncedChannel);
            j =
                "{\"TheirNetworkKey\":\"024a8b7fc86957537bb365cc0242255582d3d40a5532489f67e700a89bcac2f010\",\"ChannelValueSatoshis\":100000,\"PushMSat\":1000,\"OverrideConfig\":null}";
            openChannelRequest = JsonSerializer.Deserialize<OpenChannelRequest>(j, new JsonSerializerOptions() { Converters = { new HexPubKeyConverter()  }});
            Assert.Equal(100000UL, openChannelRequest.ChannelValueSatoshis);
            Assert.Equal(1000UL, openChannelRequest.PushMSat);
            Assert.NotNull(openChannelRequest.TheirNetworkKey);
            
            // wallet info
            j =
                "{\"DerivationStrategy\":\"tpubDBte1PdX36pt167AFbKpHwFJqZAVVRuJSadZ49LdkX5JJbJCNDc8JQ7w5GdaDZcUXm2SutgwjRuufwq4q4soePD4fPKSZCUhqDDarKRCUen\",\"OnChainBalanceSatoshis\":0}";
            var networkProvider = new NRustLightningNetworkProvider(NetworkType.Regtest);
            var btcNetwork = networkProvider.GetByCryptoCode("BTC");
            var walletInfo = JsonSerializer.Deserialize<WalletInfo>(j, new JsonSerializerOptions {Converters = { new DerivationStrategyJsonConverter(btcNetwork.NbXplorerNetwork.DerivationStrategyFactory) }});
            
            // FeatureBit
            var featureBit = FeatureBit.TryParse("0b000000100100000100000000").ResultValue;
            var opts = new JsonSerializerOptions() {Converters = {new FeatureBitJsonConverter()}};
            j = JsonSerializer.Serialize(featureBit, opts);
            Assert.Contains("prettyPrint", j);
            var featureBit2 = JsonSerializer.Deserialize<FeatureBit>(j, opts);
            Assert.Equal(featureBit, featureBit2);
        }
        
        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanGetNewInvoice()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var resp = await c.GetInvoiceAsync(new InvoiceCreationOption());
            Assert.Equal(resp.Invoice.AmountValue, FSharpOption<LNMoney>.None);
            Assert.True(resp.Invoice.Expiry > DateTimeOffset.UnixEpoch);
            Assert.False(resp.Invoice.IsExpired);
            Assert.Null(resp.Invoice.AmountValue);
        }

        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanGetChannelList()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var resp = await c.GetChannelDetailsAsync();
            Assert.NotNull(resp);
            Assert.Empty(resp.Details);
        }

        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanGetOnChainAddress()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var resp = await c.GetNewDepositAddressAsync();
            Assert.NotNull(resp.Address);
        }

        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanGetWalletInfo()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var w = await c.GetWalletInfoAsync();
            Assert.NotNull(w);
            Assert.NotNull(w.DerivationStrategy);
        }

        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanGetInfo()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var info = await c.GetInfoAsync();
            Assert.NotNull(info);
            Assert.Equal(0, info.NumConnected);
            Assert.NotNull(info.ConnectionString);
            Assert.Empty(info.NodeIds);
        }

        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanSendOpenCloseChannelRequest()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var i = new Key().PubKey;
            
            // push_msat is larger than channel_value. so RL must complain about it.
            var request = new OpenChannelRequest{ TheirNetworkKey = i, ChannelValueSatoshis = 100000, PushMSat = 10000000000000 };
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await c.OpenChannelAsync(request));
            Assert.Contains("Unknown peer", ex.Message);

            // overriding config with bogus value will cause an error. (too short too-self-delay)
            var overrideConfig = UserConfig.GetDefault();
            var bogusOwnChannelConfig = Adaptors.ChannelHandshakeConfig.GetDefault();
            bogusOwnChannelConfig.OurToSelfDelay = 1;
            overrideConfig.OwnChannelConfig = bogusOwnChannelConfig;
            request.OverrideConfig = overrideConfig;
            ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await c.OpenChannelAsync(request));
            Assert.Contains("Unknown peer", ex.Message);
            
        }

        [Fact]
        [Trait("UnitTest", "REST")]
        public async Task CanCallSendPayment()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var c = host.GetTestNRustLightningClient();
            var e = await Assert.ThrowsAsync<HttpRequestException>(async ()  => await c.PayToInvoiceAsync("lnbcrt1p03zqaapp54vmm7lg9fjjnm6v998v8cafzqcnzecjnqlq8dk55rhgzlrgstzasdqqcqzpgsp58vt9tjxuvx8jmy4q0wtdakdu24k6zlx8np67w8pxmvcmwcacsa3q9qy9qsqx8h8dmt0fsqel426tu0ayrscaty0pt4t3tg2rz0jpl7p3xyx6hynwhhc86h7apmmf9043n250275cuuad6npdasqclk0ga9htyq0v0qqjs26z3"));
            Assert.Equal("You must specify payment amount if it is not included in invoice", e.Message);
            e = await Assert.ThrowsAsync<HttpRequestException>(async ()  => await c.PayToInvoiceAsync("lnbcrt1p03zqaapp54vmm7lg9fjjnm6v998v8cafzqcnzecjnqlq8dk55rhgzlrgstzasdqqcqzpgsp58vt9tjxuvx8jmy4q0wtdakdu24k6zlx8np67w8pxmvcmwcacsa3q9qy9qsqx8h8dmt0fsqel426tu0ayrscaty0pt4t3tg2rz0jpl7p3xyx6hynwhhc86h7apmmf9043n250275cuuad6npdasqclk0ga9htyq0v0qqjs26z3", LNMoney.MilliSatoshis(100L)));
            Assert.Contains("Cannot route when there are no outbound routes away from us", e.Message);
        }

        [Fact(Skip = "We must tackle on this when ready.")]
        public async Task LSATAuthenticationTest()
        {
            var hostBuilder = new HostBuilder().ConfigureTestHost();
            using var host = await hostBuilder.StartAsync();
            var client = host.GetTestClient();
            return;
        }
    }
}
