using Steam;
using Steamworks;
using Steamworks.Data;
using System;
using System.Threading.Tasks;

namespace SteamMultiplayerPeer.Tests;
[TestSuite]
public class SteamManagerTests
{
    private SteamManager _steamManager;
    public SteamManagerTests()
    {
        _steamManager = new SteamManager();
        _steamManager._Ready();

        if (!SteamClient.IsValid)
        {
            throw new Exception("Error: Steam Connection Failed: Make sure Steam is running for these tests to run. Unfortunately this is a limitation");
        }
    }


    [TestCase]
    public void Test_SteamManager_Initialize()
    {
        AssertThat(_steamManager.PlayerName).IsNotNull();
        AssertThat(_steamManager.PlayerSteamID != 0).IsTrue();
    }

    [TestCase]
    public async Task Test_SteamManager_InitializeLobby()
    {
        int successfullyCreatedLobbyCount = 0;
        int lobbyGameCreatedCount = 0;
        int playerJoinLobbyCount = 0;
        int playerLeftLobbyCount = 0;

        _steamManager.OnLobbySuccessfullyCreated += (l) => successfullyCreatedLobbyCount++;
        _steamManager.OnLobbyGameCreated += (l) => lobbyGameCreatedCount++;
        _steamManager.OnPlayerJoinLobby += (f) => playerJoinLobbyCount++;
        _steamManager.OnPlayerLeftLobby += (f) => playerLeftLobbyCount++;

        await _steamManager.CreateLobby();

        AssertThat(_steamManager.IsHost).IsTrue();

        Lobby lobby = (Lobby)typeof(SteamManager).GetField("_hostedLobby", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(_steamManager)!;
        AssertThat(lobby.Owner.Id == _steamManager.PlayerSteamID).IsTrue();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(0);
        AssertThat(playerLeftLobbyCount).Equals(0);

    }
    [TestCase]
    public async Task Test_SteamManager_JoinLobby()
    {
        int successfullyCreatedLobbyCount = 0;
        int lobbyGameCreatedCount = 0;
        int playerJoinLobbyCount = 0;
        int playerLeftLobbyCount = 0;

        _steamManager.OnLobbySuccessfullyCreated += (l) => successfullyCreatedLobbyCount++;
        _steamManager.OnLobbyGameCreated += (l) => lobbyGameCreatedCount++;
        _steamManager.OnPlayerJoinLobby += (f) => playerJoinLobbyCount++;
        _steamManager.OnPlayerLeftLobby += (f) => playerLeftLobbyCount++;
 
        await _steamManager.CreateLobby();

        AssertThat(_steamManager.IsHost).IsTrue();

        Lobby lobby = (Lobby)typeof(SteamManager).GetField("_hostedLobby", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(_steamManager)!;
        AssertThat(lobby.Owner.Id == _steamManager.PlayerSteamID).IsTrue();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(0);
        AssertThat(playerLeftLobbyCount).Equals(0);

        SteamManager joiningSteamManager = new SteamManager();

        await lobby.Join();

        AssertThat(joiningSteamManager.IsHost).IsFalse();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(1);
        AssertThat(playerLeftLobbyCount).Equals(0);
    }

    [TestCase]
    public async Task Test_SteamManager_SelfLeaveLobby()
    {
        int successfullyCreatedLobbyCount = 0;
        int lobbyGameCreatedCount = 0;
        int playerJoinLobbyCount = 0;
        int playerLeftLobbyCount = 0;

        _steamManager.OnLobbySuccessfullyCreated += (l) => successfullyCreatedLobbyCount++;
        _steamManager.OnLobbyGameCreated += (l) => lobbyGameCreatedCount++;
        _steamManager.OnPlayerJoinLobby += (f) => playerJoinLobbyCount++;
        _steamManager.OnPlayerLeftLobby += (f) => playerLeftLobbyCount++;

        await _steamManager.CreateLobby();

        AssertThat(_steamManager.IsHost).IsTrue();

        Lobby lobby = (Lobby)typeof(SteamManager).GetField("_hostedLobby", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(_steamManager)!;
        AssertThat(lobby.Owner.Id == _steamManager.PlayerSteamID).IsTrue();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(0);
        AssertThat(playerLeftLobbyCount).Equals(0);

        _steamManager.LeaveLobby();

        AssertThat(_steamManager.IsHost).IsFalse();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(0);
        AssertThat(playerLeftLobbyCount).Equals(0);
    }
    [TestCase]
    public async Task Test_SteamManager_JoinAndLeaveLobby()
    {
        int successfullyCreatedLobbyCount = 0;
        int lobbyGameCreatedCount = 0;
        int playerJoinLobbyCount = 0;
        int playerLeftLobbyCount = 0;

        _steamManager.OnLobbySuccessfullyCreated += (l) => successfullyCreatedLobbyCount++;
        _steamManager.OnLobbyGameCreated += (l) => lobbyGameCreatedCount++;
        _steamManager.OnPlayerJoinLobby += (f) => playerJoinLobbyCount++;
        _steamManager.OnPlayerLeftLobby += (f) => playerLeftLobbyCount++;

        await _steamManager.CreateLobby();

        AssertThat(_steamManager.IsHost).IsTrue();

        Lobby lobby = (Lobby)typeof(SteamManager).GetField("_hostedLobby", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(_steamManager)!;
        AssertThat(lobby.Owner.Id == _steamManager.PlayerSteamID).IsTrue();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(0);
        AssertThat(playerLeftLobbyCount).Equals(0);

        SteamManager joiningSteamManager = new SteamManager();

        await lobby.Join();

        AssertThat(joiningSteamManager.IsHost).IsFalse();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(1);
        AssertThat(playerLeftLobbyCount).Equals(0);

        lobby.Leave();

        AssertThat(_steamManager.IsHost).IsTrue();
        AssertThat(joiningSteamManager.IsHost).IsFalse();
        AssertThat(successfullyCreatedLobbyCount).Equals(1);
        AssertThat(lobbyGameCreatedCount).Equals(0);
        AssertThat(playerJoinLobbyCount).Equals(1);
        AssertThat(playerLeftLobbyCount).Equals(1);
    }
}
