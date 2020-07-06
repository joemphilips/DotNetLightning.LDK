using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using NRustLightning.Server.Entities;
using NRustLightning.Server.Extensions;
using NRustLightning.Server.Interfaces;
using NRustLightning.Server.Networks;

namespace NRustLightning.Server.Services
{
    public class ChannelProvider
    {
        private Dictionary<string, Channel<FeeRateSet>> _feeRateChannels = new Dictionary<string, Channel<FeeRateSet>>();
        public ChannelProvider(INBXplorerClientProvider clientProvider, NRustLightningNetworkProvider networkProvider)
        {
            foreach (var n in networkProvider.GetAll())
            {
                var maybeClient = clientProvider.TryGetClient(n.CryptoCode);
                if (maybeClient != null)
                {
                    _feeRateChannels.Add(n.CryptoCode, Channel.CreateBounded<FeeRateSet>(50));
                }
            }

        }

        public Channel<FeeRateSet> GetFeeRateChannel(NRustLightningNetwork n)
        {
            if (!_feeRateChannels.TryGetValue(n.CryptoCode, out var feeRateChannel))
            {
                throw new Exception($"Channel not found for {n.CryptoCode}! this should never happen");
            }
            return feeRateChannel;
        }
    }
}