using System.Collections;
using System.Collections.Generic;
using Architect.Behaviour.Custom;
using Architect.Behaviour.Utility;
using Architect.Config;
using Architect.Config.Types;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using Architect.Utils;
using UltimateChickenKnight.Mp;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltimateChickenKnight;

public static class UcsObjects
{
    public static readonly List<SafeRange> SafeRanges = [];
    public static Vector3 StartPos;

    public static void Init()
    {
        UltimateChickenKnight.Arena.Add(CreateSafeRange());
        UltimateChickenKnight.Arena.Add(CreateStart());
        UltimateChickenKnight.Arena.Add(CreateEnd());
    }

    private static readonly List<ConfigType> SafeRangeConfig = [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Safe Range", "start_safe_range", (o, value) =>
        {
            if (value.GetValue()) return;
            o.GetComponentInChildren<SafeRange>().gameObject.SetActive(false);
        }).WithDefaultValue(true))
    ];

    private static readonly List<ConfigType> StartPointConfig = GroupUtils.Merge(SafeRangeConfig, [
        ConfigurationManager.RegisterConfigType(new ChoiceConfigType("Facing Direction", "start_face_dir", (o, value) =>
        {
            o.GetComponent<StartPoint>().isRight = value.GetValue() == 1;
        }).WithOptions("Left", "Right").WithDefaultValue(0))
    ]);

    private static PlaceableObject CreateStart()
    {
        var start = new GameObject("UCS Start");
        start.SetActive(false);
        Object.DontDestroyOnLoad(start);

        var sro = CreateSafeRangeObject();
        sro.transform.SetParent(start.transform);
        sro.transform.localPosition = Vector3.zero;
        sro.SetActive(true);
        
        start.transform.SetPositionZ(0.006f);
        
        start.AddComponent<CustomTransitionPoint>();
        var point = start.AddComponent<TransitionPoint>();
        point.entryPoint = "";
        point.nonHazardGate = true;
        
        start.AddComponent<RespawnMarker>();
        start.AddComponent<StartPoint>();
        start.AddComponent<SpriteRenderer>().sprite = 
            ResourceUtils.LoadSpriteResource("start_point", FilterMode.Point, ppu: 7.5f);
        start.AddComponent<FloatAnim>();
        
        return new CustomObject("Starting Point", "ucs_start", start, 
            description: "Defines the spawn point for the room.\n" +
                         "If multiple start points are placed, each player will start at a random one.\n\n" +
                         "Objects cannot be added by players within the Safe Range.")
            .WithConfigGroup(StartPointConfig);
    }

    private static PlaceableObject CreateEnd()
    {
        var end = new GameObject("UCS End");
        end.SetActive(false);
        Object.DontDestroyOnLoad(end);

        var sro = CreateSafeRangeObject();
        sro.transform.SetParent(end.transform);
        sro.transform.localPosition = Vector3.zero;
        sro.SetActive(true);
        
        end.transform.SetPositionZ(0.006f);

        var cc = end.AddComponent<CircleCollider2D>();
        cc.radius = 1.2f;
        cc.isTrigger = true;
        
        var cs = end.AddComponent<ParticleSystem>();
        
        var rcs = end.GetComponent<ParticleSystemRenderer>();
        rcs.material = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = ResourceUtils.LoadSpriteResource("confetti", FilterMode.Point).texture
        };
        
        var main = cs.main;
        main.maxParticles = 100000;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.blue);

        var emission = cs.emission;
        emission.rateOverTime = 0;

        var shape = cs.shape;
        shape.scale = new Vector3(1, 1, 0);

        var vol = cs.velocityOverLifetime;
        vol.enabled = true;
        vol.speedModifier = new ParticleSystem.MinMaxCurve(0.5f, 2);

        var sol = cs.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1, 
            new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
        
        end.AddComponent<EndPoint>().confettiSystem = cs;
        
        end.AddComponent<SpriteRenderer>().sprite = 
            ResourceUtils.LoadSpriteResource("goal", FilterMode.Point, ppu: 7.5f);
        
        return new CustomObject("End Point", "ucs_end", end, 
            description: "Defines the end point for the room.\n" +
                         "If multiple end points are placed, reaching any of them will count as a win.\n\n" +
                         "Objects cannot be added by players within the Safe Range.")
            .WithConfigGroup(SafeRangeConfig);
    }

    private static PlaceableObject CreateSafeRange()
    {
        var safeRange = CreateSafeRangeObject();
        Object.DontDestroyOnLoad(safeRange);
        return new CustomObject("Safe Range", "ucs_safe", safeRange,
                description: "Objects cannot be added by players within the Safe Range.");
    }

    private static readonly Sprite SafeRangeSprite = 
        ResourceUtils.LoadSpriteResource("safe_range", FilterMode.Point, ppu:98);

    private static GameObject CreateSafeRangeObject()
    {
        var safeRange = new GameObject("UCS Safe");
        safeRange.SetActive(false);
        safeRange.transform.localScale *= 5;
        safeRange.transform.SetPositionZ(0.006f);

        safeRange.AddComponent<SafeRange>();
        var col = safeRange.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1;
        safeRange.AddComponent<SpriteRenderer>().sprite = SafeRangeSprite;

        return safeRange;
    }
    
    public class SafeRange : PreviewableBehaviour
    {
        private SpriteRenderer _renderer;
        
        private void Start()
        {
            if (isAPreview) return;
            transform.SetParent(null, true);
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (isAPreview) return;
            _renderer.enabled = UcsEditManager.IsEditing || !UcsEditManager.IsPlaying;
        }

        public bool WithinRange(Vector2 pos)
        {
            return Vector2.Distance(transform.position, pos) <= transform.GetScaleX() / 2;
        }

        private void OnEnable()
        {
            if (isAPreview) return;
            SafeRanges.Add(this);
        }

        private void OnDisable()
        {
            if (isAPreview) return;
            SafeRanges.Remove(this);
        }
    }
    
    public class StartPoint : MonoBehaviour
    {
        public bool isRight;
        
        private void Start()
        {
            name = "ucs_start";
            
            PlayerData.instance.SetBenchRespawn(
                GetComponent<RespawnMarker>(),
                GameManager.instance.sceneName,
                0);
            PlayerData.instance.SetHazardRespawn(transform.position, isRight);

            StartPos = transform.position;
        }
    }

    public class EndPoint : MonoBehaviour
    {
        public ParticleSystem confettiSystem;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (!UcsEditManager.IsPlaying || UcsEditManager.IsEditing || UcsEditManager.RoundOver) return;

            if (!collider.GetComponent<HeroController>()) return;
            UcsClientAddon.SendRoundInfo(true);
            UcsEditManager.RoundOver = true;
            UcsEditManager.DidWin = true;
            UcsEditManager.StartedSpectating = false;
            UcsEditManager.IsSpectating = true;

            StartCoroutine(EndControl());
            PlayerData.instance.isInvincible = true;
            
            if (confettiSystem) confettiSystem.Emit(200);
        }
        
        private static IEnumerator EndControl()
        {
            yield return HeroController.instance.FreeControl();
            HeroController.instance.RelinquishControl();
        }
    }
}