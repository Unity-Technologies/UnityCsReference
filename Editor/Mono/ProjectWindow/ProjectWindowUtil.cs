// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Audio;
using UnityEditor.Compilation;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEditor.Experimental;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;
using UnityEngine.U2D;

namespace UnityEditor
{
    internal class DragAndDropDelay
    {
        public Vector2 mouseDownPosition;

        public bool CanStartDrag()
        {
            return Vector2.Distance(mouseDownPosition, Event.current.mousePosition) > 6;
        }
    }

    // Callbacks to be used when creating assets via the project window
    // You can extend the EndNameEditAction and write your own callback
    // It is done this way instead of via a delegate because the action
    // needs to survive an assembly reload.
    namespace ProjectWindowCallback
    {
        public abstract class EndNameEditAction : ScriptableObject
        {
            public virtual void OnEnable()
            {
                hideFlags = HideFlags.HideAndDontSave;
            }

            public abstract void Action(int instanceId, string pathName, string resourceFile);
            public virtual void Cancelled(int instanceId, string pathName, string resourceFile) {}

            public virtual void CleanUp()
            {
                DestroyImmediate(this);
            }
        }

        internal class DoCreateNewAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                AssetDatabase.CreateAsset(EditorUtility.InstanceIDToObject(instanceId),
                    AssetDatabase.GenerateUniqueAssetPath(pathName));
                ProjectWindowUtil.FrameObjectInProjectWindow(instanceId);
            }

