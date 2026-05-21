using System.Collections.Generic;
using Newtonsoft.Json;
using Hkmp.Networking.Packet;

namespace UltimateChickenKnight.Mp.Data;

public class ToEditorPacket : IPacketData
{
    public List<(string, string, int)> UserInfo;
    public int Seed;
    
    public void WriteData(IPacket packet)
    {
        packet.Write(Seed);
        packet.Write(JsonConvert.SerializeObject(UserInfo));
    }

    public void ReadData(IPacket packet)
    {
        Seed = packet.ReadInt();
        UserInfo = JsonConvert.DeserializeObject<List<(string, string, int)>>(packet.ReadString());
    }

    public bool IsReliable => true;
    public bool DropReliableDataIfNewerExists => false;
}