
namespace Unity.DataModel
{
internal unsafe partial struct TypeTraits<T>
{
    private static TypeTraitsData _traitsData;

    internal static TypeTraitsData Get()
    {
        return _traitsData;
    }

    internal static void Set(TypeTraitsData traitsData)
    {
        _traitsData = traitsData;

        TypeTraitsRegistry.Register<T>(traitsData);
    }
}
}
