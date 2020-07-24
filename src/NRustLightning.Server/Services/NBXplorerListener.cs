using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DotNetLightning.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBXplorer;
using NBXplorer.Models;
using NRustLightning.Infrastructure.Entities;
using NRustLightning.Infrastructure.Networks;
using NRustLightning.Server.Interfaces;

namespace NRustLightning.Server.Services
{
    
    public class NBXplorerListeners : IHostedService
    {
        private Dictionary<string, NBXplorerListener> _listeners = new Dictionary<string, NBXplorerListener>();
        
        public NBXplorerListeners(NRustLightningNetworkProvider networkProvider, INBXplorerClientProvider clientProvider, PeerManagerProvider peerManagerProvider, ILoggerFactory loggerFactory,
            ChannelProvider channelProvider)
        {
            foreach (var n in networkProvider.GetAll())
            {
                var cli = clientProvider.TryGetClient(n);
                if (cli != null)
                {
                    var listener = new NBXplorerListener(cli, peerManagerProvider, loggerFactory.CreateLogger<NBXplorerListener>(), channelProvider.GetFeeRateChannel(n).Writer, n);
                    _listeners.TryAdd(n.CryptoCode, listener);
                }
            }
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(_listeners.Select(l => l.Value.StartAsync(cancellationToken)));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(_listeners.Select(l => l.Value.StopAsync(cancellationToken)));
        }
    }
    public class NBXplorerListener : BackgroundService
    {
        private readonly ExplorerClient _explorerClient;
        private readonly PeerManagerProvider _peerManagerProvider;
        private readonly ILogger<NBXplorerListener> _logger;
        private readonly ChannelWriter<FeeRateSet> _feeRateWriter;
        private readonly NRustLightningNetwork _network;
        private long lastEventId = 0;

        public NBXplorerListener(
            ExplorerClient explorerClient,
            PeerManagerProvider peerManagerProvider,
            ILogger<NBXplorerListener> logger,
            ChannelWriter<FeeRateSet> feeRateWriter,
            NRustLightningNetwork network)
        {
            _explorerClient = explorerClient;
            _peerManagerProvider = peerManagerProvider;
            _logger = logger;
            _feeRateWriter = feeRateWriter;
            _network = network;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var session = _explorerClient.CreateLongPollingNotificationSession();
            await ListenToSessionLoop(session, stoppingToken);
        }

        private async Task ListenToSessionLoop(LongPollingNotificationSession session, CancellationToken stoppingToken)
        {
            var client = session.Client;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var events =
                        (await session.GetEventsAsync(lastEventId, 10000, true, stoppingToken))
                        .Where(e => e.EventType == "newblock")
                        .Select(e => (NewBlockEvent) e)
                        .OrderBy(e => e.Height);

                    foreach (var e in events)
                    {
                        var newBlock = await client.RPCClient.GetBlockAsync(e.Hash).ConfigureAwait(false);
                        var peerMan = _peerManagerProvider.GetPeerManager(_network);
                        peerMan.BlockNotifier.BlockConnected(newBlock, (uint) e.Height);
                        lastEventId = e.EventId > lastEventId ? e.EventId : lastEventId;
                    }

                    try
                    {
                        var h = await _explorerClient.GetFeeRateAsync(FeeRateSet.HighPriorityBlockCount, stoppingToken);
                        var n = await _explorerClient.GetFeeRateAsync(FeeRateSet.NormalBlockCount, stoppingToken);
                        var b = await _explorerClient.GetFeeRateAsync(FeeRateSet.BackgroundBlockCount, stoppingToken);
                        _feeRateWriter.TryWrite(new FeeRateSet() { HighPriority = h.FeeRate, Normal =  n.FeeRate, Background = b.FeeRate});
                    }
                    catch (NBXplorerException ex)
                    {
                        _logger.LogError($"Failed to estimate fee by nbxplorer: \"{ex.Message}\"");
                    }

                    // nbx does not return blocks aligned with eventId(i.e. sometimes block height will decrease when
                    // eventId increase. event if there are no forks. This is especially true in regtest, where many blocks
                    // are generated at once.)
                    // so to get the blocks in batch and to sort it by its height in our side, we will limit our query
                    // frequency by waiting in here.
                    await Task.Delay(6000, stoppingToken);
                }
                catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
                {
                }
                catch (OperationCanceledException ex)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Failed to listen on nbx {ex}");
                    break;
                }
            }

            _logger.LogInformation($"Shutting down nbx listener session loop...");
        }
    }
}