// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.ObjectModel;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    internal interface IHasHints
    {
        public ReadOnlyDictionary<string, string> Hints { get; }
    }
}
