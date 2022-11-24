// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    /// <summary>
    /// Represents text rendering settings for IMGUI runtime
    /// </summary>
    internal class RuntimeTextSettings : TextSettings
    {
        private static RuntimeTextSettings s_DefaultTextSettings;

        internal static RuntimeTextSettings defaultTextSettings
        {
            get
            {
                if (s_DefaultTextSettings == null)
                {
                    s_DefaultTextSettings = ScriptableObject.CreateInstance<RuntimeTextSettings>();
                }

                return s_DefaultTextSettings;
            }
        }
    }
}
