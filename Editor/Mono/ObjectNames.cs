// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class ObjectNames
    {
        // *undocumented*
        private static string GetObjectTypeName(Object o)
        {
            if (o == null)
                return "Nothing Selected";

            if (o is GameObject)
                return o.name;

            // Show "Tags & Layers" instead of "TagManager"
            if (o is TagManager)
                return "Tags & Layers";

            if (o is Component)
            {
                var behaviour = o as MonoBehaviour;
                if (behaviour)
                {
                    var scriptClassName = behaviour.GetScriptClassName();
                    if (scriptClassName == "InvalidStateMachineBehaviour")
                        return behaviour.name + " (Script)";
                    return scriptClassName + " (Script)";
                }

                var meshfilter = o as MeshFilter;
                if (meshfilter)
                {
                    var mesh = meshfilter.sharedMesh;
                    return (mesh ? mesh.name : "[none]") + " (MeshFilter)";
                }

                return o.GetType().Name;
            }

            // Importers don't have names. Just show the type without parenthesis (like we do for components).
            if (o is AssetImporter)
            {
                var monoImporter = o as MonoImporter;
                if (monoImporter)
                {
                    var script = monoImporter.GetScript();
                    return "Default References (" + (script ? script.name : string.Empty) + ")";
                }

                if (NativeClassExtensionUtilities.ExtendsANativeType(o))
                {
                    var script = MonoScript.FromScriptedObject(o);
                    if (script != null)
                        return script.GetClass().Name + " (Script)";
                }

                return o.GetType().Name;
            }

            return o.name + " (" + o.GetType().Name + ")";
        }

        // Inspector title for an object.
        public static string GetInspectorTitle(Object obj)
        {
            if (obj == null && (object)obj != null && (obj is MonoBehaviour || obj is ScriptableObject))
                return " (Script)";

            if (obj == null)
                return "Nothing Selected";

            var title = ObjectNames.NicifyVariableName(GetObjectTypeName(obj));

            if (Attribute.IsDefined(obj.GetType(), typeof(ObsoleteAttribute)))
                title += " (Deprecated)";

            return title;
        }

        // Like GetClassName but handles folders, scenes, GUISkins, and other default assets as separate types.
        internal static string GetTypeName(Object obj)
        {
            // Return "Object" when null so we have a icon in the inspector(null does not have an icon). case 707513.
            if (obj == null)
                return "Object";

            string pathLower = AssetDatabase.GetAssetPath(obj).ToLower();
            if (pathLower.EndsWith(".unity"))
                return "Scene";
            else if (pathLower.EndsWith(".guiskin"))
                return "GUI Skin";
            else if (System.IO.Directory.Exists(AssetDatabase.GetAssetPath(obj)))
                return "Folder";
            else if (obj.GetType() == typeof(Object))
                return System.IO.Path.GetExtension(pathLower) + " File";
            return ObjectNames.GetClassName(obj);
        }

        [Obsolete("Please use NicifyVariableName instead")]
        public static string MangleVariableName(string name)
        {
            return NicifyVariableName(name);
        }

        [Obsolete("Please use GetInspectorTitle instead")]
        public static string GetPropertyEditorTitle(Object obj)
        {
            return GetInspectorTitle(obj);
        }
    }
}
