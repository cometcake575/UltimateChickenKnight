using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Config;
using Architect.Editor;
using Architect.Objects;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using Architect.Placements;
using Architect.Sharer;
using Architect.Storage;
using Architect.Utils;
using UltimateChickenKnight.Mp;
using UltimateChickenKnight.Mp.Data;
using UltimateChickenKnight.Picker;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static System.String;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UltimateChickenKnight;

public static class UcsEditorUI
{
    private static readonly Sprite BoxSprite = ResourceUtils.LoadSpriteResource("box", FilterMode.Point);
    
    #region Pool
    private static readonly Pool Pool = new(
        new EraserElement(),
        new CategoryElement(Categories.Enemies, 200),
        new CategoryElement(Categories.Abilities, 40, s =>
        {
            return s switch
            {
                    "Mask Shard" or 
                    "Vessel Fragment" => false,
                _ => true
            };
        }),
        new CategoryElement(Categories.Hazards, 800),
        new CategoryElement(Categories.Misc, 140, s =>
        {
            if (s.Contains("Statue") || s.Contains("Breakable") || s.Contains("Lantern")) return true;
            return s switch
            {
                "Grub Bottle"
                    or "Infected Blob S"
                    or "Infected Blob L"
                    or "Infected Vine"
                    or "Lifeblood Cocoon"
                    or "Millibelle"
                    or "Crystal Barrel"
                    or "Infected Drip" => true,
                _ => false
            };
        }),
        new CategoryElement(Categories.Solids, 160),
        new CategoryElement(Categories.Interactable, 25, s => 
            s.Contains("Breakable") || 
            s.Contains("Dive") || 
            s.Contains("Soul") || 
            s.Contains("Retainer") || 
            s == "Shade Gate"),
        new CategoryElement(Categories.Platforming, 500, s =>
        {
            return s switch
            {
                "Colosseum Wall" or "Colosseum Platform" => false,
                _ => true
            };
        })
    );
    #endregion
    
    public static int Seed;
    
    private static GameObject _ui;
    private static GameObject _selector;
    private static GameObject _playerInfo;
    private static readonly List<PlayerInfo> InfoLines = [];

    private static NamePreview _namePreview;
    
    private static GameObject _cursorObj;
    private static float _cursorRot;
    private static bool _cursorFlipped;
    private static UcsCursor _cursor;
    private static SpriteRenderer _cursorRenderer;

    private static readonly List<ImageSelector> Images = [];

