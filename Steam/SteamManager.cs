using Godot;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steam;
public partial class SteamManager : Node
{
    [Export]
    public uint SteamAppId = 480; // replace with your own steam id

    public event Action<Lobby>? OnLobbySuccessfullyCreated;
    public event Action<Lobby>? OnLobbyGameCreated;
    public event Action<Friend>? OnPlayerJoinLobby;
    public event Action<Friend>? OnPlayerLeftLobby;
    public event Action<List<Lobby>>? OnLobbyRefreshCompleted;

    public string PlayerName => SteamClient.Name;
    public SteamId PlayerSteamID => SteamClient.SteamId;
    public bool IsHost { get; private set; }

    private List<Lobby> _availableLobbies = new List<Lobby>();
    private Lobby _hostedLobby;
    public override void _Ready()
    {
        InstantiateSteam();

        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedWithSteamId;
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }
    public override void _ExitTree()
    {
        SteamClient.Shutdown();
    }

    private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
    {
        OnPlayerLeftLobby?.Invoke(friend);
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        OnPlayerLeftLobby?.Invoke(friend);
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        OnPlayerJoinLobby?.Invoke(friend);
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if(result == Result.OK)
        {
            OnLobbySuccessfullyCreated?.Invoke(lobby);
        }
    }

    private void OnLobbyGameCreatedWithSteamId(Lobby lobby, uint id, ushort port, SteamId steamId)
    {
        OnLobbyGameCreated?.Invoke(lobby);
    }
    public async Task CreateLobby()
    {
        Lobby? createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(20);

        if (!createLobbyOutput.HasValue)
        {
            throw new Exception();
        }
        _hostedLobby = createLobbyOutput.Value;
        _hostedLobby.SetPublic();
        _hostedLobby.SetJoinable(true);
        _hostedLobby.SetData("ownerNameDataString", PlayerName);
        IsHost = true;
    }
    public async Task GetMultiplayerLobbies()
    {
        _availableLobbies = (await SteamMatchmaking.LobbyList.WithMaxResults(10).RequestAsync()).ToList();

        OnLobbyRefreshCompleted?.Invoke(_availableLobbies);
    }
    private void OnLobbyEntered(Lobby lobby)
    {
        if (lobby.MemberCount > 0)
        {
            _hostedLobby = lobby;
            foreach (var item in lobby.Members)
            {
                OnPlayerJoinLobby?.Invoke(item);
            }
            lobby.SetGameServer(lobby.Owner.Id);
        }
    }
    public void LeaveLobby()
    {
        if (IsHost)
        {
            IsHost = false;
        }
        _hostedLobby.Leave();
    }
    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        RoomEnter joinSuccessful = await lobby.Join();
        if (joinSuccessful != RoomEnter.Success)
        {
            throw new InvalidOperationException("Failed to Join Lobby");
        }
        else
        {
            _hostedLobby = lobby;

            foreach (Friend friend in lobby.Members)
            {
                OnPlayerJoinLobby?.Invoke(friend);
            }
        }
    }

    public void OpenFriendOverlayForInvite()
    {
        SteamFriends.OpenGameInviteOverlay(_hostedLobby.Id);
    }

    private void InstantiateSteam()
    {
        try
        {
            SteamClient.Init(SteamAppId, asyncCallbacks: true);
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.Message);
        }
    }
}
