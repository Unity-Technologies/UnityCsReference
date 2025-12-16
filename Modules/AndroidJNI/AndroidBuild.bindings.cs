// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Android
{
    public static class AndroidBuild
    {
        public static class Version
        {
            private static int? m_ApiLevel;
            private static int? m_MinApiLevel;
            private static int? m_TargetApiLevel;

            public static int apiLevel => m_ApiLevel ??= GetApiLevel();
            public static int minApiLevel => m_MinApiLevel ??= GetMinApiLevel();
            public static int targetApiLevel => m_TargetApiLevel ??= GetTargetApiLevel();

            private static int GetApiLevel() => 0;
            private static int GetMinApiLevel() => 0;
            private static int GetTargetApiLevel() => 0;
        }
    }
}
