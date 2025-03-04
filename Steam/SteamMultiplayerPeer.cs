﻿using Godot;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steam;
public partial class SteamMultiplayerPeer : MultiplayerPeerExtension
{
    public enum Mode
    {
        None,
        Server,
        Client
    }

    private const int _maxPacketSize = 524288; // abritrary value from steam. Fun.

    private SteamConnectionManager? _steamConnectionManager;
    private SteamSocketManager? _steamSocketManager;

    private readonly Dictionary<int, SteamConnection> _peerIdToConnection = new Dictionary<int, SteamConnection>();
    private readonly Dictionary<ulong, SteamConnection> _connectionsBySteamId = new Dictionary<ulong, SteamConnection>();

    private int _targetPeer = -1;
    private int _uniqueId = 0;
    private Mode _mode;
    private bool _isActive => _mode != Mode.None;
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
    private TransferModeEnum _transferMode = TransferModeEnum.Reliable;

    private readonly Queue<SteamPacketPeer> _incomingPackets = new Queue<SteamPacketPeer>();
    private SteamPacketPeer? _nextReceivedPacket;

    private SteamId _steamId;

    private int _transferChannel = 0;
    private bool _refuseNewConnections = false;
    public Error CreateHost(SteamId playerId)
    {
        _steamId = playerId;
        if (_isActive)
        {
            return Error.AlreadyInUse;
        }
        _steamSocketManager = SteamNetworkingSockets.CreateRelaySocket<SteamSocketManager>();
        _steamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(playerId);

        _steamSocketManager.OnConnectionEstablished += (c) =>
        {
            if (c.Item2.Identity.SteamId != _steamId)
            {
                AddConnection(c.Item2.Identity.SteamId, c.Item1);
            }
        };

        _steamSocketManager.OnConnectionLost += (c) =>
        {
            if (c.Item2.Identity.SteamId != _steamId)
            {
                EmitSignal(SignalName.PeerDisconnected, _connectionsBySteamId[c.Item2.Identity.SteamId].PeerId);
                _peerIdToConnection.Remove(_connectionsBySteamId[c.Item2.Identity.SteamId].PeerId);
                _connectionsBySteamId.Remove(c.Item2.Identity.SteamId);
            }
        };

        _uniqueId = 1;
        _mode = Mode.Server;
        _connectionStatus = ConnectionStatus.Connected;
        return Error.Ok;
    }
    public Error CreateClient(SteamId playerId, SteamId hostId)
    {
        _steamId = playerId;
        if (_isActive)
        {
            return Error.AlreadyInUse;
        }

        _uniqueId = (int)GenerateUniqueId();

        _steamConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(hostId);

        _steamConnectionManager.OnConnectionEstablished += (connection) =>
        {
            if (connection.Identity.SteamId != _steamId)
            {
                AddConnection(connection.Identity.SteamId, _steamConnectionManager.Connection);
                _connectionStatus = ConnectionStatus.Connected;
                _connectionsBySteamId[connection.Identity.SteamId].SendPeer(_uniqueId);
            }
        };
        _steamConnectionManager.OnConnectionLost += (connection) =>
        {
            if (connection.Identity.SteamId != _steamId)
            {
                EmitSignal(SignalName.PeerDisconnected, _connectionsBySteamId[connection.Identity.SteamId].PeerId);
                _peerIdToConnection.Remove(_connectionsBySteamId[connection.Identity.SteamId].PeerId);
                _connectionsBySteamId.Remove(connection.Identity.SteamId);
            }
        };

        _mode = Mode.Client;
        _connectionStatus = ConnectionStatus.Connecting;
        return Error.Ok;
    }
    public override void _Close()
    {
        if (!_isActive || _connectionStatus != ConnectionStatus.Connected) { return; }

        foreach (var connection in _steamSocketManager?.Connected ?? Enumerable.Empty<Connection>())
        {
            connection.Close();
        }
        if (_IsServer())
        {
            _steamConnectionManager?.Close();
        }

        _steamSocketManager?.Close();
        _peerIdToConnection.Clear();
        _connectionsBySteamId.Clear();
        _mode = Mode.None;
        _uniqueId = 0;
        _connectionStatus = ConnectionStatus.Disconnected;
    }
    public override void _DisconnectPeer(int pPeer, bool pForce)
    {
        SteamConnection? connection = GetConnectionFromPeer(pPeer);

        if (connection == null) { return; }

        bool res = connection.Connection.Close();

        if (!res)
        {
            return;
        }

        connection.Connection.Flush();
        _connectionsBySteamId.Remove(connection.SteamId);
        _peerIdToConnection.Remove(pPeer);
        if (_mode == Mode.Client || _mode == Mode.Server)
        {
            GetConnectionFromPeer(0)?.Connection.Flush();
        }
        if (pForce && _mode == Mode.Client)
        {
            _connectionsBySteamId.Clear();
            Close();
        }
    }

    public override int _GetAvailablePacketCount()
    {
        return _incomingPackets.Count;
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        return _connectionStatus;
    }

    public override int _GetMaxPacketSize()
    {
        return _maxPacketSize;
    }

    public override int _GetPacketChannel()
    {
        return 0; // todo - implement more channels
    }

    public override TransferModeEnum _GetPacketMode()
    {
        return _incomingPackets.FirstOrDefault()?.TransferMode ?? TransferModeEnum.Reliable;
    }

