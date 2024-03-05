using Godot;
using System;

internal partial class Player : Node3D
{

    public override void _EnterTree()
    {
        SetMultiplayerAuthority((int)long.Parse(Name));
    }
}
