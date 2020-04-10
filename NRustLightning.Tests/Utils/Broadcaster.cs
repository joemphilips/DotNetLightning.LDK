using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotNetLightning.LDK.Adaptors;
using DotNetLightning.LDK.Handles;
using DotNetLightning.LDK.Interfaces;
using DotNetLightning.LDK.Utils;

namespace DotNetLightning.LDK.Tests.Utils
{
    public class Broadcaster : IDisposable
    {
        private readonly BroadcasterHandle _handle;

        private Broadcaster(BroadcasterHandle handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public static Broadcaster Create(IBroadcaster broadcaster)
        {
            Interop.create_broadcaster(ref broadcaster.BroadcastTransaction, out var handle);
            return new Broadcaster(handle);
        }

        public void Broadcast()
        {
            Interop.ffi_test_broadcaster(_handle);
        }
        
        public void Dispose()
        {
            _handle.Dispose();
        }
    }
        internal class TestBroadcaster : IBroadcaster
        {
            public ConcurrentBag<string> BroadcastedTxHex { get; } = new ConcurrentBag<string>();

            public FFIBroadcastTransaction _broadcast_ptr;

            public TestBroadcaster()
            {
                _broadcast_ptr =
                    (ref FFITransaction tx) =>
                     {
                         var hex = Hex.Encode(tx.AsSpan());
                         BroadcastedTxHex.Add(hex);
                     };
            }
            ref FFIBroadcastTransaction IBroadcaster.BroadcastTransaction
                => ref _broadcast_ptr;
        }

}