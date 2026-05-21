using System;
using System.Collections;
using System.Linq;
using Architect.Content.Preloads;
using Architect.Editor;
using Architect.Placements;
using Architect.Utils;
using GlobalEnums;
using UltimateChickenKnight.Mp;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UltimateChickenKnight;

public static class UcsEditManager
{
    public static bool IsPlaying;
    public static bool OpenEditor;
    public static bool RoundOver;
    public static bool DidWin;
    public static bool IsSpectating;
    public static bool StartedSpectating;

    public static bool IsEditing
    {
        get;
        set
        {
            field = value;
            if (PlayerData.instance != null) PlayerData.instance.isInvincible = value;
            if (!value)
            {
                UcsEditorUI.Close();
                RoundOver = false;
            }
        }
    }

    public static LevelData CurrentEdits = new([], [], [], []);
    
    public static void Init()
    {
        EditManager.ToggleChecks.Add(() => !IsPlaying);
        
        typeof(HeroController).Hook(nameof(HeroController.SceneInit),
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                
                if (!PreloadManager.HasPreloaded || !IsPlaying) return;

                foreach (var placement in CurrentEdits.Placements)
                {
                    if (IsEditing) placement.PlaceGhost(store: false);
                    else
                    { 
                        var obj = placement.SpawnObject();
                        obj.RemoveComponent<PersistentBoolItem>();
                        obj.RemoveComponent<PersistentIntItem>();
                    }
                }

                if (IsEditing && OpenEditor) UcsEditorUI.DoOpen();
                OpenEditor = false;
            });
        
        typeof(TransitionPoint).Hook(nameof(TransitionPoint.Start),
            (Action<TransitionPoint> orig, TransitionPoint self) =>
            {
                if (IsPlaying) self.StartCoroutine(DeactivateLater(self));
                orig(self);
            });
        
        typeof(HeroController).Hook(nameof(HeroController.CanTakeDamage), BlockAction);
        
        typeof(GameManager).Hook(nameof(GameManager.FindTransitionPoint),
            (Func<GameManager, string, Scene, bool, TransitionPoint> orig,
                GameManager self,
                string entryPointName,
                Scene filterScene,
                bool fallbackToAnyAvailable) =>
            {
                if (entryPointName == "ucs_start")
                {
                    var points = TransitionPoint.TransitionPoints.Where(transitionPoint => 
                        transitionPoint.name == "ucs_start" && 
                        (!filterScene.IsValid() || transitionPoint.gameObject.scene == filterScene)).ToList();
                    if (!points.IsNullOrEmpty())
                    {
                        return points[Random.RandomRangeInt(0, points.Count)];
                    }
                }
                return orig(self, entryPointName, filterScene, fallbackToAnyAvailable);
            });
    }

    private static bool BlockAction(Func<HeroController, bool> orig, HeroController self) => !IsEditing && !IsSpectating && orig(self);
    
    private static IEnumerator DeactivateLater(TransitionPoint obj)
    {
        yield return new WaitForSeconds(1);
        Object.Destroy(obj);
    }

    public static void GoToScene(string sceneName)
    {
        IsSpectating = false;
        HeroController.instance.vignette.gameObject.SetActive(true);
        HeroController.instance.MaxHealth();
        UltimateChickenKnight.Instance.StartCoroutine(DoGoToScene(sceneName));
    }

    private static IEnumerator DoGoToScene(string sceneName)
    {
        if (GameManager.instance.isPaused)
        {
            yield return GameManager.instance.PauseGameToggle();
        }
        GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
        {
            SceneName = sceneName,
            EntryGateName = "ucs_start"
        });
    }
    
    public static void Update()
    {
        if (!GameManager._instance || !GameManager._instance.IsGameplayScene()) return;

        if (!IsPlaying)
        {
            IsSpectating = false;
            if (IsEditing) IsEditing = false;
        }
        
        if (IsSpectating)
        {
            var actions = InputHandler.Instance.inputActions;
            var left = actions.left.WasPressed;
            var right = actions.right.WasPressed;
            HeroController.instance.ResetHardLandingTimer();
            if (left != right)
            {
                StartedSpectating = true;
                UcsClientAddon.MoveSpectate(left ? -1 : 1);
            }
            
            if (!DidWin) HeroController.instance.transform.SetPosition2D(9999999, 9999999);

            if (StartedSpectating)
            {
                GameManager.instance.cameraCtrl.isGameplayScene = false;
                var sp = UcsClientAddon.GetSpectatePos();
                GameManager.instance.cameraCtrl.SnapTo(sp.x, sp.y);
            }
        }

        if (IsEditing)
        {
            DoFlyMovement();
            HeroController.instance.ResetHardLandingTimer();
        }
    }
    
    private static Vector3 _noclipPos;
    
    private static void DoFlyMovement()
    {
        var actions = InputHandler.Instance.inputActions;
        var paused = GameManager.instance.isPaused;
        
        var up = actions.up.IsPressed;
        var down = actions.down.IsPressed;
        var left = actions.left.IsPressed;
        var right = actions.right.IsPressed;

        var speed = actions.dash.IsPressed ? 35 : 20;
        if (!EditManager.LoadPos)
        {
            if (!paused && up != down) _noclipPos += (up ? Vector3.up : Vector3.down) * (Time.deltaTime * speed);
            if (!paused && left != right) _noclipPos += (left ? Vector3.left : Vector3.right) * (Time.deltaTime * speed);
        }

        if (HeroController.instance.transitionState == HeroTransitionState.WAITING_TO_TRANSITION)
            HeroController.instance.transform.position = _noclipPos;
        else _noclipPos = HeroController.instance.transform.position;
    }
}