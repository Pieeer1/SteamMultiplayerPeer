using Godot;
using static Godot.MultiplayerPeer;

namespace Steam;
public partial class SteamPacketPeer : RefCounted
{
    public SteamPacketPeer(byte[] data, TransferModeEnum transferMode = TransferModeEnum.Reliable)
    {
        Data = data;
        TransferMode = transferMode;
    }

    public byte[] Data { get; private set; }
    public ulong SenderSteamId { get; set; }
    public TransferModeEnum TransferMode { get; private set; }
}
