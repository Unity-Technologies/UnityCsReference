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
    public override VisualElement CreateInspectorGUI()
    {
        return new VisualElement();
    }
}
