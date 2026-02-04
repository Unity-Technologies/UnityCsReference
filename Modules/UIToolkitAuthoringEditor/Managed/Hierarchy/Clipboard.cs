// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class Clipboard
{
    readonly List<VisualElement> m_CutElements = new();

    public void SetCutElements(IReadOnlyList<VisualElement> cutElements)
    {
        m_CutElements.Clear();
        m_CutElements.AddRange(cutElements);
    }

    public IReadOnlyList<VisualElement> GetCutElements() => m_CutElements;

    public void ClearCutElements()
    {
        m_CutElements.Clear();
    }

    public void Dispose()
    {
        m_CutElements.Clear();
    }
}
