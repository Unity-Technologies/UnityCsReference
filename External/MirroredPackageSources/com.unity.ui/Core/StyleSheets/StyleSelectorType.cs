using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    internal enum StyleSelectorType
    {
        Unknown,
        Wildcard,
        Type,
        Class,
        PseudoClass,
        RecursivePseudoClass,
        ID,
        Predicate
    }
}