            public override void Cancelled(int instanceId, string pathName, string resourceFile)
            {
                Selection.activeObject = null;
            }
        }

        internal class DoCreateFolder : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                string guid = AssetDatabase.CreateFolder(Path.GetDirectoryName(pathName), Path.GetFileName(pathName));
                Object o = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        internal class DoCreateScene : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                bool createDefaultGameObjects = true;
                if (EditorSceneManager.CreateSceneAsset(pathName, createDefaultGameObjects))
                {
                    Object sceneAsset = AssetDatabase.LoadAssetAtPath(pathName, typeof(SceneAsset));
                    ProjectWindowUtil.ShowCreatedAsset(sceneAsset);
                }
            }
        }

        internal class DoCreateFolderWithTemplates : EndNameEditAction
        {
            public string ResourcesTemplatePath = "Resources/ScriptTemplates";

            public bool UseCustomPath = false;

            public IList<string> templates { get; set; }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var fileName = Path.GetFileName(pathName);
                string guid = AssetDatabase.CreateFolder(Path.GetDirectoryName(pathName), fileName);
                string basePath = UseCustomPath ? ResourcesTemplatePath :
                    Path.Combine(EditorApplication.applicationContentsPath, ResourcesTemplatePath);

                foreach (var template in templates ?? Enumerable.Empty<string>())
                {
                    var templateNameWithoutTxt = template.Replace(".txt", string.Empty);
                    var templateExtension = Path.GetExtension(templateNameWithoutTxt);

                    ProjectWindowUtil.CreateScriptAssetFromTemplate(Path.Combine(pathName, fileName + templateExtension), Path.Combine(basePath, template));
                }

                Object o = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        internal class DoCreatePrefabVariant : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(resourceFile);
                Object o = PrefabUtility.CreateVariant(go, pathName);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        internal class DoCreateScriptAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Object o = ProjectWindowUtil.CreateScriptAssetFromTemplate(pathName, resourceFile);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        internal class DoCreateAssetWithContent : EndNameEditAction
        {
            public string filecontent;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Object o = ProjectWindowUtil.CreateScriptAssetWithContent(pathName, filecontent);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        internal class DoCreateAnimatorController : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Animations.AnimatorController controller = Animations.AnimatorController.CreateAnimatorControllerAtPath(pathName);
                ProjectWindowUtil.ShowCreatedAsset(controller);
            }
        }

        internal class DoCreateAudioMixer : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                AudioMixerController controller = AudioMixerController.CreateMixerControllerAtPath(pathName);

                // Check if the output group should be initialized (instanceID is stored in the resource file) TODO: rename 'resourceFile' to 'userData' so it's more obvious that it can be used by all EndNameEditActions
                if (!string.IsNullOrEmpty(resourceFile))
                {
                    int outputInstanceID;
                    if (System.Int32.TryParse(resourceFile, out outputInstanceID))
                    {
                        var outputGroup = InternalEditorUtility.GetObjectFromInstanceID(outputInstanceID) as AudioMixerGroupController;
                        if (outputGroup != null)
                            controller.outputAudioMixerGroup = outputGroup;
                    }
                }
                ProjectWindowUtil.ShowCreatedAsset(controller);
            }
        }

        internal class DoCreateSpritePolygon : EndNameEditAction
        {
            public int sides;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                bool showSpriteEditorAfter = false;
                if (sides < 0)
                {
                    sides = 5;
                    showSpriteEditorAfter = true;
                }

                Sprites.SpriteUtility.CreateSpritePolygonAssetAtPath(pathName, sides);
                if (showSpriteEditorAfter)
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(pathName);
                    SpriteUtilityWindow.ShowSpriteEditorWindow();
                }
            }
        }

        internal class DoCreateSpriteAtlas : EndNameEditAction
        {
            public int sides;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var spriteAtlasAsset = new SpriteAtlasAsset();
                InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { spriteAtlasAsset }, pathName, true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }
    }

    public class ProjectWindowUtil
    {
        [MenuItem("Assets/Create/GUI Skin", false, 601)]
        public static void CreateNewGUISkin()
        {
            GUISkin skin = ScriptableObject.CreateInstance<GUISkin>();
            GUISkin original = Resources.GetBuiltinResource(typeof(GUISkin), "GameSkin/GameSkin.guiskin") as GUISkin;
            if (original)
                EditorUtility.CopySerialized(original, skin);
            else
                Debug.LogError("Internal error: unable to load builtin GUIskin");

            CreateAsset(skin, "New GUISkin.guiskin");
        }

        // Returns the path of currently selected folder. If multiple are selected, returns the first one.
        internal static string GetActiveFolderPath()
        {
            ProjectBrowser projectBrowser = GetProjectBrowserIfExists();

            if (projectBrowser == null)
                return "Assets";

            return projectBrowser.GetActiveFolderPath();
        }

        internal static bool TryGetActiveFolderPath(out string path)
        {
            ProjectBrowser projectBrowser = GetProjectBrowserIfExists();

            path = string.Empty;

            if (projectBrowser == null || !projectBrowser.IsTwoColumns())
                return false;

            path = projectBrowser.GetActiveFolderPath();

            return true;
        }

        internal static void EndNameEditAction(EndNameEditAction action, int instanceId, string pathName, string resourceFile, bool accepted)
        {
            pathName = AssetDatabase.GenerateUniqueAssetPath(pathName);
            if (action != null)
            {
                if (accepted)
                    action.Action(instanceId, pathName, resourceFile);
                else
                    action.Cancelled(instanceId, pathName, resourceFile);
                action.CleanUp();
            }
        }

        // Create a standard Object-derived asset.
        public static void CreateAsset(Object asset, string pathName)
        {
            StartNameEditingIfProjectWindowExists(asset.GetInstanceID(), ScriptableObject.CreateInstance<DoCreateNewAsset>(), pathName, AssetPreview.GetMiniThumbnail(asset), null);
        }

        // Create a folder
        public static void CreateFolder()
        {
            StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateFolder>(), "New Folder", EditorGUIUtility.IconContent(EditorResources.emptyFolderIconName).image as Texture2D, null);
        }

        internal static void CreateFolderWithTemplates(string defaultName, params string[] templates)
        {
            var folderIcon = templates != null && templates.Length > 0
                ? EditorResources.folderIconName
                : EditorResources.emptyFolderIconName;

            var endNameEditAction = ScriptableObject.CreateInstance<DoCreateFolderWithTemplates>();
            endNameEditAction.templates = templates;
            StartNameEditingIfProjectWindowExists(0, endNameEditAction, defaultName, EditorGUIUtility.IconContent(folderIcon).image as Texture2D, null);
        }

        internal static void CreateFolderWithTemplatesWithCustomResourcesPath(string defaultName, string customResPath, params string[] templates)
        {
            var folderIcon = templates != null && templates.Length > 0
                ? EditorResources.folderIconName
                : EditorResources.emptyFolderIconName;

            var endNameEditAction = ScriptableObject.CreateInstance<DoCreateFolderWithTemplates>();
            endNameEditAction.templates = templates;
            endNameEditAction.ResourcesTemplatePath = customResPath;
            endNameEditAction.UseCustomPath = true;
            StartNameEditingIfProjectWindowExists(0, endNameEditAction, defaultName, EditorGUIUtility.IconContent(folderIcon).image as Texture2D, null);
        }

        public static void CreateScene()
        {
            StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateScene>(), "New Scene.unity", EditorGUIUtility.FindTexture(typeof(SceneAsset)), null);
        }

        [MenuItem("Assets/Create/Prefab Variant", true)]
        static bool CreatePrefabVariantValidation()
        {
            var go = Selection.activeGameObject;
            return (go != null && EditorUtility.IsPersistent(go));
        }

        [MenuItem("Assets/Create/Prefab Variant", false, 202)]
        static void CreatePrefabVariant()
        {
            var go = Selection.activeGameObject;
            if (go == null || !EditorUtility.IsPersistent(go))
                return;

            string sourcePath = AssetDatabase.GetAssetPath(go);

            string sourceDir = Path.GetDirectoryName(sourcePath).ConvertSeparatorsToUnity();
            string variantPath = string.Format("{0}/{1} Variant.prefab", sourceDir, go.name);

            StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreatePrefabVariant>(),
                variantPath,
                EditorGUIUtility.FindTexture("PrefabVariant Icon") as Texture2D,
                sourcePath);
        }

        public static void CreateAssetWithContent(string filename, string content, Texture2D icon = null)
        {
            var action = ScriptableObject.CreateInstance<DoCreateAssetWithContent>();
            action.filecontent = content;
            StartNameEditingIfProjectWindowExists(0, action, filename, icon, null);
        }

        [RequiredByNativeCode]
        public static void CreateScriptAssetFromTemplateFile(string templatePath, string defaultNewFileName)
        {
            if (templatePath == null)
                throw new ArgumentNullException(nameof(templatePath));
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"The template file \"{templatePath}\" could not be found.", templatePath);

            if (string.IsNullOrEmpty(defaultNewFileName))
                defaultNewFileName = Path.GetFileName(templatePath);

            Texture2D icon = null;
            switch (Path.GetExtension(defaultNewFileName))
            {
                case ".cs":
                    icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                    break;
                case ".shader":
                    icon = EditorGUIUtility.IconContent<Shader>().image as Texture2D;
                    break;
                case ".asmdef":
                    icon = EditorGUIUtility.IconContent<AssemblyDefinitionAsset>().image as Texture2D;
                    break;
                case ".asmref":
                    icon = EditorGUIUtility.IconContent<AssemblyDefinitionReferenceAsset>().image as Texture2D;
                    break;
                default:
                    icon = EditorGUIUtility.IconContent<TextAsset>().image as Texture2D;
                    break;
            }
            StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateScriptAsset>(), defaultNewFileName, icon, templatePath);
        }

        public static void ShowCreatedAsset(Object o)
        {
            // Show it
            Selection.activeObject = o;
            if (o)
                FrameObjectInProjectWindow(o.GetInstanceID());
        }

        static private void CreateAnimatorController()
        {
            var icon = EditorGUIUtility.IconContent<Animations.AnimatorController>().image as Texture2D;
            StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateAnimatorController>(), "New Animator Controller.controller", icon, null);
        }

        static private void CreateAudioMixer()
        {
            var icon = EditorGUIUtility.IconContent<AudioMixerController>().image as Texture2D;
            StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateAudioMixer>(), "NewAudioMixer.mixer", icon, null);
        }

        static private void CreateSpriteAtlas()
        {
            var icon = EditorGUIUtility.IconContent<SpriteAtlasAsset>().image as Texture2D;
            DoCreateSpriteAtlas action = ScriptableObject.CreateInstance<DoCreateSpriteAtlas>();
            StartNameEditingIfProjectWindowExists(0, action, "New Sprite Atlas.spriteatlasv2", icon, null);
        }

        static private void CreateSpritePolygon(int sides)
        {
            string assetName = "";
            switch (sides)
            {
                case 0:
                    assetName = "Square";
                    break;
                case 3:
                    assetName = "Triangle";
                    break;
                case 4:
                    assetName = "Diamond";
                    break;
                case 6:
                    assetName = "Hexagon";
                    break;
                case 42:
                    // http://hitchhikers.wikia.com/wiki/42
                    assetName = "Everythingon";
                    break;
                case 128:
                    assetName = "Circle";
                    break;
                default:
                    assetName = "Polygon";
                    break;
            }

            var icon = EditorGUIUtility.IconContent<Sprite>().image as Texture2D;
            DoCreateSpritePolygon action = ScriptableObject.CreateInstance<DoCreateSpritePolygon>();
            action.sides = sides;
            StartNameEditingIfProjectWindowExists(0, action, assetName + ".png", icon, null);
        }

        internal static string SetLineEndings(string content, LineEndingsMode lineEndingsMode)
        {
            const string windowsLineEndings = "\r\n";
            const string unixLineEndings = "\n";

            string preferredLineEndings;

            switch (lineEndingsMode)
            {
                case LineEndingsMode.OSNative:
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                        preferredLineEndings = windowsLineEndings;
                    else
                        preferredLineEndings = unixLineEndings;
                    break;
                case LineEndingsMode.Unix:
                    preferredLineEndings = unixLineEndings;
                    break;
                case LineEndingsMode.Windows:
                    preferredLineEndings = windowsLineEndings;
                    break;
                default:
                    preferredLineEndings = unixLineEndings;
                    break;
            }

            content = Regex.Replace(content, @"\r\n?|\n", preferredLineEndings);

            return content;
        }

        internal static Object CreateScriptAssetWithContent(string pathName, string templateContent)
        {
            templateContent = SetLineEndings(templateContent, EditorSettings.lineEndingsForNewScripts);

            string fullPath = Path.GetFullPath(pathName);

            // utf8-bom encoding was added for case 510374 in 2012. i think this was the wrong solution. BOM's are
            // problematic for diff tools, naive readers and writers (of which we have many!), and generally not
            // something most people think about. you wouldn't believe how many unity source files have BOM's embedded
            // in the middle of them for no reason. copy paste problem? bad tool? unity should instead have been fixed
            // to read all files that have no BOM as utf8 by default, and then we just strip them all, always, from
            // files we control. perhaps we'll do this one day and this next line can be removed. -scobi
            var encoding = new System.Text.UTF8Encoding(/*encoderShouldEmitUTF8Identifier:*/ true);

            File.WriteAllText(fullPath, templateContent, encoding);

            // Import the asset
            AssetDatabase.ImportAsset(pathName);

            return AssetDatabase.LoadAssetAtPath(pathName, typeof(Object));
        }

        internal static string RemoveOrInsertNamespace(string content, string rootNamespace)
        {
            var rootNamespaceBeginTag = "#ROOTNAMESPACEBEGIN#";
            var rootNamespaceEndTag = "#ROOTNAMESPACEEND#";

            if (!content.Contains(rootNamespaceBeginTag) || !content.Contains(rootNamespaceEndTag))
                return content;

            if (string.IsNullOrEmpty(rootNamespace))
            {
                content = Regex.Replace(content, $"((\\r\\n)|\\n)[ \\t]*{rootNamespaceBeginTag}[ \\t]*", string.Empty);
                content = Regex.Replace(content, $"((\\r\\n)|\\n)[ \\t]*{rootNamespaceEndTag}[ \\t]*", string.Empty);

                return content;
            }

            // Use first found newline character as newline for entire file after replace.
            var newline = content.Contains("\r\n") ? "\r\n" : "\n";
            var contentLines = new List<string>(content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));

            int i = 0;

            for (; i < contentLines.Count; ++i)
            {
                if (contentLines[i].Contains(rootNamespaceBeginTag))
                    break;
            }

            var beginTagLine = contentLines[i];

            // Use the whitespace between beginning of line and #ROOTNAMESPACEBEGIN# as identation.
            var indentationString = beginTagLine.Substring(0, beginTagLine.IndexOf("#"));

            contentLines[i] = $"namespace {rootNamespace}";
            contentLines.Insert(i + 1, "{");

            i += 2;

            for (; i < contentLines.Count; ++i)
            {
                var line = contentLines[i];

                if (String.IsNullOrEmpty(line) || line.Trim().Length == 0)
                    continue;

                if (line.Contains(rootNamespaceEndTag))
                {
                    contentLines[i] = "}";
                    break;
                }

                contentLines[i] = $"{indentationString}{line}";
            }

            return string.Join(newline, contentLines.ToArray());
        }

        internal static Object CreateScriptAssetFromTemplate(string pathName, string resourceFile)
        {
            string rootNamespace = null;

            if (Path.GetExtension(pathName) == ".cs")
            {
                rootNamespace = CompilationPipeline.GetAssemblyRootNamespaceFromScriptPath(pathName);
            }

            string content = File.ReadAllText(resourceFile);

            // #NOTRIM# is a special marker that is used to mark the end of a line where we want to leave whitespace. prevent editors auto-stripping it by accident.
            content = content.Replace("#NOTRIM#", "");

            // macro replacement
            string baseFile = Path.GetFileNameWithoutExtension(pathName);

            content = content.Replace("#NAME#", baseFile);
            string baseFileNoSpaces = baseFile.Replace(" ", "");
            content = content.Replace("#SCRIPTNAME#", baseFileNoSpaces);

            content = RemoveOrInsertNamespace(content, rootNamespace);

            // if the script name begins with an uppercase character we support a lowercase substitution variant
            if (char.IsUpper(baseFileNoSpaces, 0))
            {
                baseFileNoSpaces = char.ToLower(baseFileNoSpaces[0]) + baseFileNoSpaces.Substring(1);
                content = content.Replace("#SCRIPTNAME_LOWER#", baseFileNoSpaces);
            }
            else
            {
                // still allow the variant, but change the first character to upper and prefix with "my"
                baseFileNoSpaces = "my" + char.ToUpper(baseFileNoSpaces[0]) + baseFileNoSpaces.Substring(1);
                content = content.Replace("#SCRIPTNAME_LOWER#", baseFileNoSpaces);
            }

            return CreateScriptAssetWithContent(pathName, content);
        }

        public static void StartNameEditingIfProjectWindowExists(int instanceID, EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile)
        {
            ProjectBrowser pb = GetProjectBrowserIfExists();
            if (pb)
            {
                pb.Focus();
                pb.BeginPreimportedNameEditing(instanceID, endAction, pathName, icon, resourceFile);
                pb.Repaint();
            }
            else
            {
                if (!pathName.StartsWith("assets/", StringComparison.CurrentCultureIgnoreCase))
                    pathName = "Assets/" + pathName;
                EndNameEditAction(endAction, instanceID, pathName, resourceFile, true);
                Selection.activeObject = EditorUtility.InstanceIDToObject(instanceID);
            }
        }

        static ProjectBrowser GetProjectBrowserIfExists()
        {
            return ProjectBrowser.s_LastInteractedProjectBrowser;
        }

        internal static void FrameObjectInProjectWindow(int instanceID)
        {
            ProjectBrowser pb = GetProjectBrowserIfExists();
            if (pb)
            {
                pb.FrameObject(instanceID, false);
            }
        }

        // InstanceIDs larger than this is considered a favorite by the projectwindows
        internal static int k_FavoritesStartInstanceID = 1000000000;
        internal static string k_DraggingFavoriteGenericData = "DraggingFavorite";
        internal static string k_IsFolderGenericData = "IsFolder";

        internal static bool IsFavoritesItem(int instanceID)
        {
            return instanceID >= k_FavoritesStartInstanceID && instanceID != ProjectBrowser.kPackagesFolderInstanceId;
        }

        internal static void StartDrag(int draggedInstanceID, List<int> selectedInstanceIDs)
        {
            if (draggedInstanceID == ProjectBrowser.kPackagesFolderInstanceId)
                return;

            DragAndDrop.PrepareStartDrag();

            string title = "";
            if (IsFavoritesItem(draggedInstanceID))
            {
                DragAndDrop.SetGenericData(k_DraggingFavoriteGenericData, draggedInstanceID);
            }
            else
            {
                // Normal assets dragging
                bool isFolder = IsFolder(draggedInstanceID);
                DragAndDrop.objectReferences = GetDragAndDropObjects(draggedInstanceID, selectedInstanceIDs);
                DragAndDrop.SetGenericData(k_IsFolderGenericData, isFolder ? "isFolder" : "");
                string[] paths = GetDragAndDropPaths(draggedInstanceID, selectedInstanceIDs);
                if (paths.Length > 0)
                    DragAndDrop.paths = paths;

                if (DragAndDrop.objectReferences.Length > 1)
                    title = "<Multiple>";
                else
                    title = ObjectNames.GetDragAndDropTitle(InternalEditorUtility.GetObjectFromInstanceID(draggedInstanceID));
            }

            DragAndDrop.StartDrag(title);
        }

        internal static Object[] GetDragAndDropObjects(int draggedInstanceID, List<int> selectedInstanceIDs)
        {
            List<Object> outList = new List<Object>(selectedInstanceIDs.Count);
            if ((Event.current.control || Event.current.command) && !selectedInstanceIDs.Contains(draggedInstanceID))
            {
                selectedInstanceIDs.Add(draggedInstanceID);
            }
            if (selectedInstanceIDs.Contains(draggedInstanceID))
            {
                for (int i = 0; i < selectedInstanceIDs.Count; ++i)
                {
                    Object obj = InternalEditorUtility.GetObjectFromInstanceID(selectedInstanceIDs[i]);
                    if (obj != null)
                        outList.Add(obj);
                }
            }
            else
            {
                Object obj = InternalEditorUtility.GetObjectFromInstanceID(draggedInstanceID);
                if (obj != null)
                    outList.Add(obj);
            }
            return outList.ToArray();
        }

        internal static string[] GetDragAndDropPaths(int draggedInstanceID, List<int> selectedInstanceIDs)
        {
            // Assets
            List<string> paths = new List<string>();
            foreach (int instanceID in selectedInstanceIDs)
            {
                if (AssetDatabase.IsMainAsset(instanceID))
                {
                    string path = AssetDatabase.GetAssetPath(instanceID);
                    paths.Add(path);
                }
            }

            string dragPath = AssetDatabase.GetAssetPath(draggedInstanceID);
            if (!string.IsNullOrEmpty(dragPath))
            {
                if (paths.Contains(dragPath))
                {
                    return paths.ToArray();
                }
                else if (Event.current.control || Event.current.command)
                {
                    paths.Add(dragPath);
                    return paths.ToArray();
                }
                else
                {
                    return new[] { dragPath };
                }
            }
            return new string[0];
        }

        // Returns instanceID of folders (and main asset if input is a subasset) up until and including the Assets folder
        public static int[] GetAncestors(int instanceID)
        {
            HashSet<int> ancestors = new HashSet<int>();
            GetAncestors(instanceID, ancestors);
            return ancestors.ToArray();
        }

        internal static void GetAncestors(int instanceID, HashSet<int> ancestors)
        {
            // Ensure we handle packages root folder
            if (instanceID == ProjectBrowser.kPackagesFolderInstanceId)
                return;

            // Ensure we add the main asset as ancestor if input is a subasset
            int mainAssetInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(AssetDatabase.GetAssetPath(instanceID));
            bool isSubAsset = mainAssetInstanceID != instanceID;
            if (isSubAsset)
                ancestors.Add(mainAssetInstanceID);

            // Find ancestors of main aset
            string currentFolderPath = GetContainingFolder(AssetDatabase.GetAssetPath(mainAssetInstanceID));
            while (!string.IsNullOrEmpty(currentFolderPath))
            {
                int currentInstanceID = ProjectBrowser.GetFolderInstanceID(currentFolderPath);
                ancestors.Add(currentInstanceID);
                currentFolderPath = GetContainingFolder(AssetDatabase.GetAssetPath(currentInstanceID));
            }
        }

        public static bool IsFolder(int instanceID)
        {
            return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(instanceID));
        }

        // Returns containing folder if possible otherwise null.
        // Trims any trailing forward slashes
        public static string GetContainingFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            path = path.Trim('/');
            int pos = path.LastIndexOf("/", StringComparison.Ordinal);
            if (pos != -1)
            {
                return path.Substring(0, pos);
            }

            // Could not determine containing folder
            return null;
        }

        // Input the following list:
        //  assets/flesh/big
        //  assets/icons/duke
        //  assets/icons/duke/snake
        //  assets/icons/duke/zoo
        //
        // ... And the returned list becomes:
        //  assets/flesh/big
        //  assets/icons/duke

        // Returned paths are trimmed for ending slashes
        public static string[] GetBaseFolders(string[] folders)
        {
            if (folders.Length <= 1)
                return folders;

            List<string> result = new List<string>();
            List<string> sortedFolders = new List<string>(folders);

            // Remove forward slashes before sorting otherwise will "Assets 1/" come before "Assets/"
            // which we do not want in the find base folders section below
            for (int i = 0; i < sortedFolders.Count; ++i)
                sortedFolders[i] = sortedFolders[i].Trim('/');

            sortedFolders.Sort();

            // Ensure folder paths are ending with '/' so e.g: "assets/" is not found in "assets 1/".
            // If we did not end with '/' then "assets" could be found in "assets 1"
            // which is not what we want when finding base folders
            for (int i = 0; i < sortedFolders.Count; ++i)
                if (!sortedFolders[i].EndsWith("/"))
                    sortedFolders[i] = sortedFolders[i] + "/";

            // Find base folders
            // We assume sortedFolders is sorted with less first. E.g: {assets/, assets/icons/}
            string curPath = sortedFolders[0];
            result.Add(curPath);
            for (int i = 1; i < sortedFolders.Count; ++i)
            {
                // Ensure path matches from start of curPath (to ensure "assets/monkey" and "npc/assets/monkey" both are returned as base folders)
                bool startOfPathMatches = sortedFolders[i].IndexOf(curPath, StringComparison.Ordinal) == 0;
                if (!startOfPathMatches)
                {
                    // Add tested path if not part of current path and use tested path as new base
                    result.Add(sortedFolders[i]);
                    curPath = sortedFolders[i];
                }
            }

            // Remove forward slashes again (added above)
            for (int i = 0; i < result.Count; ++i)
                result[i] = result[i].Trim('/');

            return result.ToArray();
        }

        internal static void DuplicateSelectedAssets()
        {
            Selection.objects = DuplicateAssets(Selection.objects).ToArray();
        }

        // Deletes the assets of the instance IDs, with an optional user confirmation dialog.
        // Returns true if the delete operation was successfully performed on all assets.
        // Note: Zero input assets always returns true.
        // Also note that the operation cannot be undone even if some operations failed.
        internal static bool DeleteAssets(List<int> instanceIDs, bool askIfSure)
        {
            if (instanceIDs.Count == 0)
                return true;

            bool foundAssetsFolder = instanceIDs.IndexOf(AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets")) >= 0;
            if (foundAssetsFolder)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Cannot Delete"), L10n.Tr("Deleting the 'Assets' folder is not allowed"), L10n.Tr("Ok"));
                return false;
            }

            var paths = GetMainPathsOfAssets(instanceIDs).ToList();

            if (paths.Count == 0)
                return false;

            if (askIfSure)
            {
                string title;
                if (paths.Count > 1)
                    title = L10n.Tr("Delete selected assets?");
                else
                    title = L10n.Tr("Delete selected asset?");

                int maxCount = 3;
                var infotext = new StringBuilder();
                for (int i = 0; i < paths.Count && i < maxCount; ++i)
                    infotext.AppendLine("   " + paths[i]);
                if (paths.Count > maxCount)
                    infotext.AppendLine("   ...");
                infotext.AppendLine(L10n.Tr("You cannot undo this action."));
                if (!EditorUtility.DisplayDialog(title, infotext.ToString(), L10n.Tr("Delete"), L10n.Tr("Cancel")))
                {
                    return false;
                }
            }

            bool success = true;

            AssetDatabase.StartAssetEditing();

            string[] pathArray = new string[paths.Count];
            for (int i = 0; i < pathArray.Length; i++)
                pathArray[i] = paths[i];

            List<string> failedPaths = new List<string>();
            if (!AssetDatabase.MoveAssetsToTrash(pathArray, failedPaths))
                success = false;

            AssetDatabase.StopAssetEditing();
            if (!success)
            {
                string message = (Provider.enabled && Provider.onlineState == OnlineState.Offline) ?
                    L10n.Tr("Some assets could not be deleted.\nMake sure you are connected to your Version Control server or \"Work Offline\" is enabled.") :
                    L10n.Tr("Some assets could not be deleted.\nMake sure nothing is keeping a hook on them, like a loaded DLL for example.");

                EditorUtility.DisplayDialog(L10n.Tr("Cannot Delete"), message, L10n.Tr("Ok"));
            }
            return success;
        }

        internal static IEnumerable<Object> DuplicateAssets(IEnumerable<Object> assets)
        {
            var copiedPaths = new List<string>();
            Object firstDuplicatedObjectToFail = null;

            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);

                // if duplicating a sub-asset, then create a copy next to the main asset file
                if (!String.IsNullOrEmpty(assetPath) && AssetDatabase.IsSubAsset(asset))
                {
                    if (asset is ISubAssetNotDuplicatable)
                    {
                        firstDuplicatedObjectToFail = firstDuplicatedObjectToFail ? firstDuplicatedObjectToFail : asset;
                        continue;
                    }

                    var extension = NativeFormatImporterUtility.GetExtensionForAsset(asset);

                    // We dot sanitize or block unclean the asset filename (asset.name)
                    // since the assertdb will do it for us and has a whole tailored logic for that.

                    // It feels wrong that the asset name (that can apparently contain any char)
                    // is conflated with the orthogonal notion of filename. From the user's POV
                    // it will force an asset dup but with mangled names if the original name contained
                    // "invalid chars" for filenames.
                    // Path.Combine is not used here to avoid blocking asset names that might
                    // contain chars not allowed in filenames.
                    if ((new HashSet<char>(Path.GetInvalidFileNameChars())).Intersect(asset.name).Count() != 0)
                    {
                        Debug.LogWarning(string.Format("Duplicated asset name '{0}' contains invalid characters. Those will be replaced in the duplicated asset name.", asset.name));
                    }

                    var newPath = AssetDatabase.GenerateUniqueAssetPath(
                        string.Format("{0}{1}{2}.{3}",
                            Path.GetDirectoryName(assetPath),
                            Path.DirectorySeparatorChar,
                            asset.name,
                            extension)
                    );
                    AssetDatabase.CreateAsset(Object.Instantiate(asset), newPath);
                    copiedPaths.Add(newPath);
                }
                // otherwise duplicate the main asset file
                else if (EditorUtility.IsPersistent(asset))
                {
                    var newPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    if (newPath.Length > 0 && AssetDatabase.CopyAsset(assetPath, newPath))
                        copiedPaths.Add(newPath);
                }
            }

            if (firstDuplicatedObjectToFail != null)
            {
                var errString = string.Format(
                    "Duplication error: One or more sub assets (with types of {0}) can not be duplicated directly, use the appropriate editor instead",
                    firstDuplicatedObjectToFail.GetType().Name
                );

                Debug.LogError(errString, firstDuplicatedObjectToFail);
            }

            return copiedPaths.Select(AssetDatabase.LoadMainAssetAtPath);
        }

        internal static IEnumerable<Object> DuplicateAssets(IEnumerable<int> instanceIDs)
        {
            return DuplicateAssets(instanceIDs.Select(id => EditorUtility.InstanceIDToObject(id)));
        }

        internal static IEnumerable<string> GetMainPathsOfAssets(IEnumerable<int> instanceIDs)
        {
            foreach (var instanceID in instanceIDs)
            {
                if (AssetDatabase.IsMainAsset(instanceID))
                {
                    yield return AssetDatabase.GetAssetPath(instanceID);
                }
            }
        }
    }
}
