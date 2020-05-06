using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NRustLightning.Server.ModelBinders;
using static DotNetLightning.Utils.Primitives;

namespace NRustLightning.Server.Models.Request
{
    [ModelBinder(BinderType = typeof(PeerConnectionStringModelBinders))]
    public class PeerConnectionString
    {
        public PeerConnectionString(NodeId nodeId, EndPoint endPoint)
        {
            EndPoint = endPoint;
            NodeId = nodeId;
        }

        public static PeerConnectionString Create(string str)
        {
            if (TryCreate(str, out var result))
                return result;
            throw new FormatException($"Invalid connection string {str}");
        }

        public static bool TryCreate(string str, out PeerConnectionString result)
        {
            result = null;
            if (str == null) throw new ArgumentNullException(nameof(str));
            var s = str.Split("@");
            if (s.Length != 2)
                return false;
            var nodeId = NodeId.NewNodeId(new PubKey(s[0]));
            var addrAndPort = s[1].Split(":");
            if (addrAndPort.Length != 2)
                return false;
            if (!int.TryParse(addrAndPort[1], out var port))
                return false;

            EndPoint endPoint;
            if (IPAddress.TryParse(addrAndPort[0], out var ipAddr))
                endPoint = new IPEndPoint(ipAddr, port);
            else
            {
                endPoint = new DnsEndPoint(addrAndPort[0], port);
            }
            result = new PeerConnectionString(nodeId, endPoint);
            return true;
        }
        public NodeId NodeId { get; }
        public EndPoint EndPoint { get; }

        public override string ToString() => $"{NodeId.Value.ToHex()}@{EndPoint}";
    }
}