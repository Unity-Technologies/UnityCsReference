// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    /// <summary>
    /// Implement this interface to intercept the visitation of any primitive type.
    /// </summary>
    public interface IVisitPrimitivesPropertyAdapter :
        IVisitPropertyAdapter<sbyte>,
        IVisitPropertyAdapter<short>,
        IVisitPropertyAdapter<int>,
        IVisitPropertyAdapter<long>,
        IVisitPropertyAdapter<byte>,
        IVisitPropertyAdapter<ushort>,
        IVisitPropertyAdapter<uint>,
        IVisitPropertyAdapter<ulong>,
        IVisitPropertyAdapter<float>,
        IVisitPropertyAdapter<double>,
        IVisitPropertyAdapter<bool>,
        IVisitPropertyAdapter<char>
    {
    }
}
