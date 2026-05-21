using Architect.Objects.Tools;
using UnityEngine;

namespace UltimateChickenKnight;

public class UcsEraser() : ToolObject("eraser", null, -2)
{
    public static readonly UcsEraser Instance = new();
    
    public override string GetName() => "Eraser";

    public override string GetDescription() => "Erases";

    public override void Click(Vector3 mousePosition, bool first) { }
}