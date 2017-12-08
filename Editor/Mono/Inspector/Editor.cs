// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

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
        class Styles
        {
            public GUIStyle preBackground = "preBackground";
            public GUIStyle preBackgroundSolid = new GUIStyle("preBackground");
            public GUIStyle previewMiniLabel = new GUIStyle(EditorStyles.whiteMiniLabel);
            public GUIStyle dropShadowLabelStyle = new GUIStyle("PreOverlayLabel");

            public Styles()
            {
                preBackgroundSolid.overflow = preBackgroundSolid.border;
                previewMiniLabel.alignment = TextAnchor.UpperCenter;
            }
        }
        static Styles s_Styles;

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
                if (target is MonoBehaviour)
                    guiContent.text += MonoScript.FromMonoBehaviour(target as MonoBehaviour).GetClass().Name;
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
            if (s_Styles == null)
                s_Styles = new Styles();

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
                        Selection.objects = new UnityObject[] { defaultPreview.target };

                    // Make room for label underneath
                    r.height -= kPreviewLabelHeight;
                    // Make preview square
                    Rect rSquare = new Rect(r.x + (r.width - previewSize) * 0.5f, r.y + (r.height - previewSize) * 0.5f, previewSize, previewSize);

                    // Draw preview inside a group to prevent overdraw
                    // @TODO: Make style with solid color that doesn't have overdraw
                    GUI.BeginGroup(rSquare);
                    Editor.m_AllowMultiObjectAccess = false;
                    defaultPreview.OnInteractivePreviewGUI(new Rect(0, 0, previewSize, previewSize), s_Styles.preBackgroundSolid);
                    Editor.m_AllowMultiObjectAccess = true;
                    GUI.EndGroup();

                    // Draw the name of the object
                    r.y = rSquare.yMax;
                    r.height = 16;
                    GUI.Label(r, targets[i].name, s_Styles.previewMiniLabel);
                    defaultPreview.MoveNextTarget();
                }
                defaultPreview.ResetTarget();  // Remember to reset referenceTargetIndex to prevent following calls to 'editor.target' will return a different target which breaks all sorts of places. Fix for case 600235

                if (Event.current.type == EventType.Repaint)
                    text = string.Format("Previewing {0} of {1} Objects", count, targets.Length);
            }
            // If only a single target, just draw that one
            else
            {
                defaultPreview.OnInteractivePreviewGUI(previewArea, s_Styles.preBackground);

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
                var textHeight = s_Styles.dropShadowLabelStyle.CalcHeight(GUIContent.Temp(text), previewArea.width);
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

    public partial class Editor : ScriptableObject, IPreviewable, IToolModeOwner
    {
        static Styles s_Styles;

        private const float kImageSectionWidth = 44;
        internal delegate void OnEditorGUIDelegate(Editor editor, Rect drawRect);
        internal static OnEditorGUIDelegate OnPostIconGUI = null;

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

        class Styles
        {
            public GUIStyle inspectorBig = new GUIStyle(EditorStyles.inspectorBig);
            public GUIStyle inspectorBigInner = new GUIStyle("IN BigTitle inner");
            public GUIStyle centerStyle = new GUIStyle();

            public Styles()
            {
                centerStyle.alignment = TextAnchor.MiddleCenter;
                inspectorBig.padding.bottom -= 1;
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

        internal static bool DoDrawDefaultInspector(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.Update();

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
            return DoDrawDefaultInspector(serializedObject);
        }

        // This is the method that should be called from externally e.g. myEditor.DrawHeader ();
        // Do not make this method virtual - override OnHeaderGUI instead.
        public void DrawHeader()
        {
            // If we call DrawHeader from inside an an editor's OnInspectorGUI call, we have to do special handling.
            // (See DrawHeaderFromInsideHierarchy for details.)
            // We know we're inside the OnInspectorGUI block (or a similar vertical block) if hierarchyMode is set to true.
            if (EditorGUIUtility.hierarchyMode)
                DrawHeaderFromInsideHierarchy();
            else
                OnHeaderGUI();
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
            if (!(this is AssetImporterEditor))
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
                if (GUILayout.Button("Open", EditorStyles.miniButton))
                {
                    if (this is AssetImporterEditor)
                        AssetDatabase.OpenAsset((this as AssetImporterEditor).assetEditor.targets);
                    else
                        AssetDatabase.OpenAsset(targets);
                    GUIUtility.ExitGUI();
                }
            }
        }

        protected virtual bool ShouldHideOpenButton()
        {
            return false;
        }

        internal virtual void OnHeaderIconGUI(Rect iconRect)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            Texture2D icon = null;
            if (!HasPreviewGUI())
            {
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
            }

            if (HasPreviewGUI())
                // OnPreviewGUI must have all events; not just Repaint, or else the control IDs will mis-match.
                OnPreviewGUI(iconRect, s_Styles.inspectorBigInner);
            else if (icon)
                GUI.Label(iconRect, icon, s_Styles.centerStyle);
        }

        internal virtual void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            titleRect.yMin -= 2;
            titleRect.yMax += 2;
            GUI.Label(titleRect, header, EditorStyles.largeLabel);
        }

        internal virtual void DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            // Help
            var settingsSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);

            float currentOffset = settingsSize.x;

            const int kTopMargin = 5;
            // Settings
            Rect settingsRect = new Rect(r.xMax - currentOffset, r.y + kTopMargin, settingsSize.x, settingsSize.y);
            if (EditorGUI.DropdownButton(settingsRect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive,
                    EditorStyles.iconButton))
                EditorUtility.DisplayObjectContextMenu(settingsRect, targets, 0);
            currentOffset += settingsSize.x;

            // Show Editor Header Items.
            EditorGUIUtility.DrawEditorHeaderItems(new Rect(r.xMax - currentOffset, r.y + kTopMargin, settingsSize.x, settingsSize.y), targets);
        }

        // If we call DrawHeaderGUI from inside an an editor's OnInspectorGUI call, we have to do special handling.
        // Since OnInspectorGUI is wrapped inside a BeginVertical/EndVertical block that adds padding,
        // and we don't want this padding for headers, we have to stop the vertical block,
        // draw the header, and then start a new vertical block with the same style.
        private void DrawHeaderFromInsideHierarchy()
        {
            GUIStyle style = GUILayoutUtility.topLevel.style;
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
            if (s_Styles == null)
                s_Styles = new Styles();

            GUILayout.BeginHorizontal(s_Styles.inspectorBig);
            GUILayout.Space(kImageSectionWidth - 6);
            GUILayout.BeginVertical();
            GUILayout.Space(19);
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
                GUI.Label(iconRect, AssetPreview.GetMiniTypeThumbnail(typeof(UnityObject)), s_Styles.centerStyle);

            if (editor)
                editor.DrawPostIconContent(iconRect);

            // Title
            Rect titleRect = new Rect(r.x + kImageSectionWidth, r.y + 6, r.width - kImageSectionWidth - 38 - 4, 16);
            if (editor)
                editor.OnHeaderTitleGUI(titleRect, header);
            else
                GUI.Label(titleRect, header, EditorStyles.largeLabel);

            // Help and Settings
            if (editor)
                editor.DrawHeaderHelpAndSettingsGUI(r);

            // Context Menu
            Event evt = Event.current;
            if (editor != null && evt.type == EventType.MouseDown && evt.button == 1 && r.Contains(evt.mousePosition))
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

        public virtual void DrawPreview(Rect previewArea)
        {
            ObjectPreview.DrawPreview(this, previewArea, targets);
        }

        // Auxiliary method that determines whether this editor has a set of public properties and, as thus,
        // can be expanded via a foldout. This is used in order to determine whether a foldout needs to be
        // rendered on top of the inspector title bar or not. Some examples of editors that don't require
        // a foldout are GUI Layer and Audio Listener.
        internal bool CanBeExpandedViaAFoldout()
        {
            if (m_SerializedObject == null)
                m_SerializedObject = new SerializedObject(targets, m_Context);
            else
                m_SerializedObject.Update();
            m_SerializedObject.inspectorMode = m_InspectorMode;

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
            string message;
            return IsAppropriateFileOpenForEdit(assetObject, out message);
        }

        static internal bool IsAppropriateFileOpenForEdit(UnityObject assetObject, out string message)
        {
            message = string.Empty;

            // Need to check for null early to avoid an exception being thrown in
            // AssetDatabase.IsNativeAsset(). One of these exceptions, unhandled,
            // caused case 930291 and case 930931. For both cases, the UI broke because
            // it was exiting early due to the exception.
            if (assetObject == null)
                return false;

            StatusQueryOptions opts = EditorUserSettings.allowAsyncStatusUpdate ? StatusQueryOptions.UseCachedAsync : StatusQueryOptions.UseCachedIfPossible;
            if (AssetDatabase.IsNativeAsset(assetObject))
            {
                if (!AssetDatabase.IsOpenForEdit(assetObject, out message, opts))
                    return false;
            }
            else if (AssetDatabase.IsForeignAsset(assetObject))
            {
                if (!AssetDatabase.IsMetaFileOpenForEdit(assetObject, out message, opts))
                    return false;
            }

            return true;
        }

        internal virtual bool IsEnabled()
        {
            // disable editor if any objects in the editor are not editable
            foreach (UnityObject target in targets)
            {
                if ((target.hideFlags & HideFlags.NotEditable) != 0)
                    return false;

                if (EditorUtility.IsPersistent(target) && !IsAppropriateFileOpenForEdit(target))
                    return false;
            }

            return true;
        }

        internal bool IsOpenForEdit()
        {
            string message;
            return IsOpenForEdit(out message);
        }

        internal bool IsOpenForEdit(out string message)
        {
            message = "";

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
    }
}