    public override int _GetPacketPeer()
    {
        return _connectionsBySteamId[_incomingPackets.FirstOrDefault()?.SenderSteamId ?? 0].PeerId;
    }
    public override byte[] _GetPacketScript()
    {
        if (_incomingPackets.TryDequeue(out SteamPacketPeer? packet))
        {
            return packet.Data;
        }

        return [];
    }


    public override int _GetTransferChannel()
    {
        return _transferChannel;
    }

    public override TransferModeEnum _GetTransferMode()
    {
        return _transferMode;
    }

    public override int _GetUniqueId()
    {
        return _uniqueId;
    }

    public override bool _IsRefusingNewConnections()
    {
        return _refuseNewConnections;
    }

    public override bool _IsServer()
    {
        return _uniqueId == 1;
    }

    public override bool _IsServerRelaySupported()
    {
        return _mode == Mode.Server || _mode == Mode.Client;
    }
    public override void _Poll()
    {
        if (_steamSocketManager != null)
        {
            _steamSocketManager.Receive();
        }

        if (_steamConnectionManager != null && _steamConnectionManager.Connected)
        {
            _steamConnectionManager.Receive();
        }

        IEnumerable<SteamNetworkingMessage> steamNetworkingMessages = _steamConnectionManager?.GetPendingMessages() ?? [];

        foreach (SteamConnection connection in _connectionsBySteamId.Values)
        {
            IEnumerable<SteamNetworkingMessage> messagesByConnection = steamNetworkingMessages.Union(_steamSocketManager?.ReceiveMessagesOnConnection(connection.Connection) ?? []);
            foreach (SteamNetworkingMessage message in messagesByConnection)
            {
                if (GetPeerIdFromSteamId(message.Sender) != -1)
                {
                    ProcessMesssage(message);
                }
                else
                {
                    SteamConnection.SetupPeerPayload? receive = message.Data.ToStruct<SteamConnection.SetupPeerPayload>();

                    ProcessPing(receive.Value, message.Sender);
                }
            }
        }
    }

    private void ProcessPing(SteamConnection.SetupPeerPayload receive, ulong sender)
    {
        SteamConnection connection = _connectionsBySteamId[sender];

        if (receive.PeerId != -1)
        {
            if (connection.PeerId == -1)
            {
                SetSteamIdPeer(sender, receive.PeerId);
            }
            if (_IsServer())
            {
                connection.SendPeer(_uniqueId);
            }

            EmitSignal(SignalName.PeerConnected, receive.PeerId);
        }
    }

    private void SetSteamIdPeer(ulong steamId, int peerId)
    {
        SteamConnection steamConnection = _connectionsBySteamId[steamId];
        if (steamConnection.PeerId == -1)
        {
            steamConnection.PeerId = peerId;
            _peerIdToConnection.Add(peerId, steamConnection);
        }
    }

    private void ProcessMesssage(SteamNetworkingMessage message)
    {
        SteamPacketPeer packet = new SteamPacketPeer(message.Data, TransferModeEnum.Reliable);
        packet.SenderSteamId = message.Sender;

        _incomingPackets.Enqueue(packet);
    }

    public override Error _PutPacketScript(byte[] pBuffer)
    {
        if (!_isActive || _connectionStatus != ConnectionStatus.Connected) { return Error.Unconfigured; }

        if (_targetPeer != 0 && !_peerIdToConnection.ContainsKey(Mathf.Abs(_targetPeer)))
        {
            return Error.InvalidParameter;
        }

        if (_mode == Mode.Client && !_peerIdToConnection.ContainsKey(1))
        {
            return Error.Bug;
        }

        SteamPacketPeer packet = new SteamPacketPeer(pBuffer, _transferMode);


        if (_targetPeer == 0)
        {
            Error error = Error.Ok;
            foreach (SteamConnection connection in _connectionsBySteamId.Values)
            {
                Error packetSendingError = connection.Send(packet);

                if (packetSendingError != Error.Ok)
                {
                    return packetSendingError;
                }
            }
            return error;
        }
        else
        {
            return GetConnectionFromPeer(_targetPeer)?.Send(packet) ?? Error.Unavailable;
        }
    }

    public override void _SetRefuseNewConnections(bool pEnable)
    {
        _refuseNewConnections = pEnable;
    }

    public override void _SetTargetPeer(int pPeer)
    {
        _targetPeer = pPeer;
    }

    public override void _SetTransferChannel(int pChannel)
    {
        _transferChannel = pChannel;
    }

    public override void _SetTransferMode(TransferModeEnum pMode)
    {
        _transferMode = pMode;
    }

    private SteamConnection? GetConnectionFromPeer(int peerId)
    {
        if (_peerIdToConnection.TryGetValue(peerId, out SteamConnection? value))
        {
            return value;
        }
        return null;
    }
    private int GetPeerIdFromSteamId(ulong steamId)
    {
        if (steamId == _steamId.Value)
        {
            return _uniqueId;
        }
        else if (_connectionsBySteamId.TryGetValue(steamId, out SteamConnection? value))
        {
            return value.PeerId;
        }
        else
        {
            return -1;
        }
    }

    private void AddConnection(SteamId steamId, Connection connection)
    {
        if (steamId == _steamId.Value)
        {
            throw new InvalidOperationException("Cannot add Self as Peer");
        }
        SteamConnection connectionData = new SteamConnection
        {
            Connection = connection,
            SteamIdRaw = steamId
        };
        _connectionsBySteamId.Add(steamId, connectionData);
    }
}