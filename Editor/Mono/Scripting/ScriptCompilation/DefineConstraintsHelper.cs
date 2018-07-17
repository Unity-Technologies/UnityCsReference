// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class DefineConstraintsHelper
    {
        public const string Not = "!";

        [RequiredByNativeCode]
        public static bool IsDefineConstraintsCompatible(string[] defines, string[] defineConstraints)
        {
            var expectedDefines = defineConstraints?.Where(x => !x.StartsWith(Not)).ToList();
            if ((defines == null || !defines.Any()) && (expectedDefines == null || !expectedDefines.Any()))
            {
                return true;
            }

            if (defineConstraints == null)
            {
                return true;
            }

            if (defines == null)
            {
                return false;
            }

            var notExpectedDefines = defineConstraints.Where(x => x.StartsWith(Not)).Select(x => x.Substring(1)).ToList();
            if (!expectedDefines.All(defines.Contains) || notExpectedDefines.Any(defines.Contains))
            {
                return false;
            }
            return true;
        }
    }
}
