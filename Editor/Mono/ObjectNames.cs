// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class ObjectNames
    {
        static class InspectorTitles
        {
            static readonly Dictionary<Type, string> s_InspectorTitles;

            static InspectorTitles()
            {
                var addComponentMenuTypes = TypeCache.GetTypesWithAttribute<AddComponentMenu>();

                s_InspectorTitles = new Dictionary<Type, string>(addComponentMenuTypes.Count);

                foreach (var type in addComponentMenuTypes)
                {
                    var attr = type.GetCustomAttributes(typeof(AddComponentMenu), false).FirstOrDefault()
                        as AddComponentMenu;
                    if (attr == null)
                        continue;
                    var title = attr.componentMenu?.Trim();
                    if (string.IsNullOrEmpty(title))
                        continue;
                    var lastPathCharIndex = title.LastIndexOf('/');
                    if (lastPathCharIndex >= 0)
                        if (lastPathCharIndex < title.Length - 1)
                            title = title.Substring(lastPathCharIndex + 1);
                        else
                            continue;

                    s_InspectorTitles[type] = title;
                }
            }

            public static bool TryGet(Type objectType, out string title)
            {
                return s_InspectorTitles.TryGetValue(objectType, out title);
            }
        }

        // Make a displayable name for a variable.
        public static string NicifyVariableName(string name) => NameFormatter.FormatVariableName(name);

        private static string GetObjectTypeName([NotNull] Object o, bool multiObjectEditing = false)
        {
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
                        return behaviour.name + L10n.Tr(" (Script)");
                    return scriptClassName + L10n.Tr(" (Script)");
                }

                var meshfilter = o as MeshFilter;
                if (meshfilter)
                {
                    if (multiObjectEditing)
                        return "MeshFilter";

                    var mesh = meshfilter.sharedMesh;
                    return (mesh ? mesh.name : L10n.Tr("[none]")) + " (MeshFilter)";
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
                        return script.GetClass().Name + L10n.Tr(" (Script)");
                }

                return o.GetType().Name;
            }

            return o.name + " (" + o.GetType().Name + ")";
        }

        public static string GetInspectorTitle(Object obj, bool multiObjectEditing)
        {
            if (obj == null && (object)obj != null && (obj is MonoBehaviour || obj is ScriptableObject))
                return L10n.Tr(" (Script)");

            if (obj == null)
                return L10n.Tr("Nothing Selected");

            string title;
            if (!InspectorTitles.TryGet(obj.GetType(), out title))
                title = NicifyVariableName(GetObjectTypeName(obj, multiObjectEditing));

            if (Attribute.IsDefined(obj.GetType(), typeof(ObsoleteAttribute)))
                title += L10n.Tr(" (Deprecated)");

            return title;
        }

        // Inspector title for an object.
        public static string GetInspectorTitle(Object obj)
        {
            return GetInspectorTitle(obj, false);
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
            return obj.GetType().Name;
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

        internal static string CapitaliseFirstLetter(string srt) => $"{char.ToUpper(srt[0], CultureInfo.InvariantCulture).ToString()}{srt.Substring(1)}";
    }
}
