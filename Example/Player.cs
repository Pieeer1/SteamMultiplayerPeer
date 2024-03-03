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
        _voiceInstance = GetNode<VoiceInstance>("VoiceInstance");
        _voiceInstance.IsRecording = true;
        
    }
}
