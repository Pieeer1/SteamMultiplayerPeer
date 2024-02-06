using Godot;
using Steamworks.Data;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using Steamworks;
using System.Linq;

namespace SteamMultiplayerPeer.Steam;
public class SteamSocketManager : SocketManager
{
    private Dictionary<Connection, List<SteamNetworkingMessage>> _connectionMessages = new Dictionary<Connection, List<SteamNetworkingMessage>>();

    private int _maxEnqueuedMessages = 1000;

    public event Action<(Connection, ConnectionInfo)>? OnConnectionEstablished;
    public event Action<(Connection, ConnectionInfo)>? OnConnectionLost;
    public event Action<(Connection, ConnectionInfo)>? OnConnectionChange;

    public override void OnConnectionChanged(Connection connection, ConnectionInfo info)
    {
        base.OnConnectionChanged(connection, info);
        OnConnectionChange?.Invoke((connection, info));
    }
    public override void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        _connectionMessages.Add(connection, new List<SteamNetworkingMessage>());
        OnConnectionEstablished?.Invoke((connection, info));
    }

    public override void OnConnecting(Connection connection, ConnectionInfo info)
    {
        base.OnConnecting(connection, info);
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo info)
    {
        base.OnDisconnected(connection, info);
        _connectionMessages.Remove(connection);
        OnConnectionLost?.Invoke((connection, info));
    }

    public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long recvTime, long messageNum, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);

        if (_connectionMessages[connection].Count > _maxEnqueuedMessages)
        {
            _connectionMessages[connection].RemoveAt(0);
        }

        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);

        _connectionMessages[connection].Add(new SteamNetworkingMessage(managedArray, identity.SteamId, MultiplayerPeer.TransferModeEnum.Reliable, recvTime));
    }

    public IEnumerable<SteamNetworkingMessage> ReceiveMessagesOnConnection(Connection connection, int maxMessageCount)
    {
        IEnumerable<SteamNetworkingMessage> steamNetworkingMessages = _connectionMessages[connection].Take(maxMessageCount).ToList();
        for (int i = 0; i < maxMessageCount; i++)
        {
            if (_connectionMessages[connection].Any())
            {
                _connectionMessages[connection].RemoveAt(0);
            }
        }
        return steamNetworkingMessages;
    }
}
