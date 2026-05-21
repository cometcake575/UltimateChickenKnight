using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class CursorInfoPacket : IPacketData
{
    public string ObjectID;
    public float Rotation;
    public ushort Id;
    public (float, float, float) Pos;
    public (float, float, float) Scale;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(ObjectID);
        packet.Write(Rotation);
        packet.Write(Id);
        packet.Write(Pos.Item1);
        packet.Write(Pos.Item2);
        packet.Write(Pos.Item3);
        packet.Write(Scale.Item1);
        packet.Write(Scale.Item2);
        packet.Write(Scale.Item3);
    }

    public void ReadData(IPacket packet)
    {
        ObjectID = packet.ReadString();
        Rotation = packet.ReadFloat();
        Id = packet.ReadUShort();
        Pos = (packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
        Scale = (packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
    }

    public bool IsReliable => false;
    public bool DropReliableDataIfNewerExists => true;
}