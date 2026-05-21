using System;
using System.Linq;
using Architect.Objects;
using Architect.Objects.Categories;
using Architect.Objects.Placeable;
using Random = UnityEngine.Random;

namespace UltimateChickenKnight.Picker;

public class CategoryElement(Category category, int weight, Func<string, bool> filter = null) : IPoolElement
{
    private readonly SelectableObject[] _objects = filter == null ? category.GetObjects().ToArray()
        : category.GetObjects().Where(o => filter(o.GetName())).ToArray();
    
    public SelectableObject Generate(int seed)
    {
        while (true)
        {
            Random.InitState(seed);

            if (_objects.ElementAt(Random.RandomRangeInt(0, _objects.Length)) is PlaceableObject placeableObject)
                return placeableObject;
            seed++;
        }
    }

    public int GetWeight() => weight;
}