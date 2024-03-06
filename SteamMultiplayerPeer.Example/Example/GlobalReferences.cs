using Godot;
using Steam;

namespace SteamMultiplayerPeer.Example;
internal static class GlobalReferences // is this bad practice? idk its an example project
{
    public static SteamManager SteamManager(this Node node) => node.GetTree().Root.GetNode<SteamManager>("Startup/SteamManager");

}
