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
    [ExcludeFromPreset]
    public class MonoScript : TextAsset
    {
        // Returns the System.Type object of the class implemented by this script
        public extern System.Type GetClass();

        // Returns the MonoScript object containing specified MonoBehaviour
        public static MonoScript FromMonoBehaviour(MonoBehaviour behaviour)
        {
            return FromScriptedObject(behaviour);
        }

        // Returns the MonoScript object containing specified ScriptableObject
        public static MonoScript FromScriptableObject(ScriptableObject scriptableObject)
        {
            return FromScriptedObject(scriptableObject);
        }

        // Returns the MonoScript object used by the given scripted object
        [FreeFunction]
        internal static extern MonoScript FromScriptedObject(UnityEngine.Object obj);

        internal extern bool GetScriptTypeWasJustCreatedFromComponentMenu();

        internal extern void SetScriptTypeWasJustCreatedFromComponentMenu();

        // *undocumented*
        // Pass CreateOptions.None to TextAsset constructor so it does not create a native TextAsset object.
        // We create MonoScript native object instead.
        public MonoScript() : base(TextAsset.CreateOptions.None, null)
        {
            Init_Internal(this);
        }

        internal void Init(string scriptContents, string className, string nameSpace, string assemblyName, bool isEditorScript)
        {
            Init(this, scriptContents, className, nameSpace, assemblyName, isEditorScript);
        }

        [FreeFunction("MonoScript_Init_Internal")]
        private static extern void Init_Internal([Writable] MonoScript script);

        // *undocumented*
        [FreeFunction("MonoScript_Init")]
        private static extern void Init(MonoScript self, string scriptContents, string className, string nameSpace, string assemblyName, bool isEditorScript);

        // *undocumented*
        [NativeName("GetNameSpace")]
        internal extern string GetNamespace();
    }
}
