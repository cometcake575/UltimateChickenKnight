using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class RoundInfoPacket : IPacketData
{
    public bool Won;
    public string UserPfp;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(Won);
        packet.Write(UserPfp);
    }

    public void ReadData(IPacket packet)
    {
        Won = packet.ReadBool();
        UserPfp = packet.ReadString();
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}