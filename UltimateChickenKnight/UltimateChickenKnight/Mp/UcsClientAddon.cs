using System;
using System.Collections;
using System.IO;
using System.Linq;
using Architect.Editor;
using Architect.Placements;
using Architect.Sharer.States;
using Architect.Utils;
using Newtonsoft.Json;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Api.Command.Client;
using UltimateChickenKnight.Mp.Data;
using UnityEngine;

namespace UltimateChickenKnight.Mp;

public class UcsClientAddon : ClientAddon
{
    public new static IClientApi ClientApi;
    private static IClientAddonNetworkSender<PacketId> _networkSender;
    
    public override void Initialize(IClientApi clientApi)
    {
        ClientApi = clientApi;
        _networkSender = clientApi.NetClient.GetNetworkSender<PacketId>(this);
        
        clientApi.CommandManager.RegisterCommand(new DumpCommand());
        
        var netReceiver = clientApi.NetClient.GetNetworkReceiver<PacketId>(this,
            id => id.GetData());
        
        netReceiver.RegisterPacketHandler<TogglePlayingPacket>(PacketId.StartStop, HandlePlayToggle);
        netReceiver.RegisterPacketHandler<CursorInfoPacket>(PacketId.CursorInfo, data => 
            UltimateChickenKnight.Instance.StartCoroutine(UcsEditorUI.UpdateForeignCursor(data)));
        netReceiver.RegisterPacketHandler<ToEditorPacket>(PacketId.ToEditor, HandleModeToggle);
        netReceiver.RegisterPacketHandler<PickObjectPacket>(PacketId.DidSelect, HandlePickObject);
        netReceiver.RegisterPacketHandler<PlacePacket>(PacketId.DidPlace, HandlePlace);
        netReceiver.RegisterPacketHandler<TeleportPacket>(PacketId.Teleport, HandleTeleport);
        
        netReceiver.RegisterPacketHandler<GameInfoPacket>(PacketId.GameInfo, packet =>
        {
            if (packet.Complete)
            {
                UltimateChickenKnight.Instance.Settings.DeathType =
                    (UltimateChickenKnight.DeathType)packet.DeathType;
            }
            else
            {
                packet.DeathType = (int)UltimateChickenKnight.Instance.Settings.DeathType;
                packet.Complete = true;
                _networkSender.SendSingleData(PacketId.GameInfo, packet);
            }
        });

        clientApi.ClientManager.DisconnectEvent += () =>
        {
            UcsEditorUI.HideForeignCursors();
            UcsEditorUI.HideInfo();
            UcsEditorUI.HideSelect();
            UcsEditManager.IsPlaying = false;
            UcsEditManager.RoundOver = false;
            UcsEditManager.IsEditing = false;
            UcsEditManager.OpenEditor = false;
        };
    }

    public static void Send(int index)
    {
        _networkSender.SendSingleData(PacketId.DidSelect, new PickObjectPacket { Index = index });
    }

    public static void SendCursorInfo(string objectId, float rot, Vector3 pos, Vector3 scale)
    {
        _networkSender.SendSingleData(PacketId.CursorInfo, new CursorInfoPacket
        {
            ObjectID = objectId,
            Rotation = rot,
            Pos = (pos.x, pos.y, pos.z),
            Scale = (scale.x, scale.y, scale.z)
        });
    }

    private static void HandlePlayToggle(TogglePlayingPacket data)
    {
        UcsEditManager.CurrentEdits = new LevelData([], [], [], []);
        if (data.Playing)
        {
            UcsEditorUI.HideForeignCursors();
            EditManager.IsEditing = false;
            UcsEditManager.RoundOver = false;
            UcsEditManager.IsEditing = true;
            UcsEditManager.IsPlaying = true;
            UcsEditManager.OpenEditor = true;
        }
        else
        {
            UcsEditorUI.HideForeignCursors();
            UcsEditManager.IsPlaying = false;
            UcsEditManager.RoundOver = false;
            UcsEditManager.IsEditing = false;
            UcsEditManager.OpenEditor = false;
        }

        UcsEditorUI.Seed = data.Seed;
        UcsEditManager.GoToScene(data.Scene);
    }

    private static void HandleModeToggle(ToEditorPacket data)
    {
        UltimateChickenKnight.Instance.StartCoroutine(DoModeToggle(data));
    }
    
