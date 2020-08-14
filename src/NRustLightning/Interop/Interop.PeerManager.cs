using System;
using System.Runtime.InteropServices;
using Microsoft.FSharp.Core;
using NRustLightning.Adaptors;
using NRustLightning.Handles;

namespace NRustLightning
{
    internal static partial class Interop
    {
        [DllImport(RustLightning, EntryPoint = "create_peer_manager", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe FFIResult _create_peer_manager(
            IntPtr seedPtr,
            UserConfig* userConfig,
            
            ChannelManagerHandle channelManagerHandle,
            ref InstallWatchTx installWatchTx,
            ref InstallWatchOutPoint installWatchOutPoint,
            ref WatchAllTxn watchAllTxn,
            ref GetChainUtxo getChainUtxo,
            ref FilterBlock filterBlock,
            ref ReEntered reEntered,
            
            ref Log log,
            
            IntPtr ourNodeSecret,
            out PeerManagerHandle handle
            );

        internal static unsafe FFIResult create_peer_manager(
            IntPtr seed,
            UserConfig* userConfig,
            
            ChannelManagerHandle channelManagerHandle,
            InstallWatchTx installWatchTx,
            InstallWatchOutPoint installWatchOutPoint,
            WatchAllTxn watchAllTxn,
            GetChainUtxo getChainUtxo,
            FilterBlock filterBlock,
            ReEntered reEntered,
            
            Log log,
            
            IntPtr ourNodeSecret,
            out PeerManagerHandle handle,
            bool check = true
            ) =>
            MaybeCheck(_create_peer_manager(
                seed,
                userConfig,
                channelManagerHandle,
                ref installWatchTx,
                ref installWatchOutPoint,
                ref watchAllTxn,
                ref getChainUtxo,
                ref filterBlock,
                ref reEntered,
                
                ref log,
                ourNodeSecret,
                out handle),
                check);
        
        [DllImport(RustLightning, EntryPoint = "create_peer_manager_from_net_graph", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe FFIResult _create_peer_manager_from_net_graph(
            IntPtr seedPtr,
            UserConfig* userConfig,
            
            ChannelManagerHandle channelManagerHandle,
            ref InstallWatchTx installWatchTx,
            ref InstallWatchOutPoint installWatchOutPoint,
            ref WatchAllTxn watchAllTxn,
            ref GetChainUtxo getChainUtxo,
            ref FilterBlock filterBlock,
            ref ReEntered reEntered,
            
            ref Log log,
            
            IntPtr ourNodeSecret,
            IntPtr networkGraphBufPtr,
            UIntPtr networkGraphBufLen,
            out PeerManagerHandle handle
            );

        internal static unsafe FFIResult create_peer_manager_from_net_graph(
            IntPtr seed,
            UserConfig* userConfig,
            
            ChannelManagerHandle channelManagerHandle,
            InstallWatchTx installWatchTx,
            InstallWatchOutPoint installWatchOutPoint,
            WatchAllTxn watchAllTxn,
            GetChainUtxo getChainUtxo,
            FilterBlock filterBlock,
            ReEntered reEntered,
            
            Log log,
            
            IntPtr ourNodeSecret,
            IntPtr networkGraphBufPtr,
            UIntPtr networkGraphBufLen,
            out PeerManagerHandle handle,
            bool check = true
            ) =>
            MaybeCheck(_create_peer_manager_from_net_graph(
                seed,
                userConfig,
                channelManagerHandle,
                ref installWatchTx,
                ref installWatchOutPoint,
                ref watchAllTxn,
                ref getChainUtxo,
                ref filterBlock,
                ref reEntered,
                
                ref log,
                ourNodeSecret,
                networkGraphBufPtr,
                networkGraphBufLen,
                out handle),
                check);
        
        
        [DllImport(RustLightning, EntryPoint = "new_inbound_connection", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _new_inbound_connection(
            UIntPtr index,
            ref SendData sendData,
            ref DisconnectSocket disconnectSocket,
            PeerManagerHandle handle
            );

        internal static FFIResult new_inbound_connection(
            UIntPtr index,
            SendData sendData,
            DisconnectSocket disconnectSocket,
            PeerManagerHandle handle,
            bool check = true
        ) => MaybeCheck(_new_inbound_connection(index, ref sendData, ref disconnectSocket, handle), check);
        
        [DllImport(RustLightning, EntryPoint = "new_outbound_connection", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _new_outbound_connection(
            UIntPtr index,
            ref SendData sendData,
            ref DisconnectSocket disconnectSocket,
            IntPtr theirNodeId,
            PeerManagerHandle handle,
            out ActOne initialSend
            );

        internal static FFIResult new_outbound_connection(
            UIntPtr index,
            SendData sendData,
            DisconnectSocket disconnectSocket,
            IntPtr theirNodeId,
            PeerManagerHandle handle,
            out ActOne initialSend,
            bool check = true
        ) => MaybeCheck(_new_outbound_connection(index, ref sendData, ref disconnectSocket, theirNodeId, handle, out initialSend), check);
        
        [DllImport(RustLightning, EntryPoint = "timer_tick_occured", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _timer_tick_occured(PeerManagerHandle handle);

        internal static FFIResult timer_tick_occured(PeerManagerHandle handle, bool check = true) => MaybeCheck(_timer_tick_occured(handle), check);

        [DllImport(RustLightning, EntryPoint = "write_buffer_space_avail", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _write_buffer_space_avail(UIntPtr index, ref SendData sendData, ref DisconnectSocket disconnectSocket, PeerManagerHandle handle);

        internal static FFIResult write_buffer_space_avail(UIntPtr index, SendData sendData,
            DisconnectSocket disconnectSocket, PeerManagerHandle handle, bool check = true)
            => MaybeCheck(_write_buffer_space_avail(index, ref sendData, ref disconnectSocket, handle), check);
        
        [DllImport(RustLightning, EntryPoint = "read_event", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _read_event(UIntPtr index, ref SendData sendData, ref DisconnectSocket disconnectSocket,  ref FFIBytes data, out byte shouldPause, PeerManagerHandle handle);

        internal static FFIResult read_event(UIntPtr index, SendData sendData,
            DisconnectSocket disconnectSocket, ref FFIBytes data, out byte shouldPause, PeerManagerHandle handle,  bool check = true)
            => MaybeCheck(_read_event(index, ref sendData, ref disconnectSocket, ref data, out shouldPause, handle), check);
        
        [DllImport(RustLightning, EntryPoint = "process_events", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _process_events(PeerManagerHandle handle);

        internal static FFIResult process_events(PeerManagerHandle handle, bool check = true)
            => MaybeCheck(_process_events(handle), check);

        [DllImport(RustLightning, EntryPoint = "socket_disconnected", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _socket_disconnected(UIntPtr index, ref SendData sendData, ref DisconnectSocket disconnectSocket, PeerManagerHandle handle);

        internal static FFIResult socket_disconnected(UIntPtr index, SendData sendData, DisconnectSocket disconnectSocket, PeerManagerHandle handle, bool check = true) =>
            MaybeCheck(_socket_disconnected(index, ref sendData, ref disconnectSocket, handle), check);
        
        
        [DllImport(RustLightning, EntryPoint = "get_peer_node_ids", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe FFIResult _get_peer_node_ids(byte* bufOut, UIntPtr bufLen, out UIntPtr actualNodeIdsLength, PeerManagerHandle handle);

        internal static unsafe FFIResult get_peer_node_ids(byte* bufOut, UIntPtr bufLen, out UIntPtr actualNodeIdsLength, PeerManagerHandle handle, bool check = true)
        {
            return MaybeCheck(_get_peer_node_ids(bufOut, bufLen, out actualNodeIdsLength, handle), check);
        }

        [DllImport(RustLightning, EntryPoint = "get_network_graph", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _get_network_graph(
            IntPtr bufOut,
            UIntPtr bufLen,
            out UIntPtr actualLen,
            PeerManagerHandle peerManagerHandle
            );

        internal static FFIResult get_network_graph(
            IntPtr bufOut,
            UIntPtr bufLen,
            out UIntPtr actualLen,
            PeerManagerHandle peerManagerHandle,
            bool check = true
        ) => MaybeCheck(_get_network_graph(bufOut, bufLen, out actualLen, peerManagerHandle), check);

        
        [DllImport(RustLightning, EntryPoint = "release_peer_manager", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern FFIResult _release_peer_manager(IntPtr handle);

        internal static FFIResult release_peer_manager(IntPtr handle, bool check = true) =>
            MaybeCheck(_release_peer_manager(handle), check);
        
    }
}