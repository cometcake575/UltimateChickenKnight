using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Api.Command;
using Hkmp.Api.Command.Server;
using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Modding;
using UltimateChickenKnight.Mp.Data;
using Architect.Utils;

namespace UltimateChickenKnight.Mp;

public class UcsServerAddon : ServerAddon
{
    private static IServerApi _serverApi;
    private static IServerAddonNetworkSender<PacketId> _networkSender;

    private static readonly Dictionary<string, UcsServerScene> Scenes = [];

    private static readonly Dictionary<ushort, string> Pfps = [];
    
    public override void Initialize(IServerApi serverApi)
    {
        _serverApi = serverApi;
        _networkSender = serverApi.NetServer.GetNetworkSender<PacketId>(this);
        
        _serverApi.CommandManager.RegisterCommand(new TogglePlayingCommand());

        var netReceiver = serverApi.NetServer.GetNetworkReceiver<PacketId>(this,
            id => id.GetData());
        
        netReceiver.RegisterPacketHandler<PickObjectPacket>(PacketId.DidSelect, (id, packet) =>
        {
            if (!GetScene(id, out var scene)) return;
            scene.Selected++;
            if (scene.Selected >= GetPlayers(scene.SceneName).Length)
            {
                packet.FinishSelect = true;
                scene.Placed = 0;
                scene.Won.Clear();
            }
            _networkSender.SendSingleData(PacketId.DidSelect, packet, GetPlayers(scene.SceneName));
        });
        
        netReceiver.RegisterPacketHandler<PlacePacket>(PacketId.DidPlace, (id, packet) =>
        {
            if (!GetScene(id, out var scene)) return;
            scene.Placed++;
            packet.Id = id;
            if (scene.Placed >= GetPlayers(scene.SceneName).Length)
            {
                packet.FinishPlace = true;
                scene.Editing = false;
                scene.Selected = 0;
                scene.Won.Clear();
            }
            _networkSender.SendSingleData(PacketId.DidPlace, packet, GetPlayers(scene.SceneName));
        });
        
        netReceiver.RegisterPacketHandler<GameInfoPacket>(PacketId.GameInfo, (id, packet) =>
        {
            if (!GetScene(id, out var scene)) return;
            _networkSender.SendSingleData(PacketId.GameInfo, packet, GetPlayers(scene.SceneName));
        });
        
        netReceiver.RegisterPacketHandler<CursorInfoPacket>(PacketId.CursorInfo, (id, packet) =>
        {
            if (!GetScene(id, out var scene)) return;
            packet.Id = id;
            _networkSender.SendSingleData(PacketId.CursorInfo, packet, GetPlayers(scene.SceneName, id));
        });
        
        netReceiver.RegisterPacketHandler<RoundInfoPacket>(PacketId.RoundInfo, (id, packet) =>
        {
            if (!GetScene(id, out var scene)) return;
            scene.Won[id] = packet.Won;
            Pfps[id] = packet.UserPfp;
            if (scene.Won.Count >= GetPlayers(scene.SceneName).Length && !scene.Editing) ToEditor(scene);
        });
    }

    private static void ToEditor(UcsServerScene scene)
    {
        scene.Selected = 0;
        scene.Placed = 0;
        foreach (var (id, won) in scene.Won)
        {
            scene.Wins[id] = scene.Wins.GetValueOrDefault(id, 0) + (won ? 1 : 0);
        }
        scene.Won.Clear();
        scene.Editing = true;
        _networkSender.SendSingleData(PacketId.ToEditor, new ToEditorPacket
        {
            UserInfo = scene.Wins.Select(i => 
                (_serverApi.ServerManager.GetPlayer(i.Key)?.Username ?? "", 
                    Pfps.GetValueOrDefault(i.Key, ""),
                    i.Value))
                .ToList(),
            Seed = Random.Next()
        }, GetPlayers(scene.SceneName));
    }

    protected override string Name => "UltimateChickenKnight";
    protected override string Version => UltimateChickenKnight.Version;
    public override bool NeedsNetwork => true;

    private static readonly Random Random = new();

    public class TogglePlayingCommand : IServerCommand
    {
        public string Trigger => "/ucs";
        public string[] Aliases => [];
        
        public void Execute(ICommandSender commandSender, string[] arguments)
        {
            if (commandSender is not IPlayerCommandSender sender
                || !_serverApi.ServerManager.TryGetPlayer(sender.Id, out var player))
            {
                commandSender.SendMessage("Command can only be run by a player");
                return;
            }
            if (arguments.Length < 2)
            {
                commandSender.SendMessage("Invalid usage: </ucs> <start|stop>" +
                                          "\nOr: </ucs> <tp|tph> <player>");
                return;
            }
            switch (arguments[1])
            {
                case "start":
                    // Start
                    if (Scenes.ContainsKey(player.CurrentScene))
                    {
                        commandSender.SendMessage("Game has already started, run /ucs stop to stop");
                        return;
                    }

                    if (player.CurrentScene.IsNullOrWhiteSpace())
                    {
                        commandSender.SendMessage("Server is missing scene info, try moving a bit");
                        return;
                    }

                    Scenes.Add(player.CurrentScene, new UcsServerScene(player.CurrentScene));
                    
                    _networkSender.SendSingleData(PacketId.GameInfo, new GameInfoPacket
                    {
                        Complete = false
                    }, player.Id);
                    _networkSender.SendSingleData(PacketId.StartStop, new TogglePlayingPacket
                    {
                        Playing = true,
                        Scene = player.CurrentScene,
                        Seed = Random.Next()
                    }, GetPlayers(player.CurrentScene));
                    commandSender.SendMessage("Starting Ultimate Chicken Knight!");
                    break;
                case "stop":
                    // Stop
                    if (player.CurrentScene.IsNullOrWhiteSpace())
                    {
                        commandSender.SendMessage("Server is missing scene info, try moving a bit");
                        return;
                    }
                    
                    if (!Scenes.ContainsKey(player.CurrentScene))
                    {
                        commandSender.SendMessage("Game is not running, run /ucs start to start");
                        return;
                    }

                    StopGame(player.CurrentScene);
                    commandSender.SendMessage("Stopping Ultimate Chicken Knight!");
                    break;
                default:
                    if (arguments.Length != 3 || (arguments[1] != "tph" && arguments[1] != "tp"))
                    {
                        commandSender.SendMessage("Invalid usage: </ucs> <start|stop>" +
                                                  "\nOr: </ucs> <tp|tph> <player>");
                        return;
                    }

                    var tp = player;
                    var tpto = _serverApi.ServerManager.Players
                        .FirstOrDefault(p => p.Username == arguments[2]);
                    if (tpto == null) return;

                    if (arguments[1] == "tph")
                    {
                        (tp, tpto) = (tpto, tp);
                    }

                    if (tpto.CurrentScene.IsNullOrWhiteSpace())
                    {
                        commandSender.SendMessage("Server is missing scene info, try moving a bit");
                        return;
                    }
                    
                    _networkSender.SendSingleData(PacketId.Teleport, new TeleportPacket
                    {
                        Scene = tpto.CurrentScene
                    }, tp.Id);
                    
                    commandSender.SendMessage($"Teleporting {tp.Username} to {tpto.CurrentScene}");
                    break;
            }
        }
        
        public bool AuthorizedOnly => true;
    }

    private static void StopGame(string scene)
    {
        Scenes.Remove(scene);
        _networkSender.SendSingleData(PacketId.StartStop, new TogglePlayingPacket
        {
            Playing = false,
            Scene = scene,
            Seed = Random.Next()
        }, GetPlayers(scene));
    }

    private static bool GetScene(ushort id, out UcsServerScene scene)
    {
        return Scenes.TryGetValue(_serverApi.ServerManager.GetPlayer(id)!.CurrentScene, out scene);
    }

    private static ushort[] GetPlayers(string scene) => _serverApi.ServerManager.Players
        .Where(p => p.CurrentScene == scene).Select(p => p.Id).ToArray();

    private static ushort[] GetPlayers(string scene, ushort id) => _serverApi.ServerManager.Players
        .Where(p => p.CurrentScene == scene).Select(p => p.Id).Where(i => i != id)
        .ToArray();
    
    private class UcsServerScene(string name)
    {
        public readonly string SceneName = name;

        public bool Editing;
        public int Selected;
        public int Placed;
        
        public readonly Dictionary<ushort, int> Wins = [];
        public readonly Dictionary<ushort, bool> Won = [];
    }
}
