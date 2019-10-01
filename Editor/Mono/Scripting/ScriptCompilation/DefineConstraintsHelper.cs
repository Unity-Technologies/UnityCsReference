// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class DefineConstraintsHelper
    {
        public const string Not = "!";
        public const string Or = "||";

        public static readonly char[] k_ValidWhitespaces = { ' ', '\t' };

        [RequiredByNativeCode]
        public static bool IsDefineConstraintsCompatible(string[] defines, string[] defineConstraints)
        {
            if (defines == null && defineConstraints == null || defineConstraints == null)
            {
                return true;
            }

            bool[] defineConstraintsValidity;
            GetDefineConstraintsValidity(defines, defineConstraints, out defineConstraintsValidity);

            return defineConstraintsValidity.All(c => c);
        }

        static void GetDefineConstraintsValidity(string[] defines, string[] defineConstraints, out bool[] defineConstraintsValidity)
        {
            defineConstraintsValidity = new bool[defineConstraints.Length];

            for (int i = 0; i < defineConstraints.Length; ++i)
            {
                defineConstraintsValidity[i] = IsDefineConstraintValid(defines, defineConstraints[i]);
            }
        }

        internal static bool IsDefineConstraintValid(string[] defines, string defineConstraints)
        {
            // Split by "||" (OR) and keep it in the resulting array
            var splitDefines = Regex.Split(defineConstraints, "(\\|\\|)");

            // Trim what we consider valid space characters
            for (var i = 0; i < splitDefines.Length; ++i)
            {
                splitDefines[i] = splitDefines[i].Trim(k_ValidWhitespaces);
            }

            // Check for consecutive Or
            for (var i = 0; i < splitDefines.Length; ++i)
            {
                if (splitDefines[i] == Or && (i < splitDefines.Length - 1 && splitDefines[i + 1] == Or))
                {
                    return false;
                }
            }

            var notExpectedDefines = new HashSet<string>(splitDefines.Where(x => x.StartsWith(Not) && x != Or).Select(x => x.Substring(1)));
            var expectedDefines = new HashSet<string>(splitDefines.Where(x => !x.StartsWith(Not) && x != Or));

            if (defines == null)
            {
                if (expectedDefines.Count > 0)
                {
                    return false;
                }

                return true;
            }

            foreach (var define in expectedDefines)
            {
                if (!SymbolNameRestrictions.IsValid(define))
                {
                    return false;
                }
            }

            foreach (var define in notExpectedDefines)
            {
                if (!SymbolNameRestrictions.IsValid(define))
                {
                    return false;
                }
            }

            if (expectedDefines.Overlaps(notExpectedDefines))
            {
                var complement = new HashSet<string>(expectedDefines);
                expectedDefines.ExceptWith(notExpectedDefines);
                notExpectedDefines.ExceptWith(complement);
            }

            if (notExpectedDefines.Count > 0 && expectedDefines.Count == 0)
            {
                return !notExpectedDefines.Any(defines.Contains);
            }

            if (expectedDefines.Count > 0 && notExpectedDefines.Count == 0)
            {
                return expectedDefines.Any(defines.Contains);
            }

            return expectedDefines.Any(defines.Contains) || !notExpectedDefines.Any(defines.Contains);
        }
    }
}
