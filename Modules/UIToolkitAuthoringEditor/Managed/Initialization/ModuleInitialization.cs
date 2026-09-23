// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal static class ModuleInitialization
    {
        [RequiredByNativeCode]
        public static void Initialize()
        {
            VisualElementAnimationAuthoringHandler.Register();
        }
    }
}
