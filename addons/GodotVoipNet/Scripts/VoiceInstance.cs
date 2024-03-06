using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotVoipNet;
public partial class VoiceInstance : Node
{
    private VoiceMic? _voiceMic;
    private AudioEffectCapture _audioEffectCapture = null!;
    private AudioStreamGeneratorPlayback? _playback;
    private Vector2[] _receiveBuffer = [];
    private readonly Queue<(Vector2[] buffer, long ms)> _delayedReceiveBuffer = new Queue<(Vector2[] buffer, long ms)>();
    private bool _previousFrameIsRecording = false;

    private int _unixMsDelay = 100; // one tenth hudnsecond for now

    private AudioStreamPlayer3D? _audioStreamPlayer3D;

    private bool _isPositional = false;
    [Export]
    public bool IsPositional
    {
        get => _isPositional; set
        {
            _isPositional = value;
            CreateVoice(AudioServer.GetMixRate(), value);
        }
    }
    [Export]
    public bool IsStereo { get; set; } = false;
    [Export]
    public bool IsRecording { get; set; } = false;
    [Export]
    public bool ShouldListen { get; set; } = false;
    [Export]
    public double InputThreshold { get; set; } = 0.005f;

    public event EventHandler<VoiceDataEventArgs>? ReceivedVoiceData;
    public event EventHandler<VoiceDataEventArgs>? SentVoiceData;

    public override void _Ready()
    {
        CreateVoice(AudioServer.GetMixRate(), IsPositional);
    }

    public override void _Process(double delta)
    {
        if (_playback is not null)
        {
            ProcessVoice();
        }
        ProcessMic();
        if (_voiceMic is not null)
        {
            _voiceMic.GlobalPosition = GetParent<Node3D>().GlobalPosition;
        }
    }

    private void CreateMic()
    {
        _voiceMic = new VoiceMic();
        AddChild(_voiceMic);
        int recordBusIdx = AudioServer.GetBusIndex(_voiceMic.Bus);
        _audioEffectCapture = (AudioEffectCapture)AudioServer.GetBusEffect(recordBusIdx, 0);
    }

    private void CreateVoice(float mixRate, bool isPositional)
    {
        var generator = new AudioStreamGenerator();
        generator.BufferLength = 0.1f;
        generator.MixRate = mixRate;

        if (isPositional)
        {
            SwitchToPositional(generator);
        }
        else
        {
            SwitchToNonPositional(generator);
        }
    }

    private void SwitchToNonPositional(AudioStreamGenerator audioStreamGenerator)
    {
        _audioStreamPlayer3D = null;
        GetNodeOrNull("AudioStreamPlayer")?.Free();

        AudioStreamPlayer audioStreamPlayer = new AudioStreamPlayer();

        audioStreamPlayer.Name = "AudioStreamPlayer";

        AddChild(audioStreamPlayer);

        audioStreamPlayer.Stream = audioStreamGenerator;
        audioStreamPlayer.Play();
        _playback = audioStreamPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }
    private void SwitchToPositional(AudioStreamGenerator audioStreamGenerator)
    {
        GetNodeOrNull("AudioStreamPlayer")?.Free();

        _audioStreamPlayer3D = new AudioStreamPlayer3D();
        _audioStreamPlayer3D.Name = "AudioStreamPlayer";

        AddChild(_audioStreamPlayer3D);

        _audioStreamPlayer3D.Stream = audioStreamGenerator;
        _audioStreamPlayer3D.Play();
        _playback = _audioStreamPlayer3D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void Speak(Vector2[] data, int id, Vector3 position, long unixQueuedTime)
    {
        if (_audioStreamPlayer3D is not null)
        {
            _audioStreamPlayer3D.GlobalPosition = position;
        }
        ReceivedVoiceData?.Invoke(this, new VoiceDataEventArgs(data, id));

        long difference = (long)MathF.Abs(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - unixQueuedTime);

        _delayedReceiveBuffer.Enqueue((data, unixQueuedTime-difference+1000));

        GD.Print($"{_receiveBuffer.FirstOrDefault().X} {_receiveBuffer.FirstOrDefault().Y} {DateTime.UtcNow.Ticks}");
    }

    private void ProcessVoice()
    {
        int framesAvailable = _playback?.GetFramesAvailable() ?? 0;
        if (framesAvailable < 1) { return; }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        while (_delayedReceiveBuffer.Any() && _delayedReceiveBuffer.Peek().ms < now)
        {
            _receiveBuffer = [.._receiveBuffer, .. _delayedReceiveBuffer.Dequeue().buffer];
        }

        _playback?.PushBuffer(_receiveBuffer);
        _receiveBuffer = [];
    }

    private void ProcessMic()
    {
        if (IsRecording)
        {
            if (_audioEffectCapture is null)
            {
                CreateMic();
            }
            if (!_previousFrameIsRecording)
            {
                _audioEffectCapture?.ClearBuffer();
            }
            Vector2[] stereoData = _audioEffectCapture?.GetBuffer(_audioEffectCapture.GetFramesAvailable()) ?? [];
            if (stereoData.Any())
            {
                var data = new Vector2[stereoData.Length];

                float maxValue = 0.0f;

                for (int i = 0; i < stereoData.Length; i++)
                {
                    float value = (stereoData[i].X + stereoData[i].Y) / 2.0f;
                    maxValue = Math.Max(value, maxValue);
                    data[i] = IsStereo ? stereoData[i] : new Vector2(value, value);
                }
                if (maxValue < InputThreshold)
                {
                    return;
                }
                if (ShouldListen)
                {
                    Speak(data, Multiplayer.GetUniqueId(), GetParent<Node3D>().GlobalPosition, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _unixMsDelay);
                }
                Rpc(nameof(Speak), [data, Multiplayer.GetUniqueId(), GetParent<Node3D>().GlobalPosition, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _unixMsDelay]);
                SentVoiceData?.Invoke(this, new VoiceDataEventArgs(data, Multiplayer.GetUniqueId()));
            }
        }
        _previousFrameIsRecording = IsRecording;
    }
}
