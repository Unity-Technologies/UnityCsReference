// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a style sheet that's assembled from other style sheets.
    /// </summary>
    [HelpURL("UIE-tss")]
    [Serializable]
    public class ThemeStyleSheet : StyleSheet
    {
        internal override void OnEnable()
        {
            isDefaultStyleSheet = true;
            base.OnEnable();
        }
    }
}
