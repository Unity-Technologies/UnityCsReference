// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace UnityEditor
{
    // Representation of Script assets.
    [NativeClass(null)]
    [NativeType("Editor/Mono/MonoScript.bindings.h")]
    public class MonoScript : TextAsset
    {
        // Returns the System.Type object of the class implemented by this script
        public extern System.Type GetClass();

        // Returns the MonoScript object containing specified MonoBehaviour
        [FreeFunction]
        public extern static MonoScript FromMonoBehaviour(MonoBehaviour behaviour);

        // Returns the MonoScript object containing specified ScriptableObject
        [FreeFunction]
        public extern static MonoScript FromScriptableObject(ScriptableObject scriptableObject);

        internal extern bool GetScriptTypeWasJustCreatedFromComponentMenu();

        internal extern void SetScriptTypeWasJustCreatedFromComponentMenu();

        // *undocumented*
        public MonoScript()
        {
            Init_Internal(this);
        }

        [FreeFunction("MonoScript_Init_Internal")]
        private static extern void Init_Internal([Writable] MonoScript script);

        // *undocumented*
        [NativeName("InitializeAndRegisterScript")]
        internal extern void Init(string scriptContents, string className, string nameSpace, string assemblyName, bool isEditorScript);

        // *undocumented*
        [NativeName("GetNameSpace")]
        internal extern string GetNamespace();
    }
}
