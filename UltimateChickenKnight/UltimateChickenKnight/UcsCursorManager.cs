using System.Linq;
using Architect.Config;
using Architect.Objects;
using Architect.Objects.Placeable;
using Architect.Utils;
using UnityEngine;

namespace UltimateChickenKnight;

public static class UcsCursorManager
{
    private static GameObject _cursorObject;
    public static PreviewUtils.Preview CursorPreview;

    public static MeshRenderer[] Mrs = [];
    public static SpriteRenderer[] Srs = [];

    public static bool ObjectChanged = true;

    public static void Refresh(SelectableObject selectable)
    {
        var type = selectable as PlaceableObject;
        
        if (ObjectChanged && _cursorObject)
        {
            Object.Destroy(_cursorObject);
            _cursorObject = null;
        }
        ObjectChanged = false;

        if (_cursorObject)
        {
            _cursorObject.SetActive(true);
        } else if (type != null) {
            var wasPrefabActive = type.Prefab.activeSelf;
            type.Prefab.SetActive(false);
            var obj = Object.Instantiate(type.Prefab);
            if (wasPrefabActive) type.Prefab.SetActive(true);

            obj.name = "[UCS] Cursor";
            Object.DontDestroyOnLoad(obj);

            type.PostSpawnAction?.Invoke(obj);
            
            _cursorObject = obj;

            var offset = Vector3.zero;
            if (type.SpritePreview)
            {
                _cursorObject = PreviewUtils.MakeSpritePreview(
                    _cursorObject, 
                    type, 
                    UcsEditorUI.CursorFlipped, UcsEditorUI.CursorRot, 1, 
                    out offset);
            }
        
            var cursor = _cursorObject.AddComponent<UcsEditorUI.UcsCursor>();
            cursor.offset = offset;
            CursorPreview = _cursorObject.AddComponent<PreviewUtils.Preview>();
            CursorPreview.Setup(type);

            CursorPreview.Settings = new PreviewUtils.PreviewSettings(1, 0.2f, 0.2f, 0.5f);
        
            _cursorObject.SetActive(true);
            
            foreach (var rb2d in _cursorObject.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            Mrs = _cursorObject.GetComponentsInChildren<MeshRenderer>();
            Srs = _cursorObject.GetComponentsInChildren<SpriteRenderer>();
        }
        else
        {
            _cursorObject = new GameObject("[UCS] Cursor");
            Object.DontDestroyOnLoad(_cursorObject);

            _cursorObject.AddComponent<SpriteRenderer>().sprite = selectable.GetUISprite();
            _cursorObject.AddComponent<UcsEditorUI.UcsCursor>();
            
            Mrs = [];
            Srs = [];
        }

        if (type != null)
        {
            var rot = UcsEditorUI.CursorRot + type.Prefab.transform.GetRotation2D();
            _cursorObject.transform.SetRotation2D(rot);

            _cursorObject.transform.localScale = type.IgnoreScale ? Vector3.one : type.Prefab.transform.localScale;

            _cursorObject.transform.SetScaleX((UcsEditorUI.CursorFlipped ? -1 : 1)
                                              * (type.IgnoreScale ? 1 : type.Prefab.transform.GetScaleX()));


            foreach (var configVal in type.ConfigGroup.Select(c => c.GetDefaultValue())
                         .Where(c => c != null)
                         .OrderBy(configVal => configVal.GetPriority()))
                configVal.SetupPreview(_cursorObject, ConfigurationManager.PreviewContext.Cursor);
        }
    }
}