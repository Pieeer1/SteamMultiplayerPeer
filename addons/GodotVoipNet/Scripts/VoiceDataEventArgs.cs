using Godot;
using Godot.Collections;
using System;

namespace GodotVoipNet;
public class VoiceDataEventArgs : EventArgs
{
    public Array<Vector2> Data { get; }
    public int Id { get; }

    public VoiceDataEventArgs(Array<Vector2> data, int id)
    {
        Data = data;
        Id = id;
    }
}
