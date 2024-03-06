using static Godot.MultiplayerPeer;
using Steamworks;

namespace Steam;
public class SteamNetworkingMessage
{
    public SteamNetworkingMessage(byte[] data, SteamId sender, TransferModeEnum transferMode, long receiveTime)
    {
        Data = data;
        Sender = sender;
        TransferMode = transferMode;
        ReceiveTime = receiveTime;

    }

    public byte[] Data { get; private set; }
    public SteamId Sender { get; private set; }

    public TransferModeEnum TransferMode { get; private set; }

    public int Size => Data.Length;

    public long ReceiveTime { get; private set; }

}