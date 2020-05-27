// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    internal interface IPreviewable
    {
        void Initialize(UnityObject[] targets);

        UnityObject target { get; }
        bool MoveNextTarget();
        void ResetTarget();

        bool HasPreviewGUI();
        GUIContent GetPreviewTitle();
        void DrawPreview(Rect previewArea);
        void OnPreviewGUI(Rect r, GUIStyle background);
        void OnInteractivePreviewGUI(Rect r, GUIStyle background);
        void OnPreviewSettings();
        string GetInfoString();

        void ReloadPreviewInstances();
    }

    public class ObjectPreview : IPreviewable
    {
        static class Styles
        {
            public static readonly GUIStyle preBackground = "PreBackground";
            public static readonly GUIStyle preBackgroundSolid = "PreBackgroundSolid";
            public static readonly GUIStyle previewMiniLabel = "PreMiniLabel";
            public static readonly GUIStyle dropShadowLabelStyle = "PreOverlayLabel";
        }

        const int kPreviewLabelHeight = 12;
        const int kPreviewMinSize = 55;
        const int kGridTargetCount = 25;
        const int kGridSpacing = 10;
        const int kPreviewLabelPadding = 5;

        protected UnityObject[] m_Targets;
        protected int m_ReferenceTargetIndex;

        public virtual void Initialize(UnityObject[] targets)
        {
            m_ReferenceTargetIndex = 0;
            m_Targets = targets;
        }

        public virtual bool MoveNextTarget()
        {
            m_ReferenceTargetIndex++;

            return (m_ReferenceTargetIndex < m_Targets.Length - 1);
        }

        public virtual void ResetTarget()
        {
            m_ReferenceTargetIndex = 0;
        }

        public virtual UnityObject target
        {
            get
            {
                return m_Targets[m_ReferenceTargetIndex];
            }
        }

        public virtual bool HasPreviewGUI()
        {
            return false;
        }

        public virtual GUIContent GetPreviewTitle()
        {
            GUIContent guiContent = new GUIContent();
            if (m_Targets.Length == 1)
                guiContent.text = target.name;
            else
            {
                guiContent.text = m_Targets.Length + " ";
                if (NativeClassExtensionUtilities.ExtendsANativeType(target))
                    guiContent.text += MonoScript.FromScriptedObject(target).GetClass().Name;
                else
                    guiContent.text += ObjectNames.NicifyVariableName(ObjectNames.GetClassName(target));

                guiContent.text += "s";
            }

            return guiContent;
        }

        public virtual void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);
        }

        public virtual void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            OnPreviewGUI(r, background);
        }

        public virtual void OnPreviewSettings()
        {
        }

        public virtual string GetInfoString()
        {
            return "";
        }

        public void DrawPreview(Rect previewArea)
        {
            DrawPreview(this, previewArea, m_Targets);
        }

        public virtual void ReloadPreviewInstances()
        {
        }

        internal static void DrawPreview(IPreviewable defaultPreview, Rect previewArea, UnityObject[] targets)
        {
            string text = string.Empty;
            Event evt = Event.current;

            // If multiple targets, draw a grid of previews
            if (targets.Length > 1)
            {
                // Draw the previews inside the region of the background that's solid colored
                Rect previewPositionInner = new RectOffset(16, 16, 20, 25).Remove(previewArea);

                // Number of previews to aim at
                int maxRows = Mathf.Max(1, Mathf.FloorToInt((previewPositionInner.height + kGridSpacing) / (kPreviewMinSize + kGridSpacing + kPreviewLabelHeight)));
                int maxCols = Mathf.Max(1, Mathf.FloorToInt((previewPositionInner.width + kGridSpacing) / (kPreviewMinSize + kGridSpacing)));
                int countWithMinimumSize = maxRows * maxCols;
                int neededCount = Mathf.Min(targets.Length, kGridTargetCount);

                // Get number of columns and rows
                bool fixedSize = true;
                int[] division = new int[2] { maxCols, maxRows };
                if (neededCount < countWithMinimumSize)
                {
                    division = GetGridDivision(previewPositionInner, neededCount, kPreviewLabelHeight);
                    fixedSize = false;
                }

                // The available cells in the grid may be slightly higher than what was aimed at.
                // If the number of targets is also higher, we might as well fill in the remaining cells.
                int count = Mathf.Min(division[0] * division[1], targets.Length);

                // Calculations become simpler if we add one spacing to the width and height,
                // so there is the same number of spaces and previews.
                previewPositionInner.width += kGridSpacing;
                previewPositionInner.height += kGridSpacing;

                Vector2 cellSize = new Vector2(
                    Mathf.FloorToInt(previewPositionInner.width / division[0] - kGridSpacing),
                    Mathf.FloorToInt(previewPositionInner.height / division[1] - kGridSpacing)
                );
                float previewSize = Mathf.Min(cellSize.x, cellSize.y - kPreviewLabelHeight);
                if (fixedSize)
                    previewSize = Mathf.Min(previewSize, kPreviewMinSize);

                bool selectingOne = (evt.type == EventType.MouseDown && evt.button == 0 && evt.clickCount == 2 &&
                    previewArea.Contains(evt.mousePosition));

                defaultPreview.ResetTarget();
                for (int i = 0; i < count; i++)
                {
                    Rect r = new Rect(
                        previewPositionInner.x + (i % division[0]) * previewPositionInner.width / division[0],
                        previewPositionInner.y + (i / division[0]) * previewPositionInner.height / division[1],
                        cellSize.x,
                        cellSize.y
                    );

                    if (selectingOne && r.Contains(Event.current.mousePosition))
                    {
                        if (defaultPreview.target is AssetImporter)
                            // The new selection should be the asset itself, not the importer
                            Selection.objects = new[] { AssetDatabase.LoadAssetAtPath<UnityObject>(((AssetImporter)defaultPreview.target).assetPath)};
                        else
                            Selection.objects = new UnityObject[] {defaultPreview.target};
                    }

                    // Make room for label underneath
                    r.height -= kPreviewLabelHeight;
                    // Make preview square
                    Rect rSquare = new Rect(r.x + (r.width - previewSize) * 0.5f, r.y + (r.height - previewSize) * 0.5f, previewSize, previewSize);

                    // Draw preview inside a group to prevent overdraw
                    // @TODO: Make style with solid color that doesn't have overdraw
                    GUI.BeginGroup(rSquare);
                    Editor.m_AllowMultiObjectAccess = false;
                    defaultPreview.OnInteractivePreviewGUI(new Rect(0, 0, previewSize, previewSize), Styles.preBackgroundSolid);
                    Editor.m_AllowMultiObjectAccess = true;
                    GUI.EndGroup();

                    // Draw the name of the object
                    r.y = rSquare.yMax;
                    r.height = 16;
                    GUI.Label(r, targets[i].name, Styles.previewMiniLabel);
                    defaultPreview.MoveNextTarget();
                }
                defaultPreview.ResetTarget();  // Remember to reset referenceTargetIndex to prevent following calls to 'editor.target' will return a different target which breaks all sorts of places. Fix for case 600235

                if (Event.current.type == EventType.Repaint)
                    text = string.Format("Previewing {0} of {1} Objects", count, targets.Length);
            }
            // If only a single target, just draw that one
            else
            {
                defaultPreview.OnInteractivePreviewGUI(previewArea, Styles.preBackground);

                if (Event.current.type == EventType.Repaint)
                {
                    // TODO: This should probably be calculated during import and stored together with the asset somehow. Or maybe not. Not sure, really...
                    text = defaultPreview.GetInfoString();
                    if (text != string.Empty)
                    {
                        text = text.Replace("\n", "   ");
                        text = string.Format("{0}\n{1}", defaultPreview.target.name, text);
                    }
                }
            }

            // Draw the asset info.
            if (Event.current.type == EventType.Repaint && text != string.Empty)
            {
                var textHeight = Styles.dropShadowLabelStyle.CalcHeight(GUIContent.Temp(text), previewArea.width);
                EditorGUI.DropShadowLabel(new Rect(previewArea.x, previewArea.yMax - textHeight - kPreviewLabelPadding, previewArea.width, textHeight), text);
            }
        }

        // Get the number or columns and rows for a grid with a certain minimum number of cells
        // such that the cells are as close to square as possible.
        private static int[] GetGridDivision(Rect rect, int minimumNr, int labelHeight)
        {
            // The edge size of a square calculated based on area
            float approxSize = Mathf.Sqrt(rect.width * rect.height / minimumNr);
            int xCount = Mathf.FloorToInt(rect.width / approxSize);
            int yCount = Mathf.FloorToInt(rect.height / (approxSize + labelHeight));
            // This heuristic is not entirely optimal and could probably be improved
            while (xCount * yCount < minimumNr)
            {
                float ratioIfXInc = AbsRatioDiff((xCount + 1) / rect.width, yCount / (rect.height - yCount * labelHeight));
                float ratioIfYInc = AbsRatioDiff(xCount / rect.width, (yCount + 1) / (rect.height - (yCount + 1) * labelHeight));
                if (ratioIfXInc < ratioIfYInc)
                {
                    xCount++;
                    if (xCount * yCount > minimumNr)
                        yCount = Mathf.CeilToInt((float)minimumNr / xCount);
                }
                else
                {
                    yCount++;
                    if (xCount * yCount > minimumNr)
                        xCount = Mathf.CeilToInt((float)minimumNr / yCount);
                }
            }
            return new int[] { xCount, yCount };
        }

        private static float AbsRatioDiff(float x, float y)
        {
            return Mathf.Max(x / y, y / x);
        }
    }

    internal interface IToolModeOwner
    {
        bool areToolModesAvailable { get; }
        int GetInstanceID();
        Bounds GetWorldBoundsOfTargets();
        bool ModeSurvivesSelectionChange(int toolMode);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditorForRenderPipelineAttribute : CustomEditor
    {
        internal Type renderPipelineType;

        public CustomEditorForRenderPipelineAttribute(Type inspectedType, Type renderPipeline) : base(inspectedType)
        {
            renderPipelineType = renderPipeline;
        }

        public CustomEditorForRenderPipelineAttribute(Type inspectedType, Type renderPipeline, bool editorForChildClasses) : base(inspectedType, editorForChildClasses)
        {
            renderPipelineType = renderPipeline;
        }
    }

    public sealed partial class CanEditMultipleObjects : System.Attribute {}

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class CachePropertyAttribute : System.Attribute
    {
        public string propertyPath { get; }

        public CachePropertyAttribute()
        {
            propertyPath = null;
        }

        public CachePropertyAttribute(string propertyPath)
        {
            this.propertyPath = propertyPath;
        }
    }

    // Base class to derive custom Editors from. Use this to create your own custom inspectors and editors for your objects.
    [ExcludeFromObjectFactory]
    public partial class Editor : ScriptableObject, IPreviewable, IToolModeOwner
    {
        //If you modify the members of the Editor class, please keep in mind
        //that you need to keep the c++ struct MonoInspectorData in sync.
        //Last time this struct could be found at: Editor\src\Utility\CreateEditor.cpp

        // The object currently inspected by this editor.
        UnityObject[] m_Targets;
        // The context object with which this Editor was created
        internal UnityObject m_Context;
        // Note that m_Dirty is not only set through 'isInspectorDirty' but also from C++ in 'SetCustomEditorIsDirty (MonoBehaviour* inspector, bool dirty)'
        int m_IsDirty;
        int m_ReferenceTargetIndex = 0;
        PropertyHandlerCache m_PropertyHandlerCache = new PropertyHandlerCache();
        IPreviewable m_DummyPreview;
        AudioFilterGUI m_AudioFilterGUI;

        internal SerializedObject m_SerializedObject = null;
        internal SerializedProperty m_EnabledProperty = null;
        private InspectorMode m_InspectorMode = InspectorMode.Normal;
        internal InspectorMode inspectorMode
        {
            get
            {
                return m_InspectorMode;
            }
            set
            {
                if (m_InspectorMode != value)
                {
                    m_InspectorMode = value;
                    m_SerializedObject = null;
                    m_EnabledProperty = null;
                }
            }
        }


        internal static float kLineHeight = EditorGUI.kSingleLineHeight;

        internal bool hideInspector = false;

        const float kImageSectionWidth = 44;
        internal const float k_WideModeMinWidth = 330f;
        internal const float k_HeaderHeight = 21f;

        internal delegate void OnEditorGUIDelegate(Editor editor, Rect drawRect);
        internal static OnEditorGUIDelegate OnPostIconGUI = null;

        internal static bool m_AllowMultiObjectAccess = true;

        // used internally to know if this the first editor in the inspector window
        internal bool firstInspectedEditor { get; set; }

        internal IPropertyView propertyViewer { get; set; }

        internal virtual bool HasLargeHeader()
        {
            return AssetDatabase.IsMainAsset(target) || AssetDatabase.IsSubAsset(target);
        }

        internal bool canEditMultipleObjects
        {
            get { return GetType().GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0; }
        }

        internal virtual IPreviewable preview
        {
            get
            {
                if (m_DummyPreview == null)
                {
                    m_DummyPreview = new ObjectPreview();
                    m_DummyPreview.Initialize(targets);
                }
                return m_DummyPreview;
            }
        }

        internal PropertyHandlerCache propertyHandlerCache
        {
            get { return m_PropertyHandlerCache; }
        }

        static class BaseStyles
        {
            public static readonly GUIContent open = EditorGUIUtility.TrTextContent("Open");
            public static readonly GUIStyle inspectorBig = new GUIStyle(EditorStyles.inspectorBig);
            public static readonly GUIStyle centerStyle = new GUIStyle();
            public static readonly GUIStyle postLargeHeaderBackground = "IN BigTitle Post";

            static BaseStyles()
            {
                centerStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        bool IToolModeOwner.areToolModesAvailable
        {
            get
            {
                // tool modes not available when the target is a prefab parent
                return !EditorUtility.IsPersistent(target);
            }
        }

        // The object being inspected.
        public UnityObject target { get { return m_Targets[referenceTargetIndex]; } set { throw new InvalidOperationException("You can't set the target on an editor."); } }

        // An array of all the object being inspected.
        public UnityObject[] targets
        {
            get
            {
                if (!m_AllowMultiObjectAccess)
                    Debug.LogError("The targets array should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead.");
                return m_Targets;
            }
        }

        internal virtual int referenceTargetIndex
        {
            get { return Mathf.Clamp(m_ReferenceTargetIndex, 0, m_Targets.Length - 1); }
            // Modulus that works for negative numbers as well
            set { m_ReferenceTargetIndex = (Math.Abs(value * m_Targets.Length) + value) % m_Targets.Length; }
        }

        internal virtual string targetTitle
        {
            get
            {
                if (m_Targets.Length == 1 || !m_AllowMultiObjectAccess)
                    return ObjectNames.GetInspectorTitle(target);
                else
                    return m_Targets.Length + " " + ObjectNames.NicifyVariableName(ObjectNames.GetTypeName(target)) + "s";
            }
        }

        // A [[SerializedObject]] representing the object or objects being inspected.
        public SerializedObject serializedObject
        {
            get
            {
                if (!m_AllowMultiObjectAccess)
                    Debug.LogError("The serializedObject should not be used inside OnSceneGUI or OnPreviewGUI. Use the target property directly instead.");
                return GetSerializedObjectInternal();
            }
        }

        internal SerializedProperty enabledProperty
        {
            get
            {
                GetSerializedObjectInternal();
                return m_EnabledProperty;
            }
        }

        internal bool isInspectorDirty
        {
            get { return m_IsDirty != 0; }
            set { m_IsDirty = value ? 1 : 0; }
        }

        public static Editor CreateEditorWithContext(UnityObject[] targetObjects, UnityObject context, [DefaultValue("null")] Type editorType)
        {
            if (editorType != null && !editorType.IsSubclassOf(typeof(Editor)))
                throw new ArgumentException($"Editor type '{editorType}' does not derive from UnityEditor.Editor", "editorType");

            return CreateEditorWithContextInternal(targetObjects, context, editorType);
        }

        [ExcludeFromDocs]
        public static Editor CreateEditorWithContext(UnityObject[] targetObjects, UnityObject context)
        {
            Type editorType = null;
            return CreateEditorWithContext(targetObjects, context, editorType);
        }

        public static void CreateCachedEditorWithContext(UnityObject targetObject, UnityObject context, Type editorType, ref Editor previousEditor)
        {
            CreateCachedEditorWithContext(new[] {targetObject}, context, editorType, ref previousEditor);
        }

        public static void CreateCachedEditorWithContext(UnityObject[] targetObjects, UnityObject context, Type editorType, ref Editor previousEditor)
        {
            if (previousEditor != null && ArrayUtility.ArrayEquals(previousEditor.m_Targets, targetObjects) && previousEditor.m_Context == context)
                return;

            if (previousEditor != null)
                DestroyImmediate(previousEditor);
            previousEditor = CreateEditorWithContext(targetObjects, context, editorType);
        }

        public static void CreateCachedEditor(UnityObject targetObject, Type editorType, ref Editor previousEditor)
        {
            CreateCachedEditorWithContext(new[] {targetObject}, null, editorType, ref previousEditor);
        }

        public static void CreateCachedEditor(UnityObject[] targetObjects, Type editorType, ref Editor previousEditor)
        {
            CreateCachedEditorWithContext(targetObjects, null, editorType, ref previousEditor);
        }

        [ExcludeFromDocs]
        public static Editor CreateEditor(UnityObject targetObject)
        {
            Type editorType = null;
            return CreateEditor(targetObject, editorType);
        }

        public static Editor CreateEditor(UnityObject targetObject, [DefaultValue("null")]  Type editorType)
        {
            return CreateEditorWithContext(new[] {targetObject}, null, editorType);
        }

        [ExcludeFromDocs]
        public static Editor CreateEditor(UnityObject[] targetObjects)
        {
            Type editorType = null;
            return CreateEditor(targetObjects, editorType);
        }

        public static Editor CreateEditor(UnityObject[] targetObjects, [DefaultValue("null")]  Type editorType)
        {
            return CreateEditorWithContext(targetObjects, null, editorType);
        }

        internal void CleanupPropertyEditor()
        {
            if (m_SerializedObject != null)
            {
                m_SerializedObject.Dispose();
                m_SerializedObject = null;
            }
        }

        private void OnDisableINTERNAL()
        {
            CleanupPropertyEditor();
        }

        internal virtual SerializedObject GetSerializedObjectInternal()
        {
            if (m_SerializedObject == null)
            {
                CreateSerializedObject();
            }

            return m_SerializedObject;
        }

        internal class SerializedObjectNotCreatableException : Exception
        {
            public SerializedObjectNotCreatableException(string msg) : base(msg) {}
        }

        private void CreateSerializedObject()
        {
            try
            {
                m_SerializedObject = new SerializedObject(targets, m_Context);
                m_SerializedObject.inspectorMode = inspectorMode;
                AssignCachedProperties(this, m_SerializedObject.GetIterator());
                m_EnabledProperty = m_SerializedObject.FindProperty("m_Enabled");
            }
            catch (ArgumentException e)
            {
                m_SerializedObject = null;
                m_EnabledProperty = null;
                throw new SerializedObjectNotCreatableException(e.Message);
            }
        }

        internal static void AssignCachedProperties<T>(T self, SerializedProperty root) where T : class
        {
            var fields = ScriptAttributeUtility.GetAutoLoadProperties(typeof(T));
            if (fields.Count == 0)
                return;

            var properties = new Dictionary<string, FieldInfo>(fields.Count);
            var allParents = new HashSet<string>();
            foreach (var fieldInfo in fields)
            {
                var attribute = (CachePropertyAttribute)fieldInfo.GetCustomAttributes(typeof(CachePropertyAttribute), false).First();
                var propertyName = string.IsNullOrEmpty(attribute.propertyPath) ? fieldInfo.Name : attribute.propertyPath;
                properties.Add(propertyName, fieldInfo);
                int dot = propertyName.LastIndexOf('.');
                while (dot != -1)
                {
                    propertyName = propertyName.Substring(0, dot);
                    if (!allParents.Add(propertyName))
                        break;
                    dot = propertyName.LastIndexOf('.');
                }
            }

            var parentPath = root.propertyPath;
            var parentPathLength = parentPath.Length > 0 ? parentPath.Length + 1 : 0;
            var exitCount = properties.Count;
            var iterator = root.Copy();
            bool enterChildren = true;
            while (iterator.Next(enterChildren) && exitCount > 0)
            {
                FieldInfo fieldInfo;
                var propertyPath = iterator.propertyPath.Substring(parentPathLength);
                if (properties.TryGetValue(propertyPath, out fieldInfo))
                {
                    fieldInfo.SetValue(self, iterator.Copy());
                    properties.Remove(propertyPath);
                    exitCount--;
                }

                enterChildren = allParents.Contains(propertyPath);
            }
            iterator.Dispose();
            if (exitCount > 0)
            {
                Debug.LogWarning("The following properties registered with CacheProperty where not found during the inspector creation: " + string.Join(", ", properties.Keys.ToArray()));
            }
        }

        internal virtual void InternalSetTargets(UnityObject[] t) { m_Targets = t; }
        internal void InternalSetHidden(bool hidden) { hideInspector = hidden; }
        internal void InternalSetContextObject(UnityObject context) { m_Context = context; }

        Bounds IToolModeOwner.GetWorldBoundsOfTargets()
        {
            var result = new Bounds();
            bool initialized = false;

            foreach (var t in targets)
            {
                if (t == null)
                    continue;

                Bounds targetBounds = GetWorldBoundsOfTarget(t);

                if (!initialized)
                    result = targetBounds;
                result.Encapsulate(targetBounds);

                initialized = true;
            }

            return result;
        }

        internal virtual Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            return targetObject is Component ? ((Component)targetObject).gameObject.CalculateBounds() : new Bounds();
        }

        bool IToolModeOwner.ModeSurvivesSelectionChange(int toolMode)
        {
            return false;
        }

        // Reload SerializedObject because flags etc might have changed.
        internal virtual void OnForceReloadInspector()
        {
            if (m_SerializedObject != null)
            {
                m_SerializedObject.SetIsDifferentCacheDirty();
                // Need to make sure internal target list PPtr have been updated from a native memory
                // When assets are reloaded they are destroyed and recreated and the managed list does not get updated
                // The m_SerializedObject is a native object thus its targetObjects is a native memory PPtr list which have the new PPtr ids.
                InternalSetTargets(m_SerializedObject.targetObjects);
            }
        }

        internal virtual bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out float height)
        {
            height = -1;
            return false;
        }

        internal virtual bool OnOptimizedInspectorGUI(Rect contentRect)
        {
            Debug.LogError("Not supported");
            return false;
        }

        protected internal static void DrawPropertiesExcluding(SerializedObject obj, params string[] propertyToExclude)
        {
            // Loop through properties and create one field (including children) for each top level property.
            SerializedProperty property = obj.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                expanded = false;

                if (propertyToExclude.Contains(property.name))
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }
        }

        // Draw the built-in inspector.
        public bool DrawDefaultInspector()
        {
            return DoDrawDefaultInspector();
        }

        internal static bool DoDrawDefaultInspector(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.UpdateIfRequiredOrScript();

            // Loop through properties and create one field (including children) for each top level property.
            SerializedProperty property = obj.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                using (new EditorGUI.DisabledScope("m_Script" == property.propertyPath))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
                expanded = false;
            }

            obj.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        internal bool DoDrawDefaultInspector()
        {
            bool res;
            using (new LocalizationGroup(target))
            {
                res = DoDrawDefaultInspector(serializedObject);

                var behaviour = target as MonoBehaviour;
                if (behaviour == null || !AudioUtil.HasAudioCallback(behaviour) || AudioUtil.GetCustomFilterChannelCount(behaviour) <= 0)
                    return res;

                // If we have an OnAudioFilterRead callback, draw vu meter
                if (m_AudioFilterGUI == null)
                    m_AudioFilterGUI = new AudioFilterGUI();
                m_AudioFilterGUI.DrawAudioFilterGUI(behaviour);
            }
            return res;
        }

        // Repaint any inspectors that shows this editor.
        public void Repaint()
        {
            if (propertyViewer != null)
                propertyViewer.Repaint();
            else
                InspectorWindow.RepaintAllInspectors();
        }

        // Implement this function to make a custom IMGUI inspector.
        public virtual void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        // Implement this function to make a custom UIElements inspector.
        public virtual VisualElement CreateInspectorGUI()
        {
            return null;
        }

        // Implement this function if you want your editor constantly repaint (every 33ms)
        public virtual bool RequiresConstantRepaint()
        {
            return false;
        }

        public static event Action<Editor> finishedDefaultHeaderGUI = null;

        // This is the method that should be called from externally e.g. myEditor.DrawHeader ();
        // Do not make this method virtual - override OnHeaderGUI instead.
        public void DrawHeader()
        {
            // If we call DrawHeader from inside an an editor's OnInspectorGUI call, we have to do special handling.
            // (See DrawHeaderFromInsideHierarchy for details.)
            // We know we're inside the OnInspectorGUI block (or a similar vertical block) if hierarchyMode is set to true.
            var hierarchyMode = EditorGUIUtility.hierarchyMode;
            if (hierarchyMode)
                DrawHeaderFromInsideHierarchy();
            else
                OnHeaderGUI();

            if (finishedDefaultHeaderGUI != null)
            {
                // see DrawHeaderFromInsideHierarchy()
                if (hierarchyMode)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(GUILayoutUtility.topLevel.style);
                }
                // reset label and field widths to defaults
                EditorGUIUtility.labelWidth = 0f;
                EditorGUIUtility.fieldWidth = 0f;

                GUILayout.Space(
                    -1f                                          // move up to cover up bottom pixel of header box
                    - BaseStyles.inspectorBig.margin.bottom
                    - BaseStyles.inspectorBig.padding.bottom
                    - BaseStyles.inspectorBig.overflow.bottom    // move up to bottom of content area in header
                );

                // align with controls in the Inspector
                // see InspectorWindow.DrawEditor() before calls to OnOptimizedInspectorGUI()/OnInspectorGUI()
                EditorGUIUtility.hierarchyMode = true;
                EditorGUIUtility.wideMode = EditorGUIUtility.contextWidth > k_WideModeMinWidth;
                EditorGUILayout.BeginVertical(BaseStyles.postLargeHeaderBackground, GUILayout.ExpandWidth(true));
                finishedDefaultHeaderGUI(this);
                EditorGUILayout.EndVertical();
                if (hierarchyMode)
                {
                    EditorGUILayout.EndVertical();
                    // see InspectorWindow.DoOnInspectorGUI()
                    EditorGUILayout.BeginVertical(UseDefaultMargins() ? EditorStyles.inspectorDefaultMargins : GUIStyle.none);
                }
            }
        }

        // This is the method to override to create custom header GUI.
        // Do not make this method internal or public - call DrawHeader instead.
        protected virtual void OnHeaderGUI()
        {
            DrawHeaderGUI(this, targetTitle);
        }

        internal virtual void OnHeaderControlsGUI()
        {
            // Ensure we take up the same amount of height as regular controls
            GUILayoutUtility.GetRect(10, 10, 16, 16, EditorStyles.layerMaskField);

            GUILayout.FlexibleSpace();

            bool showOpenButton = true;
            var importerEditor = this as AssetImporterEditor;
            // only show open button for the main object of an asset and for AssetImportInProgressProxy (asset not yet imported)
            if (importerEditor == null && !(targets[0] is AssetImportInProgressProxy))
            {
                var assetPath = AssetDatabase.GetAssetPath(targets[0]);
                // Don't show open button if the target is not an asset
                if (!AssetDatabase.IsMainAsset(targets[0]))
                    showOpenButton = false;
                // Don't show open button if the target has an importer
                // (but ignore AssetImporters since they're not shown)
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer && importer.GetType() != typeof(AssetImporter))
                    showOpenButton = false;
            }

            if (showOpenButton && !ShouldHideOpenButton())
            {
                var assets = importerEditor != null ? importerEditor.assetTargets : targets;
                var disabled = importerEditor != null && importerEditor.assetTarget == null;
                ShowOpenButton(assets, !disabled);
            }
        }

        internal void ShowOpenButton(UnityObject[] assets, bool enableCondition = true)
        {
            bool previousGUIState = GUI.enabled;
            GUI.enabled = enableCondition;

            if (GUILayout.Button(BaseStyles.open, EditorStyles.miniButton))
            {
                if (AssetDatabase.MakeEditable(
                    assets.Select(AssetDatabase.GetAssetPath).ToArray(),
                    "Do you want to check out this file or files?"))
                {
                    AssetDatabase.OpenAsset(assets);
                    GUIUtility.ExitGUI();
                }
            }

            GUI.enabled = previousGUIState;
        }

        protected virtual bool ShouldHideOpenButton()
        {
            return false;
        }

        internal virtual void OnHeaderIconGUI(Rect iconRect)
        {
            Texture2D icon = null;

            //  Fetch isLoadingAssetPreview to ensure that there is no situation where a preview needs a repaint because it hasn't finished loading yet.
            bool isLoadingAssetPreview = AssetPreview.IsLoadingAssetPreview(target.GetInstanceID());
            icon = AssetPreview.GetAssetPreview(target);
            if (!icon)
            {
                // We have a static preview it just hasn't been loaded yet. Repaint until we have it loaded.
                if (isLoadingAssetPreview)
                    Repaint();
                icon = AssetPreview.GetMiniThumbnail(target);
            }

            GUI.Label(iconRect, icon, BaseStyles.centerStyle);
        }

        internal virtual void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            titleRect.yMin -= 2;
            titleRect.yMax += 2;
            GUI.Label(titleRect, header, EditorStyles.largeLabel);
        }

        // Draws the help and settings part of the header.
        // Returns a Rect to know where to draw the rest of the header.
        internal virtual Rect DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            // Help
            var settingsSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);

            float currentOffset = settingsSize.x;

            const int kTopMargin = 5;
            // Settings; process event even for disabled UI
            Rect settingsRect = new Rect(r.xMax - currentOffset, r.y + kTopMargin, settingsSize.x, settingsSize.y);
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;
            var showMenu = EditorGUI.DropdownButton(settingsRect, GUIContent.none, FocusType.Passive,
                EditorStyles.optionsButtonStyle);
            GUI.enabled = wasEnabled;
            if (showMenu)
            {
                EditorUtility.DisplayObjectContextMenu(settingsRect, targets, 0);
            }

            currentOffset += settingsSize.x;

            // Show Editor Header Items.
            return EditorGUIUtility.DrawEditorHeaderItems(new Rect(r.xMax - currentOffset, r.y + kTopMargin, settingsSize.x, settingsSize.y), targets);
        }

        // If we call DrawHeaderGUI from inside an an editor's OnInspectorGUI call, we have to do special handling.
        // Since OnInspectorGUI is wrapped inside a BeginVertical/EndVertical block that adds padding,
        // and we don't want this padding for headers, we have to stop the vertical block,
        // draw the header, and then start a new vertical block with the same style.
        private void DrawHeaderFromInsideHierarchy()
        {
            var style = GUILayoutUtility.topLevel.style;
            EditorGUILayout.EndVertical();
            OnHeaderGUI();
            EditorGUILayout.BeginVertical(style);
        }

        internal static Rect DrawHeaderGUI(Editor editor, string header)
        {
            return DrawHeaderGUI(editor, header, 0f);
        }

        internal static Rect DrawHeaderGUI(Editor editor, string header, float leftMargin)
        {
            GUILayout.BeginHorizontal(BaseStyles.inspectorBig);
            GUILayout.Space(kImageSectionWidth - 6);
            GUILayout.BeginVertical();
            GUILayout.Space(k_HeaderHeight);
            GUILayout.BeginHorizontal();
            if (leftMargin > 0f)
                GUILayout.Space(leftMargin);
            if (editor)
                editor.OnHeaderControlsGUI();
            else
                EditorGUILayout.GetControlRect();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            Rect fullRect = GUILayoutUtility.GetLastRect();

            // Content rect
            Rect r = new Rect(fullRect.x + leftMargin, fullRect.y, fullRect.width - leftMargin, fullRect.height);

            // Icon
            Rect iconRect = new Rect(r.x + 6, r.y + 6, 32, 32);

            if (editor)
                editor.OnHeaderIconGUI(iconRect);
            else
                GUI.Label(iconRect, AssetPreview.GetMiniTypeThumbnail(typeof(UnityObject)), BaseStyles.centerStyle);

            if (editor)
                editor.DrawPostIconContent(iconRect);

            // Help and Settings
            Rect titleRect;
            var titleHeight = EditorGUI.lineHeight;
            if (editor)
            {
                Rect helpAndSettingsRect = editor.DrawHeaderHelpAndSettingsGUI(r);
                float rectX = r.x + kImageSectionWidth;
                titleRect = new Rect(rectX, r.y + 6, (helpAndSettingsRect.x - rectX) - 4, titleHeight);
            }
            else
                titleRect = new Rect(r.x + kImageSectionWidth, r.y + 6, r.width - kImageSectionWidth, titleHeight);

            // Title
            if (editor)
                editor.OnHeaderTitleGUI(titleRect, header);
            else
                GUI.Label(titleRect, header, EditorStyles.largeLabel);

            // Context Menu; process event even for disabled UI
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;
            Event evt = Event.current;
            var showMenu = editor != null && evt.type == EventType.MouseDown && evt.button == 1 && r.Contains(evt.mousePosition);
            GUI.enabled = wasEnabled;

            if (showMenu)
            {
                EditorUtility.DisplayObjectContextMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), editor.targets, 0);
                evt.Use();
            }

            return fullRect;
        }

        internal void DrawPostIconContent(Rect iconRect)
        {
            if (OnPostIconGUI != null)
            {
                // Post icon draws 16 x 16 at bottom right corner
                const float k_Size = 16;
                Rect drawRect = iconRect;
                drawRect.x = (drawRect.xMax - k_Size) + 4; // Move slightly outside bounds for overlap effect.
                drawRect.y = (drawRect.yMax - k_Size) + 1;
                drawRect.width = k_Size;
                drawRect.height = k_Size;
                OnPostIconGUI(this, drawRect);
            }
        }

        internal void DrawPostIconContent()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Rect iconRect = GUILayoutUtility.GetLastRect();
                DrawPostIconContent(iconRect);
            }
        }

        public static void DrawFoldoutInspector(UnityObject target, ref Editor editor)
        {
            if (editor != null && (editor.target != target || target == null))
            {
                UnityObject.DestroyImmediate(editor);
                editor = null;
            }

            if (editor == null && target != null)
                editor = Editor.CreateEditor(target);

            if (editor == null)
                return;

            const float kSpaceForFoldoutArrow = 10f;
            Rect titleRect = Editor.DrawHeaderGUI(editor, editor.targetTitle, kSpaceForFoldoutArrow);
            int id = GUIUtility.GetControlID(45678, FocusType.Passive);

            Rect renderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(titleRect);
            renderRect.y = titleRect.yMax - 17f; // align with bottom
            bool oldVisible = UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(target);
            bool newVisible = EditorGUI.DoObjectFoldout(oldVisible, titleRect, renderRect, editor.targets, id);

            if (newVisible != oldVisible)
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(target, newVisible);

            if (newVisible)
                editor.OnInspectorGUI();
        }

        // Override this method in subclasses if you implement OnPreviewGUI.
        public virtual bool HasPreviewGUI()
        {
            return preview.HasPreviewGUI();
        }

        // Override this method if you want to change the label of the Preview area.
        public virtual GUIContent GetPreviewTitle()
        {
            return preview.GetPreviewTitle();
        }

        // Override this method if you want to render a static preview that shows.
        public virtual Texture2D RenderStaticPreview(string assetPath, UnityObject[] subAssets, int width, int height)
        {
            return null;
        }

        // Implement to create your own custom preview. Custom previews are used in the preview area of the inspector, primary editor headers, and the object selector.
        public virtual void OnPreviewGUI(Rect r, GUIStyle background)
        {
            preview.OnPreviewGUI(r, background);
        }

        // Implement to create your own interactive custom preview. Interactive custom previews are used in the preview area of the inspector and the object selector.
        public virtual void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            OnPreviewGUI(r, background);
        }

        // Override this method if you want to show custom controls in the preview header.
        public virtual void OnPreviewSettings()
        {
            preview.OnPreviewSettings();
        }

        // Implement this method to show asset information on top of the asset preview.
        public virtual string GetInfoString()
        {
            return preview.GetInfoString();
        }

        public virtual void DrawPreview(Rect previewArea)
        {
            ObjectPreview.DrawPreview(this, previewArea, targets);
        }

        public virtual void ReloadPreviewInstances()
        {
            preview.ReloadPreviewInstances();
        }

        // Some custom editors manually display SerializedObjects with only private properties
        // Setting this to true allows them to properly toggling the visibility via the standard header foldout
        internal bool alwaysAllowExpansion {get; set;}

        // Auxiliary method that determines whether this editor has a set of public properties and, as thus,
        // can be expanded via a foldout. This is used in order to determine whether a foldout needs to be
        // rendered on top of the inspector title bar or not. Some examples of editors that don't require
        // a foldout are GUI Layer and Audio Listener.
        internal bool CanBeExpandedViaAFoldout()
        {
            if (alwaysAllowExpansion)
                return true;

            if (m_SerializedObject == null)
                CreateSerializedObject();
            else
                m_SerializedObject.Update();
            m_SerializedObject.inspectorMode = inspectorMode;

            return CanBeExpandedViaAFoldoutWithoutUpdate();
        }

        internal bool CanBeExpandedViaAFoldoutWithoutUpdate()
        {
            if (m_SerializedObject == null)
                CreateSerializedObject();
            SerializedProperty property = m_SerializedObject.GetIterator();

            bool analyzePropertyChildren = true;
            while (property.NextVisible(analyzePropertyChildren))
            {
                if (EditorGUI.GetPropertyHeight(property, null, true) > 0)
                {
                    return true;
                }
                analyzePropertyChildren = false;
            }

            return false;
        }

        static internal bool IsAppropriateFileOpenForEdit(UnityObject assetObject)
        {
            // The native object for a ScriptableObject with an invalid script will be considered not alive.
            // In order to allow editing of the m_Script property of a ScriptableObject with an invalid script
            // we use the instance ID instead of the UnityEngine.Object reference to check if the asset is open for edit.

            if ((object)assetObject == null)
                return false;

            var instanceID = assetObject.GetInstanceID();
            if (instanceID == 0)
                return false;

            StatusQueryOptions opts = EditorUserSettings.allowAsyncStatusUpdate ? StatusQueryOptions.UseCachedAsync : StatusQueryOptions.UseCachedIfPossible;
            if (AssetDatabase.IsNativeAsset(instanceID))
            {
                var assetPath = AssetDatabase.GetAssetPath(instanceID);
                if (!AssetDatabase.IsOpenForEdit(assetPath, opts))
                    return false;
            }
            else if (AssetDatabase.IsForeignAsset(instanceID))
            {
                if (!AssetDatabase.IsMetaFileOpenForEdit(assetObject, opts))
                    return false;
            }

            return true;
        }

        internal virtual bool IsEnabled()
        {
            // disable editor if any objects in the editor are not editable
            foreach (UnityObject target in targets)
            {
                if (target == null)
                    return false;
                if ((target.hideFlags & HideFlags.NotEditable) != 0)
                    return false;

                if (EditorUtility.IsPersistent(target) && !IsAppropriateFileOpenForEdit(target))
                    return false;
            }

            return true;
        }

        internal bool IsOpenForEdit()
        {
            foreach (UnityObject target in targets)
            {
                if (EditorUtility.IsPersistent(target) && !IsAppropriateFileOpenForEdit(target))
                    return false;
            }

            return true;
        }

        public virtual bool UseDefaultMargins()
        {
            return true;
        }

        public void Initialize(UnityObject[] targets)
        {
            throw new InvalidOperationException("You shouldn't call Initialize for Editors");
        }

        public bool MoveNextTarget()
        {
            referenceTargetIndex++;
            return referenceTargetIndex < targets.Length;
        }

        public void ResetTarget()
        {
            referenceTargetIndex = 0;
        }

        // Implement this method to show a limited inspector for showing tweakable parameters in an Asset Store preview.
        internal virtual void OnAssetStoreInspectorGUI()
        {
        }
    }
}
