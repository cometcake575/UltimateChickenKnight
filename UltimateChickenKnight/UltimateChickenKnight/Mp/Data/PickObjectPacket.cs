using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class PickObjectPacket : IPacketData
{
    public int Index;
    public bool FinishSelect;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(Index);
        packet.Write(FinishSelect);
    }

    public void ReadData(IPacket packet)
    {
        Index = packet.ReadInt();
        FinishSelect = packet.ReadBool();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}