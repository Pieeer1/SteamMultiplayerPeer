using Godot;
using Godot.Collections;
using System;
using System.Linq;


namespace GodotVoipNet;
public partial class VoiceOrchestrator : Node
{
    private int? _id = null;
    private Dictionary<int, VoiceInstance> _instances = new Dictionary<int, VoiceInstance>();
    [Export]
    public bool IsPositional { get; set; } = false;
    [Export]
    public bool IsStereo { get; set; } = false;
    [Export]
    public bool IsRecording { get; set; } = false;
    [Export]
    public bool ShouldListen { get; set; } = false;
    [Export]
    public double InputThreshold { get; set; } = 0.005f;

    public event EventHandler<int>? CreatedInstance;
    public event EventHandler<int>? RemovedInstance;
    public event EventHandler<VoiceDataEventArgs>? ReceivedVoiceData;
    public event EventHandler<VoiceDataEventArgs>? SentVoiceData;
    public override void _Ready()
    {
        Multiplayer.ConnectedToServer += ConnectedOk;
        Multiplayer.ServerDisconnected += Reset;
        Multiplayer.ConnectionFailed += Reset;

        Multiplayer.PeerConnected += (s) => PlayerConnected((int)s);
        Multiplayer.PeerConnected += (s) => PlayerDisconnected((int)s);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer() && _id is null)
        {
            CreateInstance(Multiplayer.GetUniqueId());
        }

        if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer() && _id == 1)
        { 
            Reset();
        }
    }
    private void ConnectedOk()
    {
        if (!Multiplayer.HasMultiplayerPeer() || (!Multiplayer.IsServer() && _id == 1))
        {
            Reset();
        }
        CreateInstance(Multiplayer.GetUniqueId());
    }
    private void CreateInstance(int id)
    {
        VoiceInstance instance = new VoiceInstance();

        if (id == Multiplayer.GetUniqueId())
        {
            instance.IsStereo = IsStereo;
            instance.IsRecording = IsRecording;
            instance.ShouldListen = ShouldListen;
            instance.InputThreshold = InputThreshold;
            instance.IsPositional = IsPositional;

            instance.SentVoiceData += (o, e) => SentVoiceData?.Invoke(this, e);

            _id = id;
        }

        instance.ReceivedVoiceData += (o, e) => ReceivedVoiceData?.Invoke(this, e);

        instance.Name = $"{id}";

        _instances[id] = instance;

        AddChild(instance);

        CreatedInstance?.Invoke(this, id);
    }

    private void Reset()
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            RemoveInstance(_instances.Keys.ElementAt(i));
        }
    }
    private void RemoveInstance(int id)
    { 
        var instance = _instances[id];
        if(id == _id) { _id = null; }

        instance.QueueFree();

        _instances.Remove(id);

        RemovedInstance?.Invoke(this, id);
    }
    private void PlayerConnected(int id)
    {
        CreateInstance(id);
    }
    private void PlayerDisconnected(int id)
    {
        RemoveInstance(id);
    }

}
