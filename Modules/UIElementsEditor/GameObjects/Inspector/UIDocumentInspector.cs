// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(UIDocument))]
    internal class UIDocumentInspector : PanelComponentInspectorBase
    {
        protected override Type parentObjectType => typeof(UIDocument);
    }
}
