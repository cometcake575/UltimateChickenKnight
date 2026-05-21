using System;
using Architect.Objects;

namespace UltimateChickenKnight.Picker;

public class EraserElement : IPoolElement
{
    public SelectableObject Generate(int seed) => UcsEraser.Instance;
    
    public int GetWeight() => Math.Min(30 * UcsEditManager.CurrentEdits.Placements.Count, 1000);
}
