using System;
using Microsoft.Extensions.Logging;
using NBXplorer;
using NBXplorer.Models;
using NRustLightning.Adaptors;
using NRustLightning.Interfaces;

namespace NRustLightning.Server.FFIProxies
{
    public class NbXplorerFeeEstimator : IFeeEstimator
    {
        private readonly ExplorerClient _client;
        private readonly ILogger<NbXplorerFeeEstimator> _logger;
        private uint _cachedFee { get; set; } = 1000;
        public NbXplorerFeeEstimator(ExplorerClient client, ILogger<NbXplorerFeeEstimator> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger;
        }
        public uint GetEstSatPer1000Weight(FFIConfirmationTarget target)
        {
            var blockCountTarget =
                target switch
                {
                    FFIConfirmationTarget.Background => 30,
                    FFIConfirmationTarget.Normal => 6,
                    FFIConfirmationTarget.HighPriority => 1,
                    _ => throw new Exception("Unreachable!")
                };
            GetFeeRateResult resp;
            try
            {
                resp = _client.GetFeeRate(blockCountTarget);
            }
            catch (NBXplorerException ex)
            {
                _logger.LogError($"Failed to estimate fee by nbxplorer: \"{ex.Message}\"");
                _logger.LogWarning($"So we are using fallback fee {_cachedFee}");
                return _cachedFee;
            }

            // RL assumes fees for 1000 *weight-units* which is 4 times smaller than that of 1000 *virtual bytes*
            var virtualSize = 250;
            var newFee = (uint)resp.FeeRate.GetFee(virtualSize).Satoshi;
            _cachedFee = newFee;
            return _cachedFee;
        }
    }
}