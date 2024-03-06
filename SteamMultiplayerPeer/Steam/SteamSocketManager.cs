using Godot;
using Steamworks.Data;
using Steamworks;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace Steam;
public class SteamSocketManager : SocketManager
{
    private Dictionary<Connection, Queue<SteamNetworkingMessage>> _connectionMessages = new Dictionary<Connection, Queue<SteamNetworkingMessage>>();

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
        _connectionMessages.Add(connection, new Queue<SteamNetworkingMessage>());
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

        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);

        _connectionMessages[connection].Enqueue(new SteamNetworkingMessage(managedArray, identity.SteamId, MultiplayerPeer.TransferModeEnum.Reliable, recvTime));
    }

    public IEnumerable<SteamNetworkingMessage> ReceiveMessagesOnConnection(Connection connection)
    {
        int messageCount = _connectionMessages[connection].Count;
        for (int i = 0; i < messageCount; i++)
        {
            yield return _connectionMessages[connection].Dequeue();
        }
    }
}