    private static IEnumerator DoModeToggle(ToEditorPacket data)
    {
        yield return new WaitForSeconds(2);
        UcsEditorUI.ShowInfo(data.UserInfo);
        yield return new WaitForSeconds(6);
        UcsEditorUI.HideInfo();
        yield return new WaitForSeconds(1);
        
        UcsEditorUI.Seed = data.Seed;
        UcsEditManager.IsEditing = true;
        UcsEditManager.OpenEditor = true;
        
        UcsEditManager.GoToScene(GameManager.instance.sceneName);
    }

    private static void HandlePickObject(PickObjectPacket data)
    {
        UcsEditorUI.Select(data.Index);
        if (data.FinishSelect)
        {
            UcsEditorUI.FinishSelect();
        }
    }
    
    private static void HandleTeleport(TeleportPacket data)
    {
        UcsEditManager.GoToScene(data.Scene);
    }

    private static void HandlePlace(PlacePacket data)
    {
        if (data.IsErase)
        {
            var placement = UcsEditManager.CurrentEdits.Placements
                .FirstOrDefault(o => o.ID == data.Data);
            if (placement != null)
            {
                placement.Destroy();
                UcsEditManager.CurrentEdits.Placements.Remove(placement);
            }
        }
        else
        {
            var placement = JsonConvert.DeserializeObject<ObjectPlacement>(data.Data, Converters.All);
            placement.PlaceGhost();
            UcsEditManager.CurrentEdits.Placements.Add(placement);
        }

        UcsEditorUI.HideForeignCursor(data.Id);
        if (data.FinishPlace) UltimateChickenKnight.Instance.StartCoroutine(StopEditing());
    }

    private static IEnumerator StopEditing()
    {
        yield return new WaitForSeconds(1);
        UcsEditManager.IsEditing = false;
        UcsEditManager.GoToScene(GameManager.instance.sceneName);
    }

    public static void DoPlace(ObjectPlacement placement)
    {
        _networkSender.SendSingleData(PacketId.DidPlace, new PlacePacket
        {
            Data = JsonConvert.SerializeObject(placement, Converters.All)
        });
    }
    
    public static void DoErase(ObjectPlacement placement)
    {
        _networkSender.SendSingleData(PacketId.DidPlace, new PlacePacket
        {
            IsErase = true,
            Data = placement.ID
        });
    }

    public static void SendRoundInfo(bool won)
    {
        _networkSender.SendSingleData(PacketId.RoundInfo, new RoundInfoPacket
        {
            Won = won,
            UserPfp = Account.CurrentUserInfo?.PfpUrl ?? string.Empty
        });
    }

    private static int _playerIndex;

    public static void MoveSpectate(int amount)
    {
        _playerIndex += amount;
        var high = ClientApi.ClientManager.Players.Count(
            p => p.IsInLocalScene && 
                 p.PlayerContainer && 
                 p.PlayerContainer.transform.GetPositionY() < 999999) - 1;
        if (_playerIndex < 0) _playerIndex = high;
        if (_playerIndex > high) _playerIndex = 0;
    }

    public static Vector3 GetSpectatePos()
    {
        do
        {
            var i = 0;
            foreach (var player in ClientApi.ClientManager.Players)
            {
                if (player.IsInLocalScene && 
                    player.PlayerContainer && 
                    player.PlayerContainer.transform.GetPositionY() < 999999)
                {
                    if (i >= _playerIndex) return player.PlayerContainer.transform.position;
                    i++;
                }
            }
            
            _playerIndex--;
        } while (_playerIndex > 0);

        return UcsObjects.StartPos;
    }

    public class DumpCommand : IClientCommand
    {
        public string Trigger => "/ucsdump";
        
        public string[] Aliases => [];
        
        public void Execute(string[] arguments)
        {
            var dataPath = Path.GetFullPath(Application.persistentDataPath + "/UCS/");
            Directory.CreateDirectory(dataPath);

            var subFolder = dataPath + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss") + "/";
            Directory.CreateDirectory(subFolder);
            
            File.WriteAllText(subFolder + GameManager.instance.sceneName + ".architect.json", 
                JsonConvert.SerializeObject(UcsEditManager.CurrentEdits, Converters.All));
        }
    }

    protected override string Name => "UltimateChickenKnight";
    protected override string Version => UltimateChickenKnight.Version;
    public override bool NeedsNetwork => true;
}
