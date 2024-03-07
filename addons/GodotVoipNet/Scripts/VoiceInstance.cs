using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GodotVoipNet;
public partial class VoiceInstance : Node
{
    private VoiceMic? _voiceMic;
    private AudioEffectCapture _audioEffectCapture = null!;
    private AudioStreamGeneratorPlayback? _playback;
    private Vector2[] _receiveBuffer = [];
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

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Speak(Vector2[] data, int id, Vector3 position)
    {
        if (_audioStreamPlayer3D is not null)
        {
            _audioStreamPlayer3D.GlobalPosition = position;
        }
        ReceivedVoiceData?.Invoke(this, new VoiceDataEventArgs(data, id));

        Task.Delay(new TimeSpan(0, 0, 0, 0, 10)).ContinueWith(o =>
        {
            _receiveBuffer = [.. data];
        });
    }

    private void ProcessVoice()
    {
        int framesAvailable = _playback?.GetFramesAvailable() ?? 0;
        if (framesAvailable < 1) { return; }

        _playback?.PushBuffer(_receiveBuffer);
        _receiveBuffer = [];
    }
    private bool _isAlreadyListening = false;
    private Vector2[] _sendingBuffer = [];
    private void ProcessMic()
    {
        if (IsRecording)
        {
            int id = Multiplayer.GetUniqueId();
            Vector3 position = GetParent<Node3D>().GlobalPosition;

            if (_audioEffectCapture is null)
            {
                CreateMic();
            }
            if (!_isAlreadyListening)
            {
                _isAlreadyListening = true;

                Task.Delay(TimeSpan.FromSeconds(0.1)).ContinueWith(o =>
                {
                    _sendingBuffer = _audioEffectCapture?.GetBuffer(_audioEffectCapture.GetFramesAvailable()) ?? [];
                    _isAlreadyListening = false;
                    _audioEffectCapture?.ClearBuffer();

                }); 
            }
            if (_sendingBuffer.Any())
            {
                float maxValue = 0.0f;

                for (int i = 0; i < _sendingBuffer.Length; i++)
                {
                    float value = (_sendingBuffer[i].X + _sendingBuffer[i].Y) / 2.0f;
                    maxValue = Math.Max(value, maxValue);
                    if (IsStereo)
                    {
                        _sendingBuffer[i] = new Vector2(value, value);
                    }
                }
                if (maxValue < InputThreshold)
                {
                    return;
                }
                if (ShouldListen)
                {
                    Speak(_sendingBuffer, id, position);
                }

                Rpc(nameof(Speak), [_sendingBuffer, id, position]);


                SentVoiceData?.Invoke(this, new VoiceDataEventArgs(_sendingBuffer, Multiplayer.GetUniqueId()));
                _sendingBuffer = [];
            }
        }
        else 
        {
            _audioEffectCapture?.ClearBuffer();
        }
        _previousFrameIsRecording = IsRecording;
    }
}
