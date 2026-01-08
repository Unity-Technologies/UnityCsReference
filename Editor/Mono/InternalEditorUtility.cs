// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Experimental;
using UnityEngine.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.UIElements;
using UnityEngine.Video;
using UnityEditor.Build;

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
                case "physicmaterial": return EditorGUIUtility.FindTexture(typeof(PhysicsMaterial));
                case "prefab": return EditorGUIUtility.FindTexture("Prefab Icon");
                case "shader": return EditorGUIUtility.FindTexture(typeof(Shader));
                case "blockshader": return EditorGUIUtility.FindTexture("BlockShaderContainer Icon");
                case "txt": return EditorGUIUtility.FindTexture(typeof(TextAsset));
                case "unity": return EditorGUIUtility.FindTexture(typeof(SceneAsset));
                case "prefs": return EditorGUIUtility.FindTexture(typeof(EditorSettings));
                case "anim": return EditorGUIUtility.FindTexture(typeof(Animation));
                case "meta": return EditorGUIUtility.FindTexture("MetaFile Icon");
                case "mixer": return EditorGUIUtility.FindTexture(typeof(UnityEditor.Audio.AudioMixerController));
                case "uxml": return EditorGUIUtility.FindTexture(typeof(UnityEngine.UIElements.VisualTreeAsset));
                case "uss": return EditorGUIUtility.FindTexture(typeof(StyleSheet));
                case "lighting": return EditorGUIUtility.FindTexture(typeof(UnityEngine.LightingSettings));
                case "controller": return EditorGUIUtility.FindTexture(typeof(UnityEditor.Animations.AnimatorController));
                case "overridecontroller": return EditorGUIUtility.FindTexture(typeof(AnimatorOverrideController));
                case "mask": return EditorGUIUtility.FindTexture(typeof(AvatarMask));
                case "scenetemplate": return EditorGUIUtility.FindTexture("UnityEditor/SceneTemplate/SceneTemplateAsset Icon");
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

                case "3df": case "3dm": case "3dmf": case "3ds": case "3dv": case "3dx": case "blend":
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

                case "vulkandevicefilter":
                case "d3d12devicefilter":
                    return EditorGUIUtility.FindTexture(typeof(EditorSettings));

                default: return null;
            }
        }

        [RequiredByNativeCode]
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
                        gc.image = EditorGUIUtility.LoadIcon("WaitSpin" + i.ToString("00"));
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

        internal static void GetRangeAndToggleStatesFromCurrentKeyModifiers(bool useShiftAsActionKey, out bool useRangeSelection, out bool useToggleSelection)
        {
            if (useShiftAsActionKey)
            {
                useToggleSelection = false; // toggling does not make sense when navigating with e.g a keyboard, so we treat both shift and ctrl/cmd as range selection
                useRangeSelection = (Event.current?.shift ?? false) || EditorGUI.actionKey;
            }
            else
            {
                useToggleSelection = EditorGUI.actionKey;
                useRangeSelection = Event.current?.shift ?? false;
            }
        }

        [Obsolete("Use HandleMultiSelection<T> instead", false)]
        public static List<EntityId> GetNewSelection(EntityId clickedInstanceID, List<EntityId> allInstanceIDs, List<EntityId> selectedInstanceIDs, EntityId lastClickedInstanceID, bool keepMultiSelection, bool useShiftAsActionKey, bool allowMultiSelection)
        {
            return HandleMultiSelectionWithCurrentModifiers(clickedInstanceID, allInstanceIDs, selectedInstanceIDs, lastClickedInstanceID, keepMultiSelection, allowMultiSelection, useShiftAsActionKey);
        }

        [Obsolete("Use HandleMultiSelection<T> instead", false)]
        public static List<int> GetNewSelection(int clickedInstanceID, List<int> allInstanceIDs, List<int> selectedInstanceIDs, int lastClickedInstanceID, bool keepMultiSelection, bool useShiftAsActionKey, bool allowMultiSelection)
        {
            return HandleMultiSelectionWithCurrentModifiers(clickedInstanceID, allInstanceIDs, selectedInstanceIDs, lastClickedInstanceID, keepMultiSelection, allowMultiSelection, useShiftAsActionKey);
        }

        public static List<T> HandleMultiSelectionWithCurrentModifiers<T>(T clickedID, List<T> allIDs, List<T> selectedIDs, T lastClickedID, bool keepMultiSelection, bool allowMultiSelection, bool useShiftAsActionKey) where T : unmanaged, IEquatable<T>
        {
            GetRangeAndToggleStatesFromCurrentKeyModifiers(useShiftAsActionKey, out bool useRangeSelection, out bool useToggleSelection);
            return HandleMultiSelection(clickedID, allIDs, selectedIDs, lastClickedID, keepMultiSelection, allowMultiSelection, useRangeSelection, useToggleSelection);
        }

        /// <summary>
        /// Handles multi-selection logic for lists and tree views.
        /// Determines the new selection based on user interaction and what selection behavior is wanted using the
        /// input parameters.
        /// </summary>
        /// <param name="clickedID">The ID of the item that was clicked or selected by keyboard navigation</param>
        /// <param name="allIDs">Complete list of all available items in display order</param>
        /// <param name="selectedIDs">Currently selected items before this interaction</param>
        /// <param name="lastClickedID">The ID of the previously clicked item (used for range selection)</param>
        /// <param name="keepMultiSelection">
        /// Controls interaction behavior:
        /// - true: Preserve existing selection when clicked on existing selection (used for drag operations, context menus)
        /// - false: Allow normal selection modification based on other parameters
        /// </param>
        /// <param name="allowMultiSelection">
        /// Controls multiselection behavior:
        /// - true: Clicked item id supports multi-selection enables range/toggle behavior
        /// - false: Clicked item id is single-selection only - ignores all modifiers
        /// </param>
        /// <param name="useRangeSelection">
        /// Request range selection behavior:
        /// - true: Select continuous range from first selected item to clicked item (typically Shift+click)
        /// - false: No range selection requested
        /// </param>
        /// <param name="useToggleSelection">
        /// Request toggle selection behavior:
        /// - true: Add/remove clicked item from selection (typically Ctrl/Cmd+click)
        /// - false: No toggle selection requested
        /// </param>
        /// <returns>New selection list reflecting the result of this interaction</returns>
        /// <remarks>
        /// Selection behavior priority:
        /// 1. If allowMultiSelection=false: Always single selection, ignore all modifiers
        /// 2. If keepMultiSelection=true: If clicked in existing selection keep this selection (for dragging)
        /// 3. If useRangeSelection=true: Perform range selection (takes priority over toggle)
        /// 4. If useToggleSelection=true: Perform toggle selection (add/remove item)
        /// 5. Otherwise: Replace selection with clicked item (normal click)
        ///
        /// Common usage patterns:
        /// - Mouse click: useRangeSelection=Event.shift, useToggleSelection=EditorGUI.actionKey
        /// - Keyboard navigation: useRangeSelection=Event.shift || EditorGUI.actionKey, useToggleSelection=false (toggling should be disabled for keyboard navigation)
        /// - Drag start: keepMultiSelection=true (if clickedID is part of current selection then preserve selection for drag operation)
        /// - Context menu: keepMultiSelection=true (preserve selection for menu)
        /// - Single-select component: allowMultiSelection=false (ignore all modifiers)
        /// </remarks>
        // Internal for testing
        internal static List<T> HandleMultiSelection<T>(T clickedID, List<T> allIDs, List<T> selectedIDs, T lastClickedID, bool keepMultiSelection, bool allowMultiSelection, bool useRangeSelection, bool useToggleSelection) where T : unmanaged, IEquatable<T>
        {
            if (EditorUtility.isInSafeMode && clickedID is EntityId)
            {
                // InstanceIDs are 0 for non script assets in safe mode. And they should not be selectable
                if (clickedID.Equals(EntityId.None))
                    return new List<T>(selectedIDs);
            }

            if (!allowMultiSelection)
                useRangeSelection = useToggleSelection = false;

            int firstIndex;
            int lastIndex;
            // Toggle selected node from selection
            if (useToggleSelection && !useRangeSelection)
            {
                var newSelection = new List<T>(selectedIDs);
                if (newSelection.Contains(clickedID))
                {
                    if (!keepMultiSelection)
                        newSelection.Remove(clickedID);
                }
                else
                {
                    newSelection.Add(clickedID);
                }
                return newSelection;
            }
            // Select everything between the first selected object and the selected
            else if (useRangeSelection)
            {
                if (clickedID.Equals(lastClickedID))
                {
                    return new List<T>(selectedIDs);
                }

                if (!GetFirstAndLastSelected(allIDs, selectedIDs, out firstIndex, out lastIndex))
                {
                    // We had no selection
                    var newSelection = new List<T>(1);
                    newSelection.Add(clickedID);

                    return newSelection;
                }

                int newIndex = -1;
                int prevIndex = -1;

                for (int i = 0; i < allIDs.Count; ++i)
                {
                    if (allIDs[i].Equals(clickedID))
                        newIndex = i;

                    if (allIDs[i].Equals(lastClickedID))
                        prevIndex = i;
                }

                if (newIndex == -1)
                {
                    Debug.LogError($"Invalid selection input: The '{nameof(clickedID)}' should be part of '{nameof(allIDs)}'");
                    return new List<T>();
                }

                int dir = 0;
                if (prevIndex != -1)
                    dir = (newIndex > prevIndex) ? 1 : -1;

                int from = 0, to = 0;
                var addExisting = false;

                bool usingArrowKeys = Event.current != null ? Event.current.keyCode == KeyCode.DownArrow || Event.current.keyCode == KeyCode.UpArrow : false;
                var clickedInTheMiddle = lastIndex > newIndex && firstIndex < newIndex;

                if (selectedIDs.Count > 1)
                {
                    var newID = allIDs[newIndex];
                    var noGapsInSelection = (allIDs.Count - firstIndex + selectedIDs.Count) == allIDs.Count - lastIndex;
                    var isInSelection = selectedIDs.Contains(newID);
                    // if the newly clicked item is already selected,
                    // we treat this as a combination of selecting items from the highest selected item to the clicked item
                    // or from the lowest selected item to the clicked item depending on the direction of the selection,
                    // e.g. if we select item 1 and shift-select item 5, then shift-select item 3, we'll have items 1 to 3 selected
                    if (isInSelection || noGapsInSelection || clickedInTheMiddle)
                    {
                        from = dir > 0 ? firstIndex : newIndex;
                        to = dir > 0 ? newIndex : lastIndex;

                        // if we clicked in-between the lowest and highest selected indices of a selection containing gaps
                        // and the item was not already in the selection
                        // make sure that the new selection is added to the currently existing one
                        if (clickedInTheMiddle && !noGapsInSelection && !isInSelection)
                            addExisting = true;
                    }
                    else if (dir > 0)
                    {
                        if (newIndex > lastIndex)
                        {
                            from = lastIndex + 1;
                            to = newIndex;

                            addExisting = true;
                        }
                        else
                        {
                            from = newIndex;
                            to = lastIndex;
                        }
                    }
                    else if (dir < 0)
                    {
                        if (newIndex < firstIndex)
                        {
                            from = newIndex;
                            to = firstIndex - 1;

                            addExisting = true;
                        }
                        else
                        {
                            from = firstIndex;
                            to = newIndex;
                        }
                    }
                }

                if (!addExisting || usingArrowKeys)
                {
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
                }

                List<T> allSelectedInstanceIDs = new List<T>();

                if (addExisting && !usingArrowKeys)
                {
                    allSelectedInstanceIDs.AddRange(selectedIDs.GetRange(0, selectedIDs.Count));
                    allSelectedInstanceIDs.AddRange(allIDs.GetRange(from, to - from + 1));

                    if (clickedInTheMiddle)
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        allSelectedInstanceIDs = allSelectedInstanceIDs.Distinct().ToList();
#pragma warning restore RS0030
                }
                else
                {
                    allSelectedInstanceIDs.AddRange(allIDs.GetRange(from, to - from + 1));
                }

                if (EditorUtility.isInSafeMode && allSelectedInstanceIDs is List<EntityId>)
                {
                    allSelectedInstanceIDs.RemoveAll(id => id.Equals(EntityId.None));
                }

                return allSelectedInstanceIDs;
            }
            // Just set the selection to the clicked object
            else
            {
                if (keepMultiSelection)
                {
                    // Don't change selection on mouse down when clicking on selected item.
                    // This is for dragging in case with multiple items selected or right click (mouse down should not unselect the rest).
                    if (selectedIDs.Contains(clickedID))
                    {
                        return new List<T>(selectedIDs);
                    }
                }

                var newSelection = new List<T>(1);
                newSelection.Add(clickedID);
                return newSelection;
            }
        }

        static internal bool GetFirstAndLastSelected<T>(List<T> allEntries, List<T> selectedInstanceIDs, out int firstIndex, out int lastIndex)
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
                    return true;
                case ".dll":
                case ".exe":
                    return AssemblyHelper.IsManagedAssembly(filename);
                default:
                    return false;
            }
        }

        internal static IEnumerable<string> GetAllScriptGUIDs()
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return AssetDatabase.GetAllAssetPaths()
#pragma warning restore RS0030
                .Where(asset => (IsScriptOrAssembly(asset) && !UnityEditor.PackageManager.Folders.IsPackagedAssetPath(asset)))
                .Select(asset => AssetDatabase.AssetPathToGUID(asset));
        }

        internal static string GetMonolithicEngineAssemblyPath()
        {
            // We still build a monolithic UnityEngine.dll as a compilation target for user projects.
            // It lives next to the editor dll.
            var dir = Path.GetDirectoryName(GetEditorAssemblyPath());
            return Path.Combine(dir, "UnityEngine.dll");
        }

        internal static string[] GetCompilationDefines(EditorScriptCompilationOptions options, BuildTarget target, int subtarget)
        {
            return GetCompilationDefines(options, target, subtarget, PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.FromActiveSettings(target)));
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

        private static Material blitSceneViewCaptureMat;

        [Obsolete("Use CaptureEditorWindow instead", false)]
        public static bool CaptureSceneView(SceneView sv, RenderTexture rt)
        {
            if (!sv.hasFocus)
                return false;

            if (blitSceneViewCaptureMat == null)
                blitSceneViewCaptureMat = (Material)EditorGUIUtility.LoadRequired("SceneView/BlitSceneViewCapture.mat");

            // Grab SceneView framebuffer into a temporary RT.
            RenderTexture tmp = RenderTexture.GetTemporary(rt.descriptor);
            Rect rect = new Rect(0, 0, sv.position.width, sv.position.height);
            sv.m_Parent.GrabPixels(tmp, rect);

            // Blit it into the target RT, it will be flipped by the shader if necessary.
            Graphics.Blit(tmp, rt, blitSceneViewCaptureMat);
            RenderTexture.ReleaseTemporary(tmp);

            return true;
        }

        public static bool CaptureEditorWindow(EditorWindow window, RenderTexture rt)
        {
            if (!window.hasFocus)
            {
                Debug.LogError("CaptureEditorWindow: window must have focus");
                return false;
            }

            blitSceneViewCaptureMat = blitSceneViewCaptureMat ?? (Material)EditorGUIUtility.LoadRequired("SceneView/BlitSceneViewCapture.mat");

            // Grab SceneView framebuffer into a temporary RT.
            RenderTexture tmp = RenderTexture.GetTemporary(rt.descriptor);
            Rect rect = new Rect(0, 0, window.position.width, window.position.height);
            window.m_Parent.GrabPixels(tmp, rect);

            // Blit it into the target RT, it will be flipped by the shader if necessary.
            Graphics.Blit(tmp, rt, blitSceneViewCaptureMat);
            RenderTexture.ReleaseTemporary(tmp);

            return true;
        }

        static readonly Regex k_UnityAssemblyRegex = new("^(Unity|UnityEditor|UnityEngine).", RegexOptions.Compiled);
        internal static bool IsUnityAssembly(Type type)
        {
            var assemblyName = type.Assembly.GetName().ToString();
            return k_UnityAssemblyRegex.IsMatch(assemblyName);
        }
    }
}