    public static void Init()
    {
        _ui = new GameObject("[UCS] UI");
        Object.DontDestroyOnLoad(_ui);

        _ui.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = _ui.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _ui.AddComponent<GraphicRaycaster>();

        var img = UIUtils.MakeImage("Selector", _ui, Vector2.zero,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(200, 200));

        img.preserveAspect = true;
        img.sprite = BoxSprite;
        _selector = img.gameObject;
        _selector.RemoveOffset();
        _selector.SetActive(false);
        
        ((RectTransform)_selector.transform).sizeDelta = new Vector2(200, 200);

        var i = 0;
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var select = UIUtils.MakeImage($"Object {i}", _selector,
                    new Vector2(x * 250, y * 250),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(250, 250));
                select.preserveAspect = true;

                var selector = select.gameObject.AddComponent<ImageSelector>();
                selector.img = select;
                selector.index = i;
                Images.Add(selector);
                
                i++;
            }
        }
        
        typeof(EditManager).Hook(nameof(EditManager.ShouldShowCursor),
            (Func<bool> orig) => orig() || UcsEditManager.IsEditing && _currentObject == null);
        
        _cursorObj = new GameObject("[UCS] Cursor");
        _cursorObj.SetActive(false);
        Object.DontDestroyOnLoad(_cursorObj);
        
        _cursor = _cursorObj.AddComponent<UcsCursor>();
        _cursorRenderer = _cursorObj.AddComponent<SpriteRenderer>();
        
        var userInfo = UIUtils.MakeImage("User Info", _ui, Vector2.zero,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(200, 200));
        
        userInfo.preserveAspect = true;
        userInfo.sprite = BoxSprite;
        _playerInfo = userInfo.gameObject;
        _playerInfo.RemoveOffset();
        _playerInfo.SetActive(false);

        for (var x = 0; x <= 1; x++)
        {
            for (var y = 1.5f; y >= -1.5f; y--)
            {
                var userLine = new GameObject($"User {y}");
                userLine.transform.SetParent(_playerInfo.transform);
                userLine.RemoveOffset();

                var pi = userLine.AddComponent<PlayerInfo>();

                pi.img = UIUtils.MakeImage("Profile", userLine,
                    new Vector2(-200 + x * 260, y * 135),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(200, 200));
                pi.img.preserveAspect = true;

                pi.userText = UIUtils.MakeLabel("Name", userLine,
                        new Vector2(-100 + x * 260, y * 135),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f))
                    .textComponent;
                pi.userText.color = Color.black;
                pi.userText.fontSize = 21;
                pi.userText.alignment = TextAnchor.UpperLeft;

                pi.scoreText = UIUtils.MakeLabel("Score", userLine,
                        new Vector2(-100 + x * 260, y * 135),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f))
                    .textComponent;
                pi.scoreText.color = Color.black;
                pi.scoreText.fontSize = 21;
                pi.scoreText.alignment = TextAnchor.LowerLeft;
                
                InfoLines.Add(pi);
            }
        }

        ((RectTransform)_playerInfo.transform).sizeDelta = new Vector2(600, 600);

        var np = UIUtils.MakeImage("Name Preview", _ui, Vector2.zero,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 10));
        np.raycastTarget = false;

        np.color = Color.black;
        np.sprite = UIUtils.Square;
        
        _namePreview = np.gameObject.AddComponent<NamePreview>();
        _namePreview.img = np;
        _namePreview.text = UIUtils.MakeLabel("Text", _namePreview.gameObject,
            Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).textComponent;
        _namePreview.text.fontSize = 24;
        _namePreview.text.raycastTarget = false;
        _namePreview.text.alignment = TextAnchor.MiddleCenter;
        
        _namePreview.gameObject.SetActive(false);
    }

    public class NamePreview : MonoBehaviour
    {
        public Image img;
        public Text text;
        public string currentText;
        private Vector3 _offset;

        private void Update()
        {
            transform.position = Input.mousePosition + _offset;
        }

        public void UpdateText(string txt)
        {
            currentText = txt;
            text.text = txt;
            img.rectTransform.sizeDelta = new Vector2(text.preferredWidth + 5, text.preferredHeight + 5);
            _offset = img.rectTransform.sizeDelta;
            Update();
        }
    }

    public class PlayerInfo : MonoBehaviour
    {
        public Image img;
        public Text userText;
        public Text scoreText;
    }

    public class ImageSelector : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image img;
        public int index;
        public SelectableObject Target;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_currentObject != null) return;
            _currentObject = Target;
            gameObject.SetActive(false);
            
            UcsClientAddon.Send(index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _namePreview.UpdateText(Target.GetName());
            _namePreview.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_namePreview.currentText == Target.GetName()) _namePreview.gameObject.SetActive(false);
        }
    }

    public static void DoOpen()
    {
        UltimateChickenKnight.Instance.StartCoroutine(Open());
    }
    
    private static IEnumerator Open()
    {
        _currentObject = null;

        List<SelectableObject> selected = [];
        foreach (var selector in Images)
        {
            var img = selector.img;
            
            img.gameObject.SetActive(true);
            
            var selectable = Pool.Generate(Seed);

            while (selected.Contains(selectable) || Random.value > 0.8f)
            {
                Seed++;
                selectable = Pool.Generate(Seed);
            }
            selected.Add(selectable);
            selector.Target = selectable;

            if (selectable is PreloadObject { Loaded: false }) yield return selectable.EnsureLoaded();
            
            img.sprite = selectable.GetUISprite();

            var rot = 0f;
            
            img.transform.SetScaleX(0.5f);
            img.transform.SetScaleY(0.5f);

            if (selectable is PlaceableObject placeable)
            {
                switch (placeable.GetUISprite().packingRotation)
                {
                    case SpritePackingRotation.FlipHorizontal:
                        img.transform.SetScaleX(-0.5f);
                        break;
                    case SpritePackingRotation.FlipVertical:
                        img.transform.SetScaleY(-0.5f);
                        break;
                    case SpritePackingRotation.Rotate180:
                        rot += 180;
                        break;
                }

                rot += placeable.Rotation + placeable.ChildRotation + placeable.Tk2dRotation;
            }
            
            img.transform.SetRotationZ(rot);
            
            Seed = Random.RandomRangeInt(0, int.MaxValue);
        }
        _selector.SetActive(true);
    }

    private static SelectableObject _currentObject;

    public static void Select(int index)
    {
        Images[index].gameObject.SetActive(false);
    }

    public static void Close()
    {
        _selector.SetActive(false);
        _namePreview.gameObject.SetActive(false);
    }

    public static void FinishSelect()
    {
        UltimateChickenKnight.Instance.StartCoroutine(DoFinishSelect());
    }

    private static IEnumerator DoFinishSelect()
    {
        yield return new WaitForSeconds(1);
        
        _selector.SetActive(false);
        _namePreview.gameObject.SetActive(false);
        _cursorObj.SetActive(true);
        foreach (var fc in ForeignCursors.Values) fc.gameObject.SetActive(true);
        _cursorRot = 0;
        _cursorFlipped = false;
        
        RefreshCursor();
    }

    private static void RefreshCursor()
    {
        Vector3 offset = default;
        var rot = 0f;
        if (_currentObject is PlaceableObject obj)
        {
            rot += _cursorRot + obj.Rotation;

            _cursorRenderer.sprite = obj.Sprite;

            offset = PreviewUtils.FixPreview(
                _cursorRenderer,
                obj,
                _cursorFlipped,
                rot,
                1);
            offset.z += obj.ZPosition + 0.001f;

            rot += obj.ChildRotation + obj.Tk2dRotation;
            _cursorObj.transform.localScale = obj.LossyScale;

            foreach (var config in obj.ConfigGroup.Select(cfg => cfg.GetDefaultValue())
                         .Where(dv => dv != null).OrderBy(configVal => configVal.GetPriority()))
                config.SetupPreview(_cursorObj, ConfigurationManager.PreviewContext.Cursor);
        }
        else
        {
            _cursorRenderer.sprite = _currentObject.GetUISprite();
            _cursorObj.transform.localScale = new Vector3(0.2f, 0.2f);
        }

        _cursor.offset = offset;
        _cursorObj.transform.SetRotation2D(rot);
    }
    
    private class UcsCursor : MonoBehaviour
    {
        public Vector3 offset;
        
        private void Update()
        {
            if (Settings.Rotate.IsPressed && _currentObject is PlaceableObject p)
            {
                var group = p.GetRotationGroup();
                
                float i;
                if (group == RotationGroup.All && Settings.Rotate.IsPressed) i = Time.deltaTime * 60;
                else if (Settings.Rotate.WasPressed)
                    i = group switch
                    {
                        RotationGroup.Vertical => 180,
                        RotationGroup.Three => 90,
                        RotationGroup.Four => 90,
                        RotationGroup.Eight => 45,
                        _ => 0
                    };
                else return;
                if (Input.GetKey(KeyCode.LeftShift)) i = -i;

                var rot = _cursorRot + i;
                if (group == RotationGroup.Three && Mathf.Approximately(rot, 180)) rot = 270;

                _cursorRot = rot;
                RefreshCursor();
            }

            if (Settings.Flip.WasPressed)
            {
                _cursorFlipped = !_cursorFlipped;
                RefreshCursor();
            }
            
            transform.position = EditManager.GetWorldPos(Input.mousePosition) + offset;
            if (!UcsEditManager.IsEditing) gameObject.SetActive(false);

            string id;
            if (_currentObject is PlaceableObject place) id = place.GetId();
            else id = "eraser";
            UcsClientAddon.SendCursorInfo(
                id,
                transform.GetRotation2D(),
                transform.position,
                transform.localScale);

            if (!Input.GetMouseButtonDown(0)) return;

            if (_currentObject is PlaceableObject placeable)
            {
                var pos = EditManager.GetWorldPos(Input.mousePosition);
                pos.z = placeable.ZPosition;

                if (UcsObjects.SafeRanges.Any(o =>
                    {
                        var closest = _cursorRenderer.bounds.ClosestPoint(o.transform.position);
                        if (_currentObject.GetName() == "Falling Lava") closest.y = o.transform.GetPositionY();
                        return o && o.WithinRange(closest);
                    }))
                {
                    StartCoroutine(BlinkRed());
                    return;
                }

                gameObject.SetActive(false);
                var obj = new ObjectPlacement(
                    placeable, 
                    pos,
                    $"{Guid.NewGuid().ToString()[..8]} [UCS]",
                    _cursorFlipped,
                    _cursorRot,
                    1,
                    false,
                    0,
                    [],
                    [],
                    placeable.ConfigGroup
                        .Select(cfg => cfg.GetDefaultValue())
                        .Where(dv => dv != null).ToArray()
                );

                UcsClientAddon.DoPlace(obj);
            }
            else
            {
                var placement = UcsEditManager.CurrentEdits.Placements
                    .FirstOrDefault(placement => placement.Touching(Input.mousePosition));

                if (placement == null) return;
                
                gameObject.SetActive(false);
                UcsClientAddon.DoErase(placement);
            }
        }

        private static IEnumerator BlinkRed()
        {
            for (var i = 0f; i < 1f; i += 0.1f)
            {
                _cursorRenderer.color = Color.Lerp(Color.white, Color.red, i);
                yield return new WaitForSeconds(0.01f);
            }
            for (var i = 0f; i < 1f; i += 0.1f)
            {
                _cursorRenderer.color = Color.Lerp(Color.red, Color.white, i);
                yield return new WaitForSeconds(0.01f);
            }

            _cursorRenderer.color = Color.white;
        }
    }

    private static readonly Dictionary<ushort, UcsForeignCursor> ForeignCursors = [];

    private static void CreateForeignCursor(ushort id)
    {
        var cursor = new GameObject($"[Architect] Foreign Cursor ({id})");
        Object.DontDestroyOnLoad(cursor);

        var fc = cursor.AddComponent<UcsForeignCursor>();
        fc.renderer = cursor.AddComponent<SpriteRenderer>();
        fc.renderer.color = new Color(0.2f, 0.8f, 1.0f, 0.6f);
        ForeignCursors[id] = fc;
    }

    public static IEnumerator UpdateForeignCursor(CursorInfoPacket data)
    {
        if (!ForeignCursors.ContainsKey(data.Id)) CreateForeignCursor(data.Id);

        var got = PlaceableObject.RegisteredObjects.TryGetValue(data.ObjectID, out var obj);
        if (got) yield return obj.EnsureLoaded();

        var cursor = ForeignCursors[data.Id];
        cursor.transform.position = new Vector3(data.Pos.Item1, data.Pos.Item2, data.Pos.Item3);
        cursor.transform.SetRotation2D(data.Rotation);
        cursor.transform.localScale = new Vector3(data.Scale.Item1, data.Scale.Item2, data.Scale.Item3);
        cursor.renderer.sprite = got
            ? obj.Sprite
            : UcsEraser.Instance.GetUISprite();
    }

    private class UcsForeignCursor : MonoBehaviour
    {
        public SpriteRenderer renderer;

        private void Update()
        {
            if (!UcsEditManager.IsEditing) gameObject.SetActive(false);
        }
    }

    public static void ShowInfo(List<(string, string, int)> userInfo)
    {
        userInfo.Sort((a, b) => 
            Compare(a.Item1, b.Item1, StringComparison.InvariantCulture));
        
        _playerInfo.SetActive(true);
        for (var i = 0; i < 8; i++)
        {
            if (i < userInfo.Count)
            {
                var line = InfoLines[i];
                line.gameObject.SetActive(true);
                line.userText.text = userInfo[i].Item1;
                line.scoreText.text = userInfo[i].Item3.ToString();
                SharerManager.DoGetSprite(userInfo[i].Item2, line.img);
            } else InfoLines[i].gameObject.SetActive(false);
        }
    }

    public static void HideInfo() => _playerInfo.SetActive(false);
    
    public static void HideSelect()
    {
        _selector.SetActive(false);
        _namePreview.gameObject.SetActive(false);
    }

    public static void HideForeignCursor(ushort id)
    {
        if (ForeignCursors.TryGetValue(id, out var cursor)) cursor.gameObject.SetActive(false);
    }

    public static void HideForeignCursors()
    {
        foreach (var cursor in ForeignCursors.Values) cursor.gameObject.SetActive(false);
    }
}