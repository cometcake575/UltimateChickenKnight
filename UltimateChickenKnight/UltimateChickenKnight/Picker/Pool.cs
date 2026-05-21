using System.Linq;
using Architect.Objects;
using Random = UnityEngine.Random;

namespace UltimateChickenKnight.Picker;

public class Pool(params IPoolElement[] elements) : IPoolElement
{
    public SelectableObject Generate(int seed)
    {
        Random.InitState(seed);

        var n = Random.RandomRangeInt(0, GetWeight());
        foreach (var element in elements)
        {
            n -= element.GetWeight();
            if (n <= 0) return element.Generate(Random.RandomRangeInt(0, int.MaxValue));
        }

        return null;
    }

    public int GetWeight() => elements.Sum(e => e.GetWeight());
}