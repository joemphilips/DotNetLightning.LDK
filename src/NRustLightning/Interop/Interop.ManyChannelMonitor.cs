using System;
using System.Runtime.InteropServices;
using NRustLightning.Adaptors;
using NRustLightning.Handles;

namespace NRustLightning
{
    internal static partial class Interop
    {

        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "create_many_channel_monitor",
            ExactSpelling = true)]
        private static extern FFIResult _create_many_channel_monitor(
            ref InstallWatchTx installWatchTx,
            ref InstallWatchOutPoint installWatchOutPoint,
            ref WatchAllTxn watchAllTxn,
            ref GetChainUtxo getChainUtxo,
            ref FilterBlock filterBlock,
            ref ReEntered reEntered,
            
            ref BroadcastTransaction broadcastTransaction,
            ref Log log,
            ref GetEstSatPer1000Weight getEstSatPer1000Weight,
            out ManyChannelMonitorHandle handle
            );

        internal static FFIResult create_many_channel_monitor(
            InstallWatchTx installWatchTx,
            InstallWatchOutPoint installWatchOutPoint,
            WatchAllTxn watchAllTxn,
            GetChainUtxo getChainUtxo,
            FilterBlock filterBlock,
            ReEntered reEntered,
            
            BroadcastTransaction broadcastTransaction,
            Log log,
            GetEstSatPer1000Weight getEstSatPer1000Weight,
            out ManyChannelMonitorHandle handle,
            bool check = true
            ) =>
            MaybeCheck(_create_many_channel_monitor(
                ref installWatchTx,
                ref installWatchOutPoint,
                ref watchAllTxn,
                ref getChainUtxo,
                ref filterBlock,
                ref reEntered,
                ref broadcastTransaction,
                ref log,
                ref getEstSatPer1000Weight,
                out handle
                ), check);



        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "serialize_many_channel_monitor",
            ExactSpelling = true)]
        private static extern FFIResult _serialize_many_channel_monitor(IntPtr bufOut, UIntPtr bufLen, out UIntPtr actualBufLen, ManyChannelMonitorHandle handle);

        internal static FFIResult serialize_many_channel_monitor(
            IntPtr bufOut,
            UIntPtr bufLen,
            out UIntPtr actualBufLen,
            ManyChannelMonitorHandle handle,
            bool check = true)
        {
            return MaybeCheck(_serialize_many_channel_monitor(bufOut, bufLen, out actualBufLen, handle), check);
        }
        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "deserialize_many_channel_monitor",
            ExactSpelling = true)]
        private static extern FFIResult _deserialize_many_channel_monitor(
            IntPtr bufPtr,
            UIntPtr bufLen,
            ref InstallWatchTx installWatchTx,
            ref InstallWatchOutPoint installWatchOutPoint,
            ref WatchAllTxn watchAllTxn,
            ref GetChainUtxo getChainUtxo,
            ref FilterBlock filterBlock,
            ref ReEntered reEntered,
            
            ref BroadcastTransaction broadcastTransaction,
            ref Log log,
            ref GetEstSatPer1000Weight getEstSatPer1000Weight,
            
            IntPtr outputBufPtr,
            UIntPtr outputBufLen,
            out UIntPtr actualBufLen,
            out ManyChannelMonitorHandle manyChannelMonitorHandle
            );

        internal static FFIResult deserialize_many_channel_monitor(
            IntPtr bufPtr,
            UIntPtr bufLen,
            InstallWatchTx installWatchTx,
            InstallWatchOutPoint installWatchOutPoint,
            WatchAllTxn watchAllTxn,
            GetChainUtxo getChainUtxo,
            FilterBlock filterBlock,
            ReEntered reEntered,
            
            BroadcastTransaction broadcastTransaction,
            Log log,
            GetEstSatPer1000Weight getEstSatPer1000Weight,
            
            IntPtr outputBufPtr,
            UIntPtr outputBufLen,
            out UIntPtr actualBufLen,
            out ManyChannelMonitorHandle manyChannelMonitorHandle,
            bool check = true
            )
            => MaybeCheck(
                _deserialize_many_channel_monitor(
                    bufPtr,
                    bufLen,
                    ref installWatchTx,
                    ref installWatchOutPoint,
                    ref watchAllTxn,
                    ref getChainUtxo,
                    ref filterBlock,
                    ref reEntered,
                    
                    ref broadcastTransaction,
                    ref log,
                    ref getEstSatPer1000Weight,
                    outputBufPtr,
                    outputBufLen,
                    out actualBufLen,
                    out manyChannelMonitorHandle
                ),
                check);
        
        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "tell_block_connected_after_resume",
            ExactSpelling = true)]
        private static extern FFIResult _tell_block_connected_after_resume(IntPtr blockRef, UIntPtr blockLen, uint height, ref FFIOutPoint keyRef, ManyChannelMonitorHandle handle);

        internal static FFIResult tell_block_connected_after_resume(IntPtr blockRef, UIntPtr blockLen, uint height, ref FFIOutPoint keyRef, ManyChannelMonitorHandle handle, bool check = true) =>
            MaybeCheck(_tell_block_connected_after_resume(blockRef, blockLen, height, ref keyRef, handle), check);
        
        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "tell_block_disconnected_after_resume",
            ExactSpelling = true)]
        private static extern FFIResult _tell_block_disconnected_after_resume(IntPtr blockHashRef, uint height, ref FFIOutPoint keyRef, ManyChannelMonitorHandle handle);

        internal static FFIResult tell_block_disconnected_after_resume(IntPtr blockHashRef, uint height, ref FFIOutPoint keyRef, ManyChannelMonitorHandle handle, bool check = true) =>
            MaybeCheck(_tell_block_disconnected_after_resume(blockHashRef, height, ref keyRef, handle), check);
        
        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "many_channel_monitor_get_and_clear_pending_events",
            ExactSpelling = true)]
        private static extern FFIResult _many_channel_monitor_get_and_clear_pending_events(
            ManyChannelMonitorHandle handle, IntPtr bufOut, UIntPtr bufLen, out UIntPtr actualBufLen
            );

        internal static FFIResult many_channel_monitor_get_and_clear_pending_events(
            ManyChannelMonitorHandle handle, IntPtr bufOut, UIntPtr bufLen, out UIntPtr actualBufLen, bool check = true
        ) => MaybeCheck(_many_channel_monitor_get_and_clear_pending_events(handle, bufOut, bufLen, out actualBufLen),
            check);
        
        
        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "release_many_channel_monitor",
            ExactSpelling = true)]
        private static extern FFIResult _release_many_channel_monitor(IntPtr handle);

        internal static FFIResult release_many_channel_monitor(IntPtr handle, bool check = true) =>
            MaybeCheck(_release_many_channel_monitor(handle), check);
    }
}