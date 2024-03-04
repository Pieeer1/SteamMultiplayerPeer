using Godot;
using System;

namespace GodotVoipNet;
public class VoiceDataEventArgs : EventArgs
{
    public Vector2[] Data { get; }
    public int Id { get; }

    public VoiceDataEventArgs(Vector2[] data, int id)
    {
        Data = data;
        Id = id;
    }
}
