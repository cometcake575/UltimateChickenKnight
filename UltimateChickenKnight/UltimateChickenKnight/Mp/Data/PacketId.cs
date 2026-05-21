using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public enum PacketId
{
    GameInfo,
    StartStop,
    ToEditor,
    DidSelect,
    DidPlace,
    RoundInfo,
    CursorInfo,
    Teleport
}

public static class PacketIdExtension
{
    public static IPacketData GetData(this PacketId packetId)
    {
        return packetId switch
        {
            PacketId.GameInfo => new GameInfoPacket(),
            PacketId.StartStop => new TogglePlayingPacket(),
            PacketId.ToEditor => new ToEditorPacket(),
            PacketId.DidSelect => new PickObjectPacket(),
            PacketId.DidPlace => new PlacePacket(),
            PacketId.RoundInfo => new RoundInfoPacket(),
            PacketId.CursorInfo => new CursorInfoPacket(),
            PacketId.Teleport => new TeleportPacket(),
            _ => null
        };
    }
}