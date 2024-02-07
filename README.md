# Steam Multiplayer Peer

## Description

The goal of this project is to greatly simplify the way that Steam Multiplayer is implemented in Godot. This project is a Godot Package that provides a simple API for creating and joining lobbies, and sending and receiving messages between players.

## Installation

The example project shown above should give a good example on how to start up. A couple key things to note

1. You need to have the Steam SDK installed and the Steam API running in your project. This is not included in the package, but you can find the SDK [here](https://partner.steamgames.com/doc/sdk). You will need to have a Steamworks account to access this.
1. I highly recommend getting your own steam app ID and setting up your own app in the Steamworks dashboard. This will allow you to test your game without interfering with other people's games. You can find the Steamworks dashboard [here](https://partner.steamgames.com/home).
1. You will need to have the Steam client running on your computer to test the multiplayer. This is because the Steam API is used to connect to the Steam servers and create lobbies. You can download the Steam client [here](https://store.steampowered.com/about/).
1. The Steam Multiplayer Peer package is not yet available on the Godot Asset Library. You will need to download the source code and add it to your project manually. You can find the source code above. I highly recommend paying special attention to the DLL's and the Facepunch Steamworks library. You will need to have the correct DLL's for your platform and the correct version of the Facepunch Steamworks library included with this package. There is no garuntee that another version of either will function with this project
1. Check the csproj for more specifics on connecting above. 
 

## License

This Project is Licensed under MIT License. See the [LICENSE](LICENSE.txt) file for more information.