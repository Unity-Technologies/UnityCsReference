// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace UnityEditor;

[CustomEditor(typeof(AudioRandomContainer))]
[CanEditMultipleObjects]
sealed class AudioRandomContainerInspector : Editor
{
    private StyleLength margin = 5;

    private Button button;

    public override VisualElement CreateInspectorGUI()
    {
        button = new Button();

        button.text = "Edit Audio Random Container";
        button.style.marginBottom = margin;
        button.style.marginTop = margin;
        button.style.marginLeft = 0;
        button.style.marginRight = 0;
        button.style.height = 24;
        button.clicked += () => { EditorWindow.GetWindow<AudioContainerWindow>(); };

        return button;
    }
}
