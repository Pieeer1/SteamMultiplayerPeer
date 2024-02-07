using Godot;
using System;

public partial class Player : Node3D
{

    public override void _EnterTree()
    {
        SetMultiplayerAuthority((int)long.Parse(Name));
    }
}
