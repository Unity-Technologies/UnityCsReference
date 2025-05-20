// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.Toolbars;

class LoadingSpinner : Image
{
    static GUIContent[] s_Wheels;

    ValueAnimation<float> m_Animation;

    public LoadingSpinner()
    {
        if (s_Wheels == null)
        {
            s_Wheels = new GUIContent[12];
            for (var i = 0; i < 12; i++)
            {
                s_Wheels[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));
            }
        }

        style.width = 16;
        style.height = 16;
        style.flexShrink = 0;

        Start();
    }

    public void Start()
    {
        Stop();
        Loop();
    }

    public void Stop()
    {
        m_Animation?.Stop();
    }

    void Loop()
    {
        m_Animation = experimental.animation.Start(0, s_Wheels.Length, 1000, (ve, f) =>
        {
            var frame = Mathf.FloorToInt(f);
            if (frame >= s_Wheels.Length)
                frame = 0;
            image = s_Wheels[frame].image;
        }).Ease(Easing.Linear).OnCompleted(Loop);
    }
}
