// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

// Spinner cycling the editor's built-in wait icons; its scheduler only ticks while attached to a panel.
class LoadingSpinner : Image
{
    const long k_FrameMs = 83;

    [NoAutoStaticsCleanup] // loaded textures; assets outlive a code reload
    static Texture2D[] s_Frames;

    int m_Frame;

    public LoadingSpinner()
    {
        if (s_Frames == null)
        {
            s_Frames = new Texture2D[12];
            for (var i = 0; i < s_Frames.Length; i++)
                s_Frames[i] = EditorGUIUtility.IconContent($"WaitSpin{i:00}").image as Texture2D;
        }

        AddToClassList(LocClasses.LocSpinner);
        image = s_Frames[0];
        schedule.Execute(Advance).Every(k_FrameMs);
    }

    void Advance()
    {
        m_Frame = (m_Frame + 1) % s_Frames.Length;
        image = s_Frames[m_Frame];
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
