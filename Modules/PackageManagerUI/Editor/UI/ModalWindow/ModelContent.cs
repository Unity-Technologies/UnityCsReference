// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class ModelContent : VisualElement
{
    public EditorWindow container { get; set; }

    public string windowTitle { get; protected set; }

    public abstract void OnModalClosed();
}
