using System;
using NBitcoin;
using NRustLightning.Adaptors;
using NRustLightning.Handles;
using NRustLightning.Interfaces;
using Extensions = NRustLightning.Adaptors.Extensions;

namespace NRustLightning
{
    public class BlockNotifier : IDisposable
    {
        private readonly BlockNotifierHandle _handle;
        private readonly object[] _deps;
        private bool _disposed = false;
        
        internal BlockNotifier(
            BlockNotifierHandle handle,
            object[] deps
            )
        {
            _handle = handle;
            _deps = deps;
        }

        public static BlockNotifier Create(NBitcoin.Network nbitcoinNetwork, ILogger logger,
            IChainWatchInterface chainWatchInterface)
        {
            var loggerDelegatesHolder = new LoggerDelegatesHolder(logger);
            var chainWatchInterfaceDelegatesHolder = new ChainWatchInterfaceConverter(chainWatchInterface);
            return Create(nbitcoinNetwork, loggerDelegatesHolder, chainWatchInterfaceDelegatesHolder);
        }

        internal static BlockNotifier Create(NBitcoin.Network nbitcoinNetwork, ILoggerDelegatesHolder loggerDelegatesHolder, IChainWatchInterfaceDelegatesHolder chainWatchInterfaceDelegatesHolder)
        {
            var network = Extensions.ToFFINetwork(nbitcoinNetwork);
            Interop.create_block_notifier(in network, loggerDelegatesHolder.Log, chainWatchInterfaceDelegatesHolder.InstallWatchTx, chainWatchInterfaceDelegatesHolder.InstallWatchOutPoint, chainWatchInterfaceDelegatesHolder.WatchAllTxn, chainWatchInterfaceDelegatesHolder.GetChainUtxo, chainWatchInterfaceDelegatesHolder.FilterBlock ,chainWatchInterfaceDelegatesHolder.ReEntered, out var handle);
            return new BlockNotifier(handle, new object[]{ loggerDelegatesHolder, chainWatchInterfaceDelegatesHolder });
        }

        public void RegisterChannelManager(ChannelManager channelManager)
        {
            Interop.register_channel_manager(channelManager.Handle, _handle);
        }

        public void UnregisterChannelManager(ChannelManager channelManager)
        {
            Interop.unregister_channel_manager(channelManager.Handle, _handle);
        }

        public unsafe void BlockConnected(NBitcoin.Block block, uint height)
        {
            var blockBytes = block.ToBytes();
            fixed (byte* b = blockBytes)
            {
                Interop.block_connected(b, (UIntPtr)blockBytes.Length, height, _handle);
            }
        }

        public unsafe void BlockDisconnected(NBitcoin.BlockHeader blockHeader, uint height)
        {
            var blockHeaderBytes = blockHeader.ToBytes();
            fixed (byte* b = blockHeaderBytes)
            {
                Interop.block_disconnected(b, (UIntPtr)blockHeaderBytes.Length, height, _handle);
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _handle.Dispose();
                _disposed = true;
            }
        }
    }
}