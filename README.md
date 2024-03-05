# Steam Multiplayer Peer

## Description

The goal of this project is to greatly simplify the way that Steam Multiplayer is implemented in Godot. This project is a Godot Package that provides a simple API for creating and joining lobbies, and sending and receiving messages between players.

## Installation

The example project shown above should give a good example on how to start up. A couple key things to note

dotnet
`dotnet add package SteamMultiplayerPeer`

package manager
`Install-Package SteamMultiplayerPeer`

Once the package is installed you will have to add the following to the csproj:
```xml
  <ItemGroup>
    <Reference Include="Facepunch.Steamworks.Win64" Condition="'$(Configuration)' == 'Debug' and $([MSBuild]::IsOSPlatform('Windows'))">
      <HintPath>.godot\mono\temp\bin\Debug\Facepunch.Steamworks.Win64.dll</HintPath>
    </Reference>
    <Reference Include="Facepunch.Steamworks.Win64" Condition="'$(Configuration)' == 'Release' and $([MSBuild]::IsOSPlatform('Windows'))">
        <HintPath>.godot\mono\temp\bin\Release\Facepunch.Steamworks.Win64.dll</HintPath>
    </Reference>
    <Reference Include="Facepunch.Steamworks.Win64" Condition="'$(Configuration)' == 'Debug' and ($([MSBuild]::IsOSPlatform('Linux')) or $([MSBuild]::IsOSPlatform('OSX')))">
        <HintPath>.godot\mono\temp\bin\Debug\Facepunch.Steamworks.Win64.dll</HintPath>
    </Reference>
    <Reference Include="Facepunch.Steamworks.Win64" Condition="'$(Configuration)' == 'Release' and ($([MSBuild]::IsOSPlatform('Linux')) or $([MSBuild]::IsOSPlatform('OSX')))">
        <HintPath>.godot\mono\temp\bin\Release\Facepunch.Steamworks.Win64.dll</HintPath>
    </Reference>
  </ItemGroup>
```

The above snippet will add the Facepunch.Steamworks.Win64.dll to the project. This is the library that is used to connect to the Steam API.

1. You will need to have a Steam account to use the Steamworks API. You can create a Steam account [here](https://store.steampowered.com/join/)
1. I highly recommend getting your own steam app ID and setting up your own app in the Steamworks dashboard. This will allow you to test your game without interfering with other people's games. You can find the Steamworks dashboard [here](https://partner.steamgames.com/home).
1. You will need to have the Steam client running on your computer to test the multiplayer. This is because the Steam API is used to connect to the Steam servers and create lobbies. You can download the Steam client [here](https://store.steampowered.com/about/).


## Usage

1. Create a SteamManager Reference at the root of your tree. Call Ready:

```csharp
public SteamManager SteamManager { get; set; } = new SteamManager();

public override void _Ready()
{
	SteamManager._Ready();
}

```
1. Set the SteamAppId to your AppId from the Steamworks dashboard.
1. This will start steam, and allow you to use the steam API (shift tab)


### Multiplayer

1. Create a SteamMultiplayerPeer instance
1. Either create or host a lobby to run the game. 
 
## FAQ

"It is not working locally"
- Make sure that you have the Steam client running on your computer. This is required to connect to the Steam servers and create lobbies.
- You will NEED TO HAVE A SECOND STEAM ACCOUNT RUNNING ON EITHER A SEPARATE COMPUTER OR A SEPARATE INSTANCE OF THE STEAM CLIENT WITH A SEPARATE INSTANCE. This is because the Steam API does not allow you to connect to the same lobby with the same account. This is a limitation of the Steam API, and there is no way around it.

## License

This Project is Licensed under MIT License. See the [LICENSE](LICENSE.txt) file for more information.