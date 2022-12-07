// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor;

internal class GraphicsSettingsInspectorBuiltinShaderLabel : GraphicsSettingsElement
{
    public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorBuiltinShaderLabel, UxmlTraits> { }

    internal class Styles
    {
        public static readonly GUIContent builtinSettings = EditorGUIUtility.TrTextContent("Built-in Shader Settings");
    }

    protected override void Initialize()
    {
        Add(new Label(Styles.builtinSettings.text));
    }
}
