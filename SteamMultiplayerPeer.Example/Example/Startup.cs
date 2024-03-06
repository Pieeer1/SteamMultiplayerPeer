using Godot;
using System;
using Steam;
public partial class Startup : Node3D
{
    public SteamManager SteamManager { get; } = new SteamManager();

    public override void _Ready()
    {
        AddChild(SteamManager);    
    }
}
