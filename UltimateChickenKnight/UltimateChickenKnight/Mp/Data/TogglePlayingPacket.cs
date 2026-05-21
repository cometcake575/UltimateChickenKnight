using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class TogglePlayingPacket : IPacketData
{
    public bool Playing;
    public string Scene;
    public int Seed;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(Playing);
        packet.Write(Scene);
        packet.Write(Seed);
    }

    public void ReadData(IPacket packet)
    {
        Playing = packet.ReadBool();
        Scene = packet.ReadString();
        Seed = packet.ReadInt();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}