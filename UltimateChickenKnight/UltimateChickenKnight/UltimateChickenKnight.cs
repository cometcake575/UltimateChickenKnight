using System.Collections;
using System.Collections.Generic;
using Modding;
using System.Reflection;
using Architect.Objects.Categories;
using Architect.Sharer.States;
using Hkmp.Api.Client;
using Hkmp.Api.Server;
using UltimateChickenKnight.Mp;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltimateChickenKnight;

internal class UltimateChickenKnight()
    : Mod("UltimateChickenKnight"), 
        IGlobalSettings<UltimateChickenKnight.UckSettings>,
        IMenuMod
{
    public static Category Arena;
    internal static UltimateChickenKnight Instance { get; private set; }
    private UckManager _manager;
    
    public static string Version => Instance.GetVersion();

    public override string GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    public override List<(string, string)> GetPreloadNames()
    {
        Arena = Categories.RegisterCategory("UCS Arena", -1);
        UcsObjects.Init();
        return [];
    }

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        Log("Initializing");

        Instance = this;

        Log("Initialized");
        
        var managerObj = new GameObject("[UCK] Manager");
        Object.DontDestroyOnLoad(managerObj);
        _manager = managerObj.AddComponent<UckManager>();
            
        ClientAddon.RegisterAddon(new UcsClientAddon());
        ServerAddon.RegisterAddon(new UcsServerAddon());

        UcsEditManager.Init();
        UcsEditorUI.Init();
        UcsRoundManager.Init();

        StartCoroutine(Account.PrepareUserInfo());
    }

    public void StartCoroutine(IEnumerator routine)
    {
        _manager.StartCoroutine(routine);
    }

    public class UckManager : MonoBehaviour
    {
        private void Update() => UcsEditManager.Update();
    }

    public UckSettings Settings { get; set; } = new();

    public class UckSettings
    {
        public DeathType DeathType = DeathType.Damage;
    }

    public enum DeathType
    {
        Damage,
        Hazard,
        Death
    }

    public void OnLoadGlobal(UckSettings s)
    {
        Settings = s;
    }

    public UckSettings OnSaveGlobal()
    {
        return Settings;
    }

    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
    {
        return [new IMenuMod.MenuEntry(
            "Death Type",
            ["Damage", "Hazard", "Death"], 
            "Whether a player should lose the round on damage, hazard damage or death", 
            i => Settings.DeathType = (DeathType)i, 
            () => (int)Settings.DeathType)];
    }

    public bool ToggleButtonInsideMenu => false;
}