using System.Collections;
using Architect.Utils;
using Modding;
using UltimateChickenKnight.Mp;
using UnityEngine;

namespace UltimateChickenKnight;

public static class UcsRoundManager
{
    public static void Init()
    {
        ModHooks.TakeDamageHook += (ref type, damage) =>
        {
            if (!UcsEditManager.IsPlaying || damage == 0) return damage;
            Died(type == 1 ? UltimateChickenKnight.DeathType.Damage : UltimateChickenKnight.DeathType.Hazard);
            if (damage >= PlayerData._instance.health)
            {
                Died(UltimateChickenKnight.DeathType.Death);
                return 0;
            }
            return damage;
        };
    }

    private static void Died(UltimateChickenKnight.DeathType type)
    {
        if (UcsEditManager.RoundOver || !UcsEditManager.IsPlaying || UcsEditManager.IsEditing ||
            (type != UltimateChickenKnight.Instance.Settings.DeathType && 
             type != UltimateChickenKnight.DeathType.Death
             && UltimateChickenKnight.Instance.Settings.DeathType != UltimateChickenKnight.DeathType.Damage)) return;
        
        UcsEditManager.RoundOver = true;
        UltimateChickenKnight.Instance.StartCoroutine(Deactivate());
    }

    private static IEnumerator Deactivate()
    {
        yield return new WaitForSeconds(0.2f);
        yield return HeroController.instance.FreeControl(hero => !hero.cState.dead);
        
        UcsEditManager.DidWin = false;
        UcsEditManager.IsSpectating = true;
        UcsEditManager.StartedSpectating = true;
        HeroController.instance.transform.SetPosition2D(9999999, 9999999);
        HeroController.instance.vignette.gameObject.SetActive(false);
        
        UcsClientAddon.SendRoundInfo(false);
    }
}