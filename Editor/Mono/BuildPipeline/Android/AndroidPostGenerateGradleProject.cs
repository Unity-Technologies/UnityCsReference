// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace UnityEditor.Android
{
    public interface IPostGenerateGradleAndroidProject : IOrderedCallback
    {
        [Obsolete("OnPostGenerateGradleAndroidProject is deprecated. Use AndroidProjectFilesModifier.OnModifyAndroidProjectFiles instead.")]
        void OnPostGenerateGradleAndroidProject(string path);
    }
}
