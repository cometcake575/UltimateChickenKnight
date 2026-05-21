using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class PlacePacket : IPacketData
{
    public bool IsErase;
    public string Data;
    public bool FinishPlace;
    public ushort Id;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(IsErase);
        packet.Write(Data);
        packet.Write(FinishPlace);
        packet.Write(Id);
    }

    public void ReadData(IPacket packet)
    {
        IsErase = packet.ReadBool();
        Data = packet.ReadString();
        FinishPlace = packet.ReadBool();
        Id = packet.ReadUShort();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}