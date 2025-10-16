// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.ShaderFoundry
{
    [FoundryAPI]
    internal enum PropertyDataSource : ushort
    {
        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN PropertyDataSource.h
        None = 0,
        Global,
        PerMaterial,
        Custom
    };
}
