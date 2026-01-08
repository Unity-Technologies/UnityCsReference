// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class InitialStyle
    {
        private unsafe static void CopyDataToNative()
        {
            NativeTransformUtils.SetInitialStyleTransformData((IntPtr)s_InitialStyle.transformData.GetValuePtr());
        }
    }
}
