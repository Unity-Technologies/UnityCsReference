// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEditor.Macros
{
    public static class MacroEvaluator
    {
        [RequiredByNativeCode]
        public static string Eval(string macro)
        {
            var ret = MethodEvaluator.ExecuteExternalCode(macro);
            return ret == null ? "Null" : ret.ToString();
        }
    }
}
