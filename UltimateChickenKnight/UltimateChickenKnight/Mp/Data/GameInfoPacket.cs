using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class GameInfoPacket : IPacketData
{
    public bool Complete;
    public int DeathType;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(DeathType);
        packet.Write(Complete);
    }

    public void ReadData(IPacket packet)
    {
        DeathType = packet.ReadInt();
        Complete = packet.ReadBool();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}