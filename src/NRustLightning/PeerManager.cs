using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using DotNetLightning.Utils;
using NBitcoin;
using NRustLightning.Adaptors;
using NRustLightning.Handles;
using NRustLightning.Interfaces;
using NRustLightning.Utils;
using RustLightningTypes;
using static NRustLightning.Utils.Utils;
using Network = NRustLightning.Adaptors.Network;

namespace NRustLightning
{
    public class PeerManager : IDisposable
    {
        private readonly PeerManagerHandle _handle;
        private readonly IList<object> _deps;
        private readonly Timer tick;
        private bool _disposed = false;

        public ChannelManager ChannelManager { get; }
        public BlockNotifier BlockNotifier { get; }
        
        internal PeerManager(
            PeerManagerHandle handle,
            ChannelManager channelManager,
            BlockNotifier blockNotifier,
            int tickInterval,
            IList<object> deps
            )
        {
            _deps = deps;
            tick = new Timer(_ => Tick(), null, tickInterval, tickInterval);
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            ChannelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
            BlockNotifier = blockNotifier ?? throw new ArgumentNullException(nameof(blockNotifier));
        }

        public static PeerManager Create(
            Span<byte> seed,
            NBitcoin.Network nbitcoinNetwork,
            IUserConfigProvider config,
            IChainWatchInterface chainWatchInterface,
            IKeysInterface keysInterface,
            IBroadcaster broadcaster,
            ILogger logger,
            IFeeEstimator feeEstimator,
            uint currentBlockHeight,
            int tickIntervalMSec = 30000
        )
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var c = config.GetUserConfig();
            return Create(seed, nbitcoinNetwork, in c, chainWatchInterface, keysInterface, broadcaster, logger, feeEstimator, currentBlockHeight, tickIntervalMSec);
        }

        public static PeerManager Create(
            Span<byte> seed,
            NBitcoin.Network nbitcoinNetwork,
            in UserConfig config,
            IChainWatchInterface chainWatchInterface,
            ILogger logger,
            byte[] ourNodeSecret,
            ChannelManager channelManager,
            uint currentBlockHeight,
            int tickIntervalMSec = 30000,
            IList<object> objectsToKeepAlive = null
        )
        {
            objectsToKeepAlive ??= new object[]{};
            var chainWatchInterfaceDelegatesHolder = new ChainWatchInterfaceConverter(chainWatchInterface);
            var loggerDelegatesHolder = new LoggerDelegatesHolder(logger);
            var blockNotifier = BlockNotifier.Create(chainWatchInterfaceDelegatesHolder);
            blockNotifier.RegisterChannelManager(channelManager);
            unsafe
            {
                fixed (byte* seedPtr = seed)
                fixed (UserConfig* configPtr = &config)
                fixed (byte* secretPtr = ourNodeSecret)
                {
                    Interop.create_peer_manager(
                        (IntPtr)seedPtr,
                        configPtr,
                        channelManager.Handle,
                        chainWatchInterfaceDelegatesHolder.InstallWatchTx,
                        chainWatchInterfaceDelegatesHolder.InstallWatchOutPoint,
                        chainWatchInterfaceDelegatesHolder.WatchAllTxn,
                        chainWatchInterfaceDelegatesHolder.GetChainUtxo,
                        chainWatchInterfaceDelegatesHolder.FilterBlock,
                        chainWatchInterfaceDelegatesHolder.ReEntered,
                        loggerDelegatesHolder.Log,
                        (IntPtr)secretPtr,
                        out var handle
                        );
                    objectsToKeepAlive.Add(chainWatchInterfaceDelegatesHolder);
                    objectsToKeepAlive.Add(loggerDelegatesHolder);
                    return new PeerManager(handle, channelManager, blockNotifier, tickIntervalMSec, objectsToKeepAlive);
                }
            }
        }
        public static PeerManager Create(
            Span<byte> seed,
            NBitcoin.Network nbitcoinNetwork,
            in UserConfig config,
            IChainWatchInterface chainWatchInterface,
            IKeysInterface keysInterface,
            IBroadcaster broadcaster,
            ILogger logger,
            IFeeEstimator feeEstimator,
            uint currentBlockHeight,
            int tickIntervalMSec = 30000
            )
        {
            if (chainWatchInterface == null) throw new ArgumentNullException(nameof(chainWatchInterface));
            if (keysInterface == null) throw new ArgumentNullException(nameof(keysInterface));
            if (broadcaster == null) throw new ArgumentNullException(nameof(broadcaster));
            var chainWatchInterfaceDelegatesHolder = new ChainWatchInterfaceConverter(chainWatchInterface);
            var keysInterfaceDelegatesHolder = new KeysInterfaceDelegatesHolder(keysInterface);
            var broadcasterDelegatesHolder = new BroadcasterDelegatesHolder(broadcaster, nbitcoinNetwork);
            var loggerDelegatesHolder = new LoggerDelegatesHolder(logger);
            var feeEstimatorDelegatesHolder = new FeeEstimatorDelegatesHolder(feeEstimator);

            var ourNodeSecret = keysInterface.GetNodeSecret().ToBytes();
            var chanMan = ChannelManager.Create(nbitcoinNetwork, in config, chainWatchInterfaceDelegatesHolder, keysInterfaceDelegatesHolder, loggerDelegatesHolder, broadcasterDelegatesHolder, feeEstimatorDelegatesHolder, currentBlockHeight);
            var blockNotifier = BlockNotifier.Create(chainWatchInterfaceDelegatesHolder);
            blockNotifier.RegisterChannelManager(chanMan);
            unsafe
            {
                fixed (byte* seedPtr = seed)
                fixed (UserConfig* configPtr = &config)
                fixed (byte* secretPtr = ourNodeSecret)
                {
                    Interop.create_peer_manager(
                        (IntPtr)seedPtr,
                        configPtr,
                        chanMan.Handle,
                        chainWatchInterfaceDelegatesHolder.InstallWatchTx,
                        chainWatchInterfaceDelegatesHolder.InstallWatchOutPoint,
                        chainWatchInterfaceDelegatesHolder.WatchAllTxn,
                        chainWatchInterfaceDelegatesHolder.GetChainUtxo,
                        chainWatchInterfaceDelegatesHolder.FilterBlock,
                        chainWatchInterfaceDelegatesHolder.ReEntered,
                        loggerDelegatesHolder.Log,
                        (IntPtr)secretPtr,
                        out var handle
                        );
                    return new PeerManager(handle, chanMan, blockNotifier, tickIntervalMSec,new object[]{ chainWatchInterfaceDelegatesHolder, keysInterfaceDelegatesHolder, broadcasterDelegatesHolder, loggerDelegatesHolder, feeEstimatorDelegatesHolder, });
                }
            }
        }
        
        private void Tick()
        {
            Interop.timer_tick_occured(_handle);
            ChannelManager.TimerChanFreshnessEveryMin();
        }

        public void NewInboundConnection(ISocketDescriptor descriptor)
        {
            Interop.new_inbound_connection(descriptor.Index, descriptor.SendData, descriptor.DisconnectSocket, _handle);
        }

        /// <summary>
        /// Returns noise act one (50 bytes)
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="theirNodeId"></param>
        public unsafe byte[] NewOutboundConnection(ISocketDescriptor descriptor, Span<byte> theirNodeId)
        {
            fixed (byte* p = theirNodeId)
            {
                Interop.new_outbound_connection(descriptor.Index, descriptor.SendData,
                    descriptor.DisconnectSocket, (IntPtr)p, _handle, out var initialSend);
                return initialSend.AsArray();
            }
        }
        public void WriteBufferSpaceAvail(ISocketDescriptor descriptor)
        {
            Interop.write_buffer_space_avail(descriptor.Index, descriptor.SendData, descriptor.DisconnectSocket, _handle);
        }

        public void SocketDisconnected(ISocketDescriptor descriptor)
        {
            Interop.socket_disconnected(descriptor.Index, descriptor.SendData, descriptor.DisconnectSocket, _handle);
        }

        /// <summary>
        /// If it returns `true`, we must stop feeding bytes into PeerManager for DoS prevention.
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public unsafe bool ReadEvent(ISocketDescriptor descriptor, ReadOnlySpan<byte> data)
        {
            fixed (byte* d = data)
            {
                var bytes = new FFIBytes((IntPtr)d, (UIntPtr)data.Length);
                Interop.read_event(
                    descriptor.Index, descriptor.SendData, descriptor.DisconnectSocket, ref bytes, out var shouldPause, _handle);
                return shouldPause == 1;
            }
        }

        public unsafe bool TryReadEvent(ISocketDescriptor descriptor, ReadOnlySpan<byte> data, out bool shouldPause,
            out FFIResult result)
        {
            fixed (byte* d = data)
            {
                var bytes = new FFIBytes((IntPtr)d, (UIntPtr)data.Length);
                result =
                Interop.read_event(
                    descriptor.Index, descriptor.SendData, descriptor.DisconnectSocket, ref bytes, out var shouldPauseB, _handle, false); 
                shouldPause = shouldPauseB == 1;
            }
            return result.IsSuccess;
        }

        public void ProcessEvents()
        {
            Interop.process_events(_handle);
        }

        public unsafe PubKey[] GetPeerNodeIds(MemoryPool<byte> pool)
        {
            var currentBufferSize = BUFFER_SIZE_UNIT;

            while (true)
            {
                using var memoryOwner = pool.Rent(currentBufferSize);
                var span = memoryOwner.Memory.Span;
                fixed (byte* ptr = span)
                {
                    var result = Interop.get_peer_node_ids(ptr, (UIntPtr) span.Length, out var actualNodeIdsLength,
                        _handle);
                    if ((int)actualNodeIdsLength > MAX_BUFFER_SIZE)
                    {
                        throw new FFIException(
                            $"Tried to return too long buffer form rust {currentBufferSize}. This should never happen.",
                            result);
                    }

                    if (result.IsSuccess)
                    {
                        var arr = span.Slice(0, (int)actualNodeIdsLength).ToArray();
                        return DecodePubKeyArray(arr);
                    }

                    if (result.IsBufferTooSmall)
                    {
                        currentBufferSize = (int)actualNodeIdsLength;
                        continue;
                    }

                    result.Check();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="theirNodeId"></param>
        /// <param name="paymentHash"></param>
        /// <param name="lastHops"></param>
        /// <param name="valueToSend"></param>
        /// <param name="finalCLTV"></param>
        /// <param name="paymentSecret"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PaymentSendException">When the payment fails for some reason. See its `Kind` property and rust-lightning's `PaymentSendFailure` type for more detail.</exception>
        /// <exception cref="FFIException">when other unexpected error happens in rl side.</exception>
        public void SendPayment(PubKey theirNodeId, Primitives.PaymentHash paymentHash, IList<RouteHint> lastHops,
            LNMoney valueToSend, Primitives.BlockHeightOffset32 finalCLTV, uint256? paymentSecret = null)
        {
            if (theirNodeId == null) throw new ArgumentNullException(nameof(theirNodeId));
            if (paymentHash == null) throw new ArgumentNullException(nameof(paymentHash));
            if (lastHops == null) throw new ArgumentNullException(nameof(lastHops));
            if (theirNodeId == null) throw new ArgumentNullException(nameof(theirNodeId));
            if (!theirNodeId.IsCompressed) throw new ArgumentException("pubkey not compressed");
            var pkBytes = theirNodeId.ToBytes();
            var paymentHashBytes = paymentHash.ToBytes(false);
            var routeHintBytes = lastHops.ToBytes();
            unsafe
            {
                fixed (byte* pkPtr = pkBytes)
                fixed (byte* paymentHashPtr = paymentHashBytes)
                fixed (byte* lastHopsPtr = routeHintBytes)
                {
                    var lastHopsFfiBytes = new FFIBytes((IntPtr) lastHopsPtr, (UIntPtr) routeHintBytes.Length);
                    if (paymentSecret is null)
                    {
                        Interop.send_non_mpp_payment_with_peer_manager(pkPtr, paymentHashPtr, ref lastHopsFfiBytes,
                            (ulong) valueToSend.MilliSatoshi, finalCLTV.Value, _handle, ChannelManager.Handle);
                    }
                    else
                    {
                        var paymentSecretBytes = paymentSecret.ToBytes(false);
                        fixed (byte* paymentSecretPtr = paymentSecretBytes)
                        {
                            Interop.send_mpp_payment_with_peer_manager(pkPtr, paymentHashPtr, ref lastHopsFfiBytes,
                                (ulong) valueToSend.MilliSatoshi, finalCLTV.Value, paymentSecretPtr, _handle, ChannelManager.Handle);
                        }
                    }
                }
            }
        }

        private static PubKey[] DecodePubKeyArray(byte[] arr)
        {
            var nPubKey = arr[0..2].ToUInt16BE();
            var result = new PubKey[nPubKey];
            arr = arr[2..];
            Debug.Assert(nPubKey * 33 == arr.Length, $"Length must be multiple of 33. it was {arr.Length}. but length was {nPubKey}");
            for (int i = 0; i < nPubKey; i++)
            {
                var s = arr[(i * 33)..((i + 1) * 33)];
                result[i] = new PubKey(s);
            }

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                tick.Dispose();
                ChannelManager.Dispose();
                BlockNotifier.Dispose();
                _handle.Dispose();
                _disposed = true;
            }
        }
    }
}