// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace UnityEditorInternal
{
    partial class InternalEditorUtility
    {
        public static Texture2D FindIconForFile(string fileName)
        {
            int i = fileName.LastIndexOf('.');
            string extension = i == -1 ? "" : fileName.Substring(i + 1).ToLower();

            switch (extension)
            {
                // Most .asset files use their scriptable object defined icon instead of a default one.
                case "asset": return AssetDatabase.GetCachedIcon(fileName) as Texture2D ?? EditorGUIUtility.FindTexture("GameManager Icon");

                case "cginc": return EditorGUIUtility.FindTexture("CGProgram Icon");
                case "cs": return EditorGUIUtility.FindTexture("cs Script Icon");
                case "guiskin": return EditorGUIUtility.FindTexture(typeof(GUISkin));
                case "dll": return EditorGUIUtility.FindTexture("Assembly Icon");
                case "asmdef": return EditorGUIUtility.FindTexture(typeof(AssemblyDefinitionAsset));
                case "asmref": return EditorGUIUtility.FindTexture(typeof(AssemblyDefinitionAsset));
                case "mat": return EditorGUIUtility.FindTexture(typeof(Material));
                case "physicmaterial": return EditorGUIUtility.FindTexture(typeof(PhysicMaterial));
                case "prefab": return EditorGUIUtility.FindTexture("Prefab Icon");
                case "shader": return EditorGUIUtility.FindTexture(typeof(Shader));
                case "txt": return EditorGUIUtility.FindTexture(typeof(TextAsset));
                case "unity": return EditorGUIUtility.FindTexture(typeof(SceneAsset));
                case "prefs": return EditorGUIUtility.FindTexture(typeof(EditorSettings));
                case "anim": return EditorGUIUtility.FindTexture(typeof(Animation));
                case "meta": return EditorGUIUtility.FindTexture("MetaFile Icon");
                case "mixer": return EditorGUIUtility.FindTexture(typeof(UnityEditor.Audio.AudioMixerController));
                case "uxml": return EditorGUIUtility.FindTexture(typeof(UnityEngine.UIElements.VisualTreeAsset));
                case "uss": return EditorGUIUtility.FindTexture(typeof(StyleSheet));
                case "lighting": return EditorGUIUtility.FindTexture(typeof(UnityEngine.LightingSettings));

                case "ttf": case "otf": case "fon": case "fnt":
                    return EditorGUIUtility.FindTexture(typeof(Font));

                case "aac": case "aif": case "aiff": case "au": case "mid": case "midi": case "mp3": case "mpa":
                case "ra": case "ram": case "wma": case "wav": case "wave": case "ogg": case "flac":
                    return EditorGUIUtility.FindTexture(typeof(AudioClip));

                case "ai": case "apng": case "png": case "bmp": case "cdr": case "dib": case "eps": case "exif":
                case "gif": case "ico": case "icon": case "j": case "j2c": case "j2k": case "jas":
                case "jiff": case "jng": case "jp2": case "jpc": case "jpe": case "jpeg": case "jpf": case "jpg":
                case "jpw": case "jpx": case "jtf": case "mac": case "omf": case "qif": case "qti": case "qtif":
                case "tex": case "tfw": case "tga": case "tif": case "tiff": case "wmf": case "psd": case "exr":
                case "hdr":
                    return EditorGUIUtility.FindTexture(typeof(Texture));

                case "3df": case "3dm": case "3dmf": case "3ds": case "3dv": case "3dx": case "blend": case "c4d":
                case "lwo": case "lws": case "ma": case "max": case "mb": case "mesh": case "obj": case "vrl":
                case "wrl": case "wrz": case "fbx":
                    return EditorGUIUtility.FindTexture(typeof(Mesh));

                case "dv": case "mp4": case "mpg": case "mpeg": case "m4v": case "ogv": case "vp8": case "webm":
                case "asf": case "asx": case "avi": case "dat": case "divx": case "dvx": case "mlv": case "m2l":
                case "m2t": case "m2ts": case "m2v": case "m4e": case "mjp": case "mov": case "movie":
                case "mp21": case "mpe": case "mpv2": case "ogm": case "qt": case "rm": case "rmvb": case "wmw": case "xvid":
                    return AssetDatabase.GetCachedIcon(fileName) as Texture2D ?? EditorGUIUtility.FindTexture(typeof(VideoClip));

                case "colors": case "gradients":
                case "curves": case "curvesnormalized": case "particlecurves": case "particlecurvessigned": case "particledoublecurves": case "particledoublecurvessigned":
                    return EditorGUIUtility.FindTexture(typeof(ScriptableObject));

                default: return null;
            }
        }

        public static Texture2D GetIconForFile(string fileName)
        {
            return FindIconForFile(fileName) ?? EditorGUIUtility.FindTexture(typeof(DefaultAsset));
        }

        static GUIContent[] sStatusWheel;

        internal static GUIContent animatedProgressImage
        {
            get
            {
                if (sStatusWheel == null)
                {
                    sStatusWheel = new GUIContent[12];
                    for (int i = 0; i < 12; i++)
                    {
                        GUIContent gc = new GUIContent();
                        gc.image = EditorGUIUtility.LoadIcon("WaitSpin" + i.ToString("00")) as Texture2D;
                        gc.image.hideFlags = HideFlags.HideAndDontSave;
                        gc.image.name = "Spinner";
                        sStatusWheel[i] = gc;
                    }
                }
                int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                return sStatusWheel[frame];
            }
        }

        public static string[] GetEditorSettingsList(string prefix, int count)
        {
            ArrayList aList = new ArrayList();

            for (int i = 1; i <= count; i++)
            {
                string str = EditorPrefs.GetString(prefix + i, "defaultValue");

                if (str == "defaultValue")
                    break;

                aList.Add(str);
            }

            return aList.ToArray(typeof(string)) as string[];
        }

        public static void SaveEditorSettingsList(string prefix, string[] aList, int count)
        {
            int i;

            for (i = 0; i < aList.Length; i++)
                EditorPrefs.SetString(prefix + (i + 1), (string)aList[i]);

            for (i = aList.Length + 1; i <= count; i++)
                EditorPrefs.DeleteKey(prefix + i);
        }

        public static string TextAreaForDocBrowser(Rect position, string text, GUIStyle style)
        {
            int id = EditorGUIUtility.GetControlID("TextAreaWithTabHandling".GetHashCode(), FocusType.Keyboard, position);
            var editor = EditorGUI.s_RecycledEditor;
            var evt = Event.current;
            if (editor.IsEditingControl(id) && evt.type == EventType.KeyDown)
            {
                if (evt.character == '\t')
                {
                    editor.Insert('\t');
                    evt.Use();
                    GUI.changed = true;
                    text = editor.text;
                }
                if (evt.character == '\n')
                {
                    editor.Insert('\n');
                    evt.Use();
                    GUI.changed = true;
                    text = editor.text;
                }
            }
            bool dummy;
            text = EditorGUI.DoTextField(editor, id, EditorGUI.IndentedRect(position), text, style, null, out dummy, false, true, false);
            return text;
        }

        public static Camera[] GetSceneViewCameras()
        {
            return SceneView.GetAllSceneCameras();
        }

        public static void ShowGameView()
        {
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymode(true);
        }

        internal struct AssetReference
        {
            // The guid is always valid. Assets not yet present will not have an instanceID yet.
            // Could be because it is on-demand imported.
            public string guid;
            public int instanceID; // instanceID of an object in an asset if the asset is available in imported form. Else 0.

            public sealed class GuidThenInstanceIDEqualityComparer : IEqualityComparer<AssetReference>
            {
                public bool Equals(AssetReference x, AssetReference y)
                {
                    if (!string.IsNullOrEmpty(x.guid) || !string.IsNullOrEmpty(y.guid))
                        return string.Equals(x.guid, y.guid);

                    // Both guids are nullOrEmpty now
                    return x.instanceID == y.instanceID;
                }

                public int GetHashCode(AssetReference assetReference)
                {
                    return (assetReference.instanceID * 397)
                        ^ (assetReference.guid != null ? assetReference.guid.GetHashCode() : 0);
                }
            }
        }

        public static List<int> GetNewSelection(int clickedInstanceID, List<int> allInstanceIDs, List<int> selectedInstanceIDs, int lastClickedInstanceID, bool keepMultiSelection, bool useShiftAsActionKey, bool allowMultiSelection)
        {
            return GetNewSelection(clickedInstanceID, allInstanceIDs, selectedInstanceIDs, lastClickedInstanceID, keepMultiSelection, useShiftAsActionKey, allowMultiSelection, Event.current.shift, EditorGUI.actionKey);
        }

        internal static List<int> GetNewSelection(int clickedInstanceID, List<int> allInstanceIDs, List<int> selectedInstanceIDs, int lastClickedInstanceID, bool keepMultiSelection, bool useShiftAsActionKey, bool allowMultiSelection, bool shiftKeyIsDown, bool actionKeyIsDown)
        {
            List<string> allGuids = null;
            var clicked = new AssetReference() { guid = null, instanceID = clickedInstanceID };

            return GetNewSelection(ref clicked, allInstanceIDs, allGuids, selectedInstanceIDs, lastClickedInstanceID, keepMultiSelection, useShiftAsActionKey, allowMultiSelection, shiftKeyIsDown, actionKeyIsDown);
        }

        internal static bool TrySetInstanceId(ref AssetReference entry)
        {
            if (entry.instanceID != 0 || string.IsNullOrEmpty(entry.guid))
                return true;

            GUID lookupGUID = new GUID(entry.guid);
            var hash = UnityEditor.Experimental.AssetDatabaseExperimental.ProduceArtifactAsync(new ArtifactKey(lookupGUID));
            if (!hash.isValid)
                return false;

            string path = AssetDatabase.GUIDToAssetPath(entry.guid);
            entry.instanceID = AssetDatabase.GetMainAssetInstanceID(path);
            return true;
        }

        internal static List<int> TryGetInstanceIds(List<int> entryInstanceIds, List<string> entryInstanceGuids, int from, int to)
        {
            List<GUID> guids = new List<GUID>();

            for (int i = from; i <= to; ++i)
            {
                if (entryInstanceIds[i] == 0)
                {
                    GUID parsedGuid = new GUID(entryInstanceGuids[i]);
                    guids.Add(parsedGuid);
                }
            }

            // Force import if needed so that we can get an instance ID for the entry

            if (guids.Count == 0)
            {
                return entryInstanceIds.GetRange(from, to - from + 1);
            }
            else
            {
                var hashes = UnityEditor.Experimental.AssetDatabaseExperimental.ProduceArtifactsAsync(guids.ToArray());
                if (System.Array.FindIndex(hashes, a => !a.isValid) != -1)
                    return null;

                var newSelection = new List<int>(to - from + 1);

                for (int i = from; i <= to; ++i)
                {
                    int instanceID = entryInstanceIds[i];
                    if (instanceID == 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(entryInstanceGuids[i]);
                        instanceID = AssetDatabase.GetMainAssetInstanceID(path);
                        entryInstanceIds[i] = instanceID;
                    }
                    newSelection.Add(instanceID);
                }
                return newSelection;
            }
        }

        internal static List<int> GetNewSelection(ref AssetReference clickedEntry, List<int> allEntryInstanceIDs, List<string> allEntryGuids, List<int> selectedInstanceIDs, int lastClickedInstanceID, bool keepMultiSelection, bool useShiftAsActionKey, bool allowMultiSelection)
        {
            return GetNewSelection(ref clickedEntry, allEntryInstanceIDs, allEntryGuids, selectedInstanceIDs, lastClickedInstanceID, keepMultiSelection, useShiftAsActionKey, allowMultiSelection, Event.current.shift, EditorGUI.actionKey);
        }

        // Multi selection handling. Returns new list of selected instanceIDs
        internal static List<int> GetNewSelection(ref AssetReference clickedEntry, List<int> allEntryInstanceIDs, List<string> allEntryGuids, List<int> selectedInstanceIDs, int lastClickedInstanceID, bool keepMultiSelection, bool useShiftAsActionKey, bool allowMultiSelection, bool shiftKeyIsDown, bool actionKeyIsDown)
        {
            bool useShift = shiftKeyIsDown || (actionKeyIsDown && useShiftAsActionKey);
            bool useActionKey = actionKeyIsDown && !useShiftAsActionKey;
            if (!allowMultiSelection)
                useShift = useActionKey = false;

            // Toggle selected node from selection
            if (useActionKey)
            {
                var newSelection = new List<int>(selectedInstanceIDs);
                if (newSelection.Contains(clickedEntry.instanceID))
                    newSelection.Remove(clickedEntry.instanceID);
                else
                {
                    if (TrySetInstanceId(ref clickedEntry))
                        newSelection.Add(clickedEntry.instanceID);
                }
                return newSelection;
            }
            // Select everything between the first selected object and the selected
            else if (useShift)
            {
                if (clickedEntry.instanceID == lastClickedInstanceID)
                {
                    return new List<int>(selectedInstanceIDs);
                }

                int firstIndex;
                int lastIndex;
                if (!GetFirstAndLastSelected(allEntryInstanceIDs, selectedInstanceIDs, out firstIndex, out lastIndex))
                {
                    // We had no selection
                    var newSelection = new List<int>(1);
                    if (TrySetInstanceId(ref clickedEntry))
                        newSelection.Add(clickedEntry.instanceID);

                    return newSelection;
                }

                int newIndex = -1;
                int prevIndex = -1;

                // Only valid in case the selection concerns assets

                if (!TrySetInstanceId(ref clickedEntry))
                    return new List<int>(selectedInstanceIDs);

                int clickedInstanceID = clickedEntry.instanceID;

                if (lastClickedInstanceID != 0)
                {
                    for (int i = 0; i < allEntryInstanceIDs.Count; ++i)
                    {
                        if (allEntryInstanceIDs[i] == clickedInstanceID)
                            newIndex = i;

                        if (allEntryInstanceIDs[i] == lastClickedInstanceID)
                            prevIndex = i;
                    }
                }
                else
                {
                    for (int i = 0; i < allEntryInstanceIDs.Count; ++i)
                    {
                        if (allEntryInstanceIDs[i] == clickedInstanceID)
                            newIndex = i;
                    }
                }

                System.Diagnostics.Debug.Assert(newIndex != -1); // new item should be part of visible folder set
                int dir = 0;
                if (prevIndex != -1)
                    dir = (newIndex > prevIndex) ? 1 : -1;

                int from, to;
                if (newIndex > lastIndex)
                {
                    from = firstIndex;
                    to = newIndex;
                }
                else if (newIndex >= firstIndex && newIndex < lastIndex)
                {
                    if (dir > 0)
                    {
                        from = newIndex;
                        to = lastIndex;
                    }
                    else
                    {
                        from = firstIndex;
                        to = newIndex;
                    }
                }
                else
                {
                    from = newIndex;
                    to = lastIndex;
                }

                // Outcomment to debug
                //Debug.Log (clickedEntry + ",   firstIndex " + firstIndex + ", lastIndex " + lastIndex + ",    newIndex " + newIndex + " " + ", lastClickedIndex " + prevIndex + ",     from " + from + ", to " + to);
                if (allEntryGuids == null)
                    return allEntryInstanceIDs.GetRange(from, to - from + 1);

                var foundInstanceIDs = TryGetInstanceIds(allEntryInstanceIDs, allEntryGuids, from, to);
                if (foundInstanceIDs != null)
                    return foundInstanceIDs;

                return new List<int>(selectedInstanceIDs);
            }
            // Just set the selection to the clicked object
            else
            {
                if (keepMultiSelection)
                {
                    // Don't change selection on mouse down when clicking on selected item.
                    // This is for dragging in case with multiple items selected or right click (mouse down should not unselect the rest).
                    if (selectedInstanceIDs.Contains(clickedEntry.instanceID))
                    {
                        return new List<int>(selectedInstanceIDs);
                    }
                }

                if (TrySetInstanceId(ref clickedEntry))
                {
                    var newSelection = new List<int>(1);
                    newSelection.Add(clickedEntry.instanceID);
                    return newSelection;
                }
                else
                {
                    return new List<int>(selectedInstanceIDs);
                }
            }
        }

        static bool GetFirstAndLastSelected(List<int> allEntries, List<int> selectedInstanceIDs, out int firstIndex, out int lastIndex)
        {
            firstIndex = -1;
            lastIndex = -1;
            for (int i = 0; i < allEntries.Count; ++i)
            {
                if (selectedInstanceIDs.Contains(allEntries[i]))
                {
                    if (firstIndex == -1)
                        firstIndex = i;
                    lastIndex = i; // just overwrite and we will have the last in the end...
                }
            }
            return firstIndex != -1 && lastIndex != -1;
        }

        internal static string GetApplicationExtensionForRuntimePlatform(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.OSXEditor:
                    return "app";
                case RuntimePlatform.WindowsEditor:
                    return "exe";
                default:
                    break;
            }
            return string.Empty;
        }

        public static bool IsValidFileName(string filename)
        {
            string validFileName = RemoveInvalidCharsFromFileName(filename, false);
            if (validFileName != filename || string.IsNullOrEmpty(validFileName))
                return false;
            return true;
        }

        public static string RemoveInvalidCharsFromFileName(string filename, bool logIfInvalidChars)
        {
            if (string.IsNullOrEmpty(filename))
                return filename;

            filename = filename.Trim(); // remove leading and trailing white spaces
            if (string.IsNullOrEmpty(filename))
                return filename;

            string invalidChars = new string(System.IO.Path.GetInvalidFileNameChars());
            string legal = "";
            bool hasInvalidChar = false;
            foreach (char c in filename)
            {
                if (invalidChars.IndexOf(c) == -1)
                    legal += c;
                else
                    hasInvalidChar = true;
            }
            if (hasInvalidChar && logIfInvalidChars)
            {
                string invalid = GetDisplayStringOfInvalidCharsOfFileName(filename);
                if (invalid.Length > 0)
                    Debug.LogWarningFormat("A filename cannot contain the following character{0}:  {1}", invalid.Length > 1 ? "s" : "", invalid);
            }

            return legal;
        }

        public static string GetDisplayStringOfInvalidCharsOfFileName(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "";

            string invalid = new string(System.IO.Path.GetInvalidFileNameChars());

            string illegal = "";
            foreach (char c in filename)
            {
                if (invalid.IndexOf(c) >= 0)
                {
                    if (illegal.IndexOf(c) == -1)
                    {
                        if (illegal.Length > 0)
                            illegal += " ";
                        illegal += c;
                    }
                }
            }
            return illegal;
        }

        internal static bool IsScriptOrAssembly(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;

            switch (System.IO.Path.GetExtension(filename).ToLower())
            {
                case ".cs":
                case ".js":
                case ".boo":
                    return true;
                case ".dll":
                case ".exe":
                    return AssemblyHelper.IsManagedAssembly(filename);
                default:
                    return false;
            }
        }

        internal static T ParentHasComponent<T>(Transform trans) where T : Component
        {
            if (trans != null)
            {
                T comp = trans.GetComponent<T>();
                if (comp)
                    return comp;

                return ParentHasComponent<T>(trans.parent);
            }
            return null;
        }

        internal static IEnumerable<string> GetAllScriptGUIDs()
        {
            return AssetDatabase.GetAllAssetPaths()
                .Where(asset => (IsScriptOrAssembly(asset) && !UnityEditor.PackageManager.Folders.IsPackagedAssetPath(asset)))
                .Select(asset => AssetDatabase.AssetPathToGUID(asset));
        }

        // Do not remove. Called through reflection by Visual Studio Tools for Unity.
        internal static UnityEditor.Scripting.MonoIsland[] GetMonoIslandsForPlayer()
        {
            var group = EditorUserBuildSettings.activeBuildTargetGroup;
            var target = EditorUserBuildSettings.activeBuildTarget;

            PrecompiledAssembly[] unityAssemblies = GetUnityAssemblies(false, group, target);

            var allPrecompiledAssemblies = EditorCompilationInterface.Instance.PrecompiledAssemblyProvider.GetPrecompiledAssembliesDictionary(false, group, target);;
            return EditorCompilationInterface.Instance.GetAllMonoIslands(unityAssemblies, allPrecompiledAssemblies, EditorScriptCompilationOptions.BuildingEmpty | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies);
        }

        // Do not remove. Called through reflection by Visual Studio Tools for Unity.
        // Use EditorCompilationInterface.GetAllMonoIslands internally instead of this method.
        internal static UnityEditor.Scripting.MonoIsland[] GetMonoIslands()
        {
            return EditorCompilationInterface.GetAllMonoIslands();
        }

        internal static string[] GetCompilationDefinesForPlayer()
        {
            var group = EditorUserBuildSettings.activeBuildTargetGroup;
            var target = EditorUserBuildSettings.activeBuildTarget;
            return GetCompilationDefines(EditorScriptCompilationOptions.BuildingEmpty, group, target);
        }

        internal static string GetMonolithicEngineAssemblyPath()
        {
            // We still build a monolithic UnityEngine.dll as a compilation target for user projects.
            // It lives next to the editor dll.
            var dir = Path.GetDirectoryName(GetEditorAssemblyPath());
            return Path.Combine(dir, "UnityEngine.dll");
        }

        internal static string[] GetCompilationDefines(EditorScriptCompilationOptions options, BuildTargetGroup targetGroup, BuildTarget target)
        {
            return GetCompilationDefines(options, targetGroup, target, PlayerSettings.GetApiCompatibilityLevel(targetGroup));
        }

        public static void SetShowGizmos(bool value)
        {
            var view = PlayModeView.GetMainPlayModeView();

            if (view == null)
                view = PlayModeView.GetRenderingView();

            if (view == null)
                return;

            view.SetShowGizmos(value);
        }
    }
}
