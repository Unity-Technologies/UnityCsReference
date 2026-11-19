// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    public static partial class GraphDatabase
    {
        static GraphDatabase()
        {
            PublicGraphFactory.EnsureStaticConstructorIsCalled();
        }
    }
}
