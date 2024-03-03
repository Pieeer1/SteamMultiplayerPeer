using Godot;
using GodotVoipNet;

public partial class Player : Node3D
{
    private VoiceInstance _voiceInstance = null!;
    public override void _EnterTree()
    {
        SetMultiplayerAuthority((int)long.Parse(Name));
    }

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority()) { return; }
        _voiceInstance = GetNode<VoiceInstance>("VoiceInstance");
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) { return; }

        if (Input.IsActionPressed("push_to_talk"))
        {
            _voiceInstance.IsRecording = true;
        }
        else
        {
            _voiceInstance.IsRecording = false;
        }
    }
}