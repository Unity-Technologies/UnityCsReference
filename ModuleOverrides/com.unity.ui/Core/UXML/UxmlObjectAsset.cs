// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal sealed class UxmlObjectAsset : VisualElementAsset
    {
        public UxmlObjectAsset(string fullTypeName)
            : base(fullTypeName) {}
    }
}
