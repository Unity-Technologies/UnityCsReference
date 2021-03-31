// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    interface IVersionDefinesConsoleLogs
    {
        void LogVersionDefineError(TargetAssembly targetAssembly, ExpressionNotValidException validationError);
        void ClearVersionDefineErrors();
    }

    [NativeHeader("Editor/Src/ScriptCompilation/VersionDefinesConsoleLogs.h")]
    class VersionDefinesConsoleLogs : IVersionDefinesConsoleLogs
    {
        public void LogVersionDefineError(TargetAssembly targetAssembly, ExpressionNotValidException validationError)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(EditorCompilationInterface.Instance.FindCustomTargetAssemblyFromTargetAssembly(targetAssembly).FilePath);
            var instanceID = asset.GetInstanceID();
            InternalLogVersionDefineError(validationError, instanceID);
        }

        public void ClearVersionDefineErrors()
        {
            InternalClearVersionDefineErrors();
        }

        [FreeFunction(nameof(InternalLogVersionDefineError))]
        static extern void InternalLogVersionDefineError(Exception ex, int assetInstanceID);


        [FreeFunction(nameof(InternalClearVersionDefineErrors))]
        static extern void InternalClearVersionDefineErrors();
    }
}
