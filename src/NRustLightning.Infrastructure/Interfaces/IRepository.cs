using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetLightning.Payment;
using DotNetLightning.Utils;
using NBitcoin;
using NRustLightning.Adaptors;

namespace NRustLightning.Infrastructure.Interfaces
{
    public interface IRepository
    {
        
        
        Task<Primitives.PaymentPreimage?> GetPreimage(Primitives.PaymentHash hash, CancellationToken ct = default);
        Task SetPreimage(Primitives.PaymentPreimage paymentPreimage, CancellationToken ct = default);

        Task<PaymentRequest?> GetInvoice(Primitives.PaymentHash hash, CancellationToken ct = default);
        Task SetInvoice(PaymentRequest paymentRequest, CancellationToken ct = default);

        IAsyncEnumerable<EndPoint> GetAllRemoteEndPoint(CancellationToken ct = default);

        Task SetRemoteEndPoint(EndPoint remoteEndPoint, CancellationToken ct = default);
        Task RemoveRemoteEndPoint(EndPoint remoteEndPoint, CancellationToken ct = default);

        /// <summary>
        /// Returns ChannelManager with its latest block hash when it is serialized.
        /// You probably want to catch up to the new latest tip by `IBlockSource`'s method.
        /// </summary>
        /// <param name="readArgs"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(uint256, ChannelManager)?> GetChannelManager(ChannelManagerReadArgs readArgs, CancellationToken ct = default);
        Task SetChannelManager(ChannelManager channelManager, CancellationToken ct = default);

        /// <summary>
        /// Returns ChannelMonitor with its latest block hash when it is serialized.
        /// You probably want to catch up to the new latest tip by `IBlockSource`'s method.
        /// </summary>
        /// <param name="readArgs"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(ManyChannelMonitor, Dictionary<Primitives.LNOutPoint, uint256>)?> GetManyChannelMonitor(ManyChannelMonitorReadArgs readArgs, CancellationToken ct = default);
        Task SetManyChannelMonitor(ManyChannelMonitor manyChannelMonitor, CancellationToken ct = default);
    }
}