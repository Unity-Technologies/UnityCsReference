// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class ModalContent : VisualElement
{
    public EditorWindow container { get; set; }

    public string windowTitle { get; protected set; }

    // We chose to have a function that executes right before the modal is shown
    // instead of right after because ShowModal() blocks the thread until the modal is closed.
    public abstract void OnBeforeShowModal();
    public abstract void OnModalClosed();
}
