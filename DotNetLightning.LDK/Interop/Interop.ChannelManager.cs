using System;
using System.Runtime.InteropServices;
using DotNetLightning.LDK.Handles;
using DotNetLightning.LDK.Adaptors;

namespace DotNetLightning.LDK
{
    internal static partial class Interop
    {
            
        private static FFIResult MaybeCheck(FFIResult result, bool check)
        {
            return check ? result.Check() : result;
        }

        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "create_channel_manager",
            ExactSpelling = true)]
        private static unsafe extern FFIResult _create_ffi_channel_manager(
            byte* seed_ptr,
            UIntPtr seed_len,
            in Network n,
            in UserConfig config,
            
            ref InstallWatchTx installWatchTx,
            ref InstallWatchOutPoint installWatchOutPoint,
            ref WatchAllTxn watchAllTxn,
            ref GetChainUtxo getChainUtxo,
            ref FilterBlock filterBlock,
            ref FFIBroadcastTransaction broadcastTransaction,
            ref Log log,
            ref FFIGetEstSatPer1000Weight getEstSatPer1000Weight,
            ulong current_block_height,
            out ChannelManagerHandle handle
            );

        internal static unsafe FFIResult create_ffi_channel_manager(
            byte* seed_ptr,
            UIntPtr seed_len,
            in Network n,
            in UserConfig config,
            ref InstallWatchTx installWatchTx,
            ref InstallWatchOutPoint installWatchOutPoint,
            ref WatchAllTxn watchAllTxn,
            ref GetChainUtxo getChainUtxo,
            ref FilterBlock filterBlock,
            ref FFIBroadcastTransaction broadcastTransaction,
            ref Log log,
            ref FFIGetEstSatPer1000Weight getEstSatPer1000Weight,
            ulong current_block_height,
            out ChannelManagerHandle handle,
            bool check = true
        )
        {
            return MaybeCheck(_create_ffi_channel_manager(seed_ptr, seed_len, in n , in config, ref installWatchTx, ref installWatchOutPoint, ref watchAllTxn, ref getChainUtxo, ref filterBlock, ref broadcastTransaction, ref log, ref getEstSatPer1000Weight, current_block_height, out handle), check);
        }

        [DllImport(RustLightning,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "release_ffi_channel_manager",
            ExactSpelling = true)]
        static unsafe extern FFIResult _release_ffi_channel_manager(IntPtr chan_man);

        internal static FFIResult release_ffi_channel_manager(
            IntPtr chan_man,
            bool check = true
        ) => MaybeCheck(_release_ffi_channel_manager(chan_man), check);
    }
}