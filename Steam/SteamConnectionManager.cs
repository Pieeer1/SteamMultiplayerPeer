using Godot;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Linq;

namespace SteamMultiplayerPeer.Steam;
public class SteamConnectionManager : ConnectionManager
{
    public event Action<ConnectionInfo>? OnConnectionEstablished;
    public event Action<ConnectionInfo>? OnConnectionLost;

    private List<SteamNetworkingMessage> _pendingMessages { get; } = new List<SteamNetworkingMessage>();
    public override void OnConnectionChanged(ConnectionInfo info)
    {
        base.OnConnectionChanged(info);
    }
    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        OnConnectionEstablished?.Invoke(info);
    }

    public override void OnConnecting(ConnectionInfo info)
    {
        base.OnConnecting(info);
    }

    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        OnConnectionLost?.Invoke(info);
    }

    public override void OnMessage(IntPtr data, int size, long recvTime, long messageNum, int channel)
    {
        base.OnMessage(data, size, messageNum, recvTime, channel);

        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);

        _pendingMessages.Add(new SteamNetworkingMessage(managedArray, ConnectionInfo.Identity.SteamId, MultiplayerPeer.TransferModeEnum.Reliable, recvTime));
    }
    public IEnumerable<SteamNetworkingMessage> GetPendingMessages(int count)
    {
        IEnumerable<SteamNetworkingMessage> messagesToSend = _pendingMessages.Take(count).ToList();

        for (int i = 0; i < count; i++)
        {
            if (_pendingMessages.Any())
            {
                _pendingMessages.RemoveAt(0);
            }
        }

        return messagesToSend;
    }
}