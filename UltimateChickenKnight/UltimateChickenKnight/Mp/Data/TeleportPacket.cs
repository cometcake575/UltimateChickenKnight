using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class TeleportPacket : IPacketData
{
    public string Scene;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(Scene);
    }

    public void ReadData(IPacket packet)
    {
        Scene = packet.ReadString();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}