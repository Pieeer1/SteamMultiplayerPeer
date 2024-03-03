using Godot;

namespace GodotVoipNet;
public partial class VoiceMic : AudioStreamPlayer3D
{
    public override void _Ready()
    {
        int currentNumber = 0;
        while (AudioServer.GetBusIndex($"VoiceMicRecorder_{currentNumber}") != -1)
        {
            currentNumber++;
        }
        string busName = $"VoiceMicRecorder_{currentNumber}";
        int idx = AudioServer.BusCount;

        AudioServer.AddBus(idx);
        AudioServer.SetBusName(idx, busName);

        AudioServer.AddBusEffect(idx, new AudioEffectCapture());

        AudioServer.SetBusMute(idx, true);

        Bus = busName;
        Stream = new AudioStreamMicrophone();

        Play();
    }

}
