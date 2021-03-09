// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal interface IDetailsExtension : IExtension
    {
        bool expanded { set; get; }
        string title { set; get; }

        void Add(VisualElement element);
    }
}
