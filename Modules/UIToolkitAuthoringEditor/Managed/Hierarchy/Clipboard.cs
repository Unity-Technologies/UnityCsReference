// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

internal class Clipboard
{
    public static Clipboard GetClipboardForStage()
    {
        switch (StageUtility.GetCurrentStage())
        {
            case VisualElementEditingStage uiStage:
                return uiStage.Clipboard;
        }

        return s_Clipboard;
    }

    // Fallback clipboard singleton with no rebuild path; must persist across code reloads.
    [NoAutoStaticsCleanup]
    static readonly Clipboard s_Clipboard = new();

    readonly List<VisualElementAsset> m_CutElements = new();
    [NoAutoStaticsCleanup] // batch-mode copy buffer, safe to persist
    static string s_BatchModeCopyBuffer;

    public static string SystemCopyBuffer
    {
        get
        {
            if (Application.isBatchMode || !Application.isHumanControllingUs)
                return s_BatchModeCopyBuffer;
            return GUIUtility.systemCopyBuffer;
        }
        set
        {
            if (Application.isBatchMode || !Application.isHumanControllingUs)
                s_BatchModeCopyBuffer = value;
            else
                GUIUtility.systemCopyBuffer = value;
        }
    }

    public void SetCutElements(IReadOnlyList<VisualElementAsset> cutElements)
    {
        m_CutElements.Clear();
        m_CutElements.AddRange(cutElements);
    }

    public IReadOnlyList<VisualElementAsset> GetCutElements() => m_CutElements;

    public void ClearCutElements()
    {
        m_CutElements.Clear();
    }

    public void Dispose()
    {
        m_CutElements.Clear();
    }

    public static bool IsSystemCopyBufferUxml()
    {
        var buffer = SystemCopyBuffer;
        if (string.IsNullOrWhiteSpace(buffer))
            return false;

        var trimmedBuffer = buffer.Trim();
        return trimmedBuffer.StartsWith("<") && trimmedBuffer.EndsWith(">");
    }

    public static bool IsSystemCopyBufferUss()
    {
        var buffer = SystemCopyBuffer;
        if (string.IsNullOrWhiteSpace(buffer))
            return false;

        var trimmedBuffer = buffer.Trim();
        return trimmedBuffer.EndsWith("}");
    }
}
