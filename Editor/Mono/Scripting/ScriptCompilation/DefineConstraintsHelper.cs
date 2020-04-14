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

        static Regex s_SplitAndKeep = new Regex("(\\|\\|)", RegexOptions.Compiled);

        public enum DefineConstraintStatus
        {
            Compatible,
            Incompatible,
            Invalid,
        }

        [RequiredByNativeCode]
        public static bool IsDefineConstraintsCompatible(string[] defines, string[] defineConstraints)
        {
            if (defines == null && defineConstraints == null || defineConstraints == null)
            {
                return true;
            }

            bool[] defineConstraintsValidity;
            GetDefineConstraintsCompatibility(defines, defineConstraints, out defineConstraintsValidity);

            return defineConstraintsValidity.All(c => c);
        }

        static void GetDefineConstraintsCompatibility(string[] defines, string[] defineConstraints, out bool[] defineConstraintsValidity)
        {
            defineConstraintsValidity = new bool[defineConstraints.Length];

            for (int i = 0; i < defineConstraints.Length; ++i)
            {
                defineConstraintsValidity[i] = GetDefineConstraintCompatibility(defines, defineConstraints[i]) == DefineConstraintStatus.Compatible;
            }
        }

        internal static DefineConstraintStatus GetDefineConstraintCompatibility(string[] defines, string defineConstraints)
        {
            // Split by "||" (OR) and keep it in the resulting array
            var splitDefines = s_SplitAndKeep.Split(defineConstraints);

            // Trim what we consider valid space characters
            for (var i = 0; i < splitDefines.Length; ++i)
            {
                splitDefines[i] = splitDefines[i].Trim(k_ValidWhitespaces);
            }

            // Check for consecutive OR
            for (var i = 0; i < splitDefines.Length; ++i)
            {
                if (splitDefines[i] == Or && (i < splitDefines.Length - 1 && splitDefines[i + 1] == Or))
                {
                    return DefineConstraintStatus.Invalid;
                }
            }

            var notExpectedDefines = new HashSet<string>(splitDefines.Where(x => x.StartsWith(Not, StringComparison.Ordinal) && x != Or).Select(x => x.Substring(1)));
            var expectedDefines = new HashSet<string>(splitDefines.Where(x => !x.StartsWith(Not, StringComparison.Ordinal) && x != Or));

            if (defines == null)
            {
                if (expectedDefines.Count > 0)
                {
                    return DefineConstraintStatus.Incompatible;
                }

                return DefineConstraintStatus.Compatible;
            }

            foreach (var define in expectedDefines)
            {
                if (!SymbolNameRestrictions.IsValid(define))
                {
                    return DefineConstraintStatus.Invalid;
                }
            }

            foreach (var define in notExpectedDefines)
            {
                if (!SymbolNameRestrictions.IsValid(define))
                {
                    return DefineConstraintStatus.Invalid;
                }
            }

            if (expectedDefines.Overlaps(notExpectedDefines))
            {
                var complement = new HashSet<string>(expectedDefines);
                expectedDefines.ExceptWith(notExpectedDefines);
                notExpectedDefines.ExceptWith(complement);
            }

            var expectedDefinesResult = DefineConstraintStatus.Incompatible;
            foreach (var define in expectedDefines)
            {
                if (defines.Contains(define))
                {
                    expectedDefinesResult = DefineConstraintStatus.Compatible;
                    break;
                }
            }

            if (expectedDefines.Count > 0 && notExpectedDefines.Count == 0)
            {
                return expectedDefinesResult;
            }

            var notExpectedDefinesResult = DefineConstraintStatus.Compatible;
            foreach (var define in notExpectedDefines)
            {
                if (defines.Contains(define))
                {
                    notExpectedDefinesResult = DefineConstraintStatus.Incompatible;
                    break;
                }
            }

            if (notExpectedDefines.Count > 0 && expectedDefines.Count == 0)
            {
                return notExpectedDefinesResult;
            }

            if (expectedDefinesResult == DefineConstraintStatus.Compatible || notExpectedDefinesResult == DefineConstraintStatus.Compatible)
            {
                return DefineConstraintStatus.Compatible;
            }

            return DefineConstraintStatus.Incompatible;
        }

        internal static bool IsDefineConstraintValid(string define)
        {
            if (define == null)
            {
                return false;
            }

            // Split define by OR symbol
            var splitDefines = define.Split(new[] { Or }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var d in splitDefines)
            {
                var finalDefine = (d.StartsWith(Not, StringComparison.Ordinal) ? d.Substring(1) : d).Trim();
                if (!SymbolNameRestrictions.IsValid(finalDefine))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
