using Steam;
using Steamworks;
using System;

namespace SteamMultiplayerPeer.Tests;
[TestSuite]
public class SteamManagerTests
{
    [TestCase]
    public void Test_SteamManager_Initialize()
    {
        SteamManager steamManager = new SteamManager();
        steamManager._Ready();

        if (!SteamClient.IsValid)
        {
            throw new Exception("Error: Steam Connection Failed: Make sure Steam is running for these tests to run. Unfortunately this is a limitation");
        }
        AssertThat(steamManager.PlayerName).IsNotNull();
        AssertThat(steamManager.PlayerSteamID != 0).IsTrue();
    }

    [TestCase]
    public void Test_SteamManager_InitializeLobby()
    { 
        
    }
}
