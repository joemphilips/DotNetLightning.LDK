using System;
using System.Buffers;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NRustLightning.Adaptors;
using NRustLightning.Infrastructure.Models.Request;
using NRustLightning.Infrastructure.Models.Response;
using NRustLightning.Infrastructure.Networks;
using NRustLightning.Infrastructure.Utils;
using NRustLightning.RustLightningTypes;
using NRustLightning.Server.Events;
using NRustLightning.Server.Interfaces;
using NRustLightning.Server.Services;

namespace NRustLightning.Server.Controllers
{
    [ApiController]
    [Route("/v1/[controller]")]
    // [Authorize(AuthenticationSchemes = "LSAT", Policy = "Readonly")]
    public class ChannelController : ControllerBase
    {
        private readonly PeerManagerProvider _peerManagerProvider;
        private readonly NRustLightningNetworkProvider _networkProvider;
        private readonly ILogger<ChannelController> _logger;
        private readonly EventAggregator _eventAggregator;
        private readonly MemoryPool<byte> _pool;

        public ChannelController(PeerManagerProvider peerManagerProvider, NRustLightningNetworkProvider networkProvider, ILogger<ChannelController> logger, EventAggregator eventAggregator)
        {
            _peerManagerProvider = peerManagerProvider;
            _networkProvider = networkProvider;
            _logger = logger;
            _eventAggregator = eventAggregator;
            _pool = MemoryPool<byte>.Shared;
        }
        
        [HttpGet]
        [Route("{cryptoCode}")]
        public ActionResult<ChannelInfoResponse> Get(string cryptoCode)
        {
            var n = _networkProvider.GetByCryptoCode(cryptoCode.ToLowerInvariant());
            var peer = _peerManagerProvider.TryGetPeerManager(n);
            if (peer is null)
            {
                return BadRequest($"cyrptocode: {cryptoCode} not supported");
            }
            var details =  peer.ChannelManager.ListChannels(_pool);
            return new ChannelInfoResponse {Details = details};
        }

        [HttpPost]
        [Route("{cryptoCode}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ulong>> OpenChannel(string cryptoCode, [FromBody] OpenChannelRequest o)
        {
            var n = _networkProvider.GetByCryptoCode(cryptoCode.ToLowerInvariant());
            var peerMan = _peerManagerProvider.TryGetPeerManager(n);
            var ps = peerMan.GetPeerNodeIds(_pool);
            if (ps.All(p => p != o.TheirNetworkKey))
            {
                return BadRequest($"Unknown peer {o.TheirNetworkKey}. Make sure you are already connected to the peer.");
            }
            var chanMan = peerMan.ChannelManager;
            var maybeConfig = o.OverrideConfig;
            var userId = RandomUtils.GetUInt64();
            try
            {
                if (maybeConfig is null)
                    chanMan.CreateChannel(o.TheirNetworkKey, o.ChannelValueSatoshis, o.PushMSat,
                        userId);
                else
                {
                    var v = maybeConfig.Value;
                    chanMan.CreateChannel(o.TheirNetworkKey, o.ChannelValueSatoshis, o.PushMSat,
                        userId, in v);
                }

                peerMan.ProcessEvents();
                var cts = new CancellationTokenSource(20000);
                await _eventAggregator.WaitNext<Event>(
                    x => x is Event.FundingBroadcastSafe d && d.Item.UserChannelId == userId, cts.Token);
            }
            catch (FFIException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                return base.Problem(ex.Message, null, 500, "Channel opening Operation timed out");
            }

            return Ok(userId);
        }

        [HttpDelete]
        [Route("{cryptoCode}")]
        public ActionResult CloseChannel(string cryptoCode, [FromBody] CloseChannelRequest req)
        {
            var n = _networkProvider.GetByCryptoCode(cryptoCode.ToLowerInvariant());
            var peerMan = _peerManagerProvider.GetPeerManager(n);
            var channels = peerMan.ChannelManager.ListChannels(_pool);
            var s = channels.FirstOrDefault(x => x.RemoteNetworkId == req.TheirNetworkKey);
            if (s is null)
            {
                return NotFound($"There is no opened channel against {req.TheirNetworkKey}");
            }
            peerMan.ChannelManager.CloseChannel(s.ChannelId);
            peerMan.ProcessEvents();
            
            // Technically, we can await the Broadcaster broadcasts the tx. But do not do it for the because ...
            // The only way to determine the broadcasted tx is indeed a closing tx of the channel is to check that one
            // of its input's "prevHash xor prevN" matches the ChannelId, but channel id has different value when it has
            // not fully opened.
            return Ok();
        }
    }
}