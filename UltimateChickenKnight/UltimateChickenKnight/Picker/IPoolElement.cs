using Architect.Objects;

namespace UltimateChickenKnight.Picker;

public interface IPoolElement
{
    public SelectableObject Generate(int seed);

    public int GetWeight();
}