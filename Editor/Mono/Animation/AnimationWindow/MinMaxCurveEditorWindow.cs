// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal class MinMaxCurveEditorWindow : EditorWindow
    {
        const int k_PresetsHeight = 46;
        const float k_WindowMinSize = 240;
        const float k_WindowMaxSize = 10000;
        const float k_PresetSwatchMargin = 45f;
        const float k_PresetSwatchWidth = 40f;
        const float k_PresetSwatchHeight = 25f;
        const float k_PresetSwatchSeperation = 5;
        const float k_PresetsDropdownButtonSize = 20;

        static MinMaxCurveEditorWindow s_SharedMinMaxCurveEditor;

        static CurveEditorWindow.Styles s_Styles;

        CurveEditor m_CurveEditor;

        AnimationCurve m_MinCurve;
        AnimationCurve m_MaxCurve;

        SerializedProperty m_MultiplierProperty;
        Color m_Color;

        DoubleCurvePresetsContentsForPopupWindow m_CurvePresets;

        [SerializeField]
        GUIView delegateView;

        public static MinMaxCurveEditorWindow instance
        {
            get
            {
                if (!s_SharedMinMaxCurveEditor)
                    s_SharedMinMaxCurveEditor = ScriptableObject.CreateInstance<MinMaxCurveEditorWindow>();
                return s_SharedMinMaxCurveEditor;
            }
        }

        public AnimationCurve minCurve { get { return m_MinCurve; } }
        public AnimationCurve maxCurve { get { return m_MaxCurve; } }

        public static string xAxisLabel { get; set; } = "time";

        public static bool visible
        {
            get { return s_SharedMinMaxCurveEditor != null; }
        }

        // Called by OnEnable to make sure the CurveEditor is not null,
        // and by Show so we get a fresh CurveEditor when the user clicks a new curve.
        void Init(CurveEditorSettings settings)
        {
            m_CurveEditor = new CurveEditor(GetCurveEditorRect(), GetCurveWrapperArray(), true);
            m_CurveEditor.curvesUpdated = UpdateCurve;
            m_CurveEditor.scaleWithWindow = true;
            m_CurveEditor.margin = 40;
            if (settings != null)
                m_CurveEditor.settings = settings;
            m_CurveEditor.settings.hTickLabelOffset = 10;
            m_CurveEditor.settings.rectangleToolFlags = CurveEditorSettings.RectangleToolFlags.MiniRectangleTool;
            m_CurveEditor.settings.undoRedoSelection = true;
            m_CurveEditor.settings.showWrapperPopups = true;
            m_CurveEditor.settings.xAxisLabel = xAxisLabel;
            UpdateRegionDomain();

            // For each of horizontal and vertical axis, if we have a finite range for that axis, use that range,
            // otherwise use framing logic to determine shown range for that axis.
            bool frameH = true;
            bool frameV = true;
            if (!float.IsNegativeInfinity(m_CurveEditor.settings.hRangeMin) && !float.IsInfinity(m_CurveEditor.settings.hRangeMax))
            {
                m_CurveEditor.SetShownHRangeInsideMargins(m_CurveEditor.settings.hRangeMin, m_CurveEditor.settings.hRangeMax);
                frameH = false;
            }
            if (!float.IsNegativeInfinity(m_CurveEditor.settings.vRangeMin) && !float.IsInfinity(m_CurveEditor.settings.vRangeMax))
            {
                m_CurveEditor.SetShownVRangeInsideMargins(m_CurveEditor.settings.vRangeMin, m_CurveEditor.settings.vRangeMax);
                frameV = false;
            }

            m_CurveEditor.FrameSelected(frameH, frameV);
        }

        void InitCurvePresets()
        {
            if (m_CurvePresets == null)
            {
                AnimationCurve max = m_CurveEditor.animationCurves[0].curve;
                AnimationCurve min = m_CurveEditor.animationCurves.Length > 1 ? m_CurveEditor.animationCurves[1].curve : new AnimationCurve();

                // Selection callback for library window
                System.Action<DoubleCurve> presetSelectedCallback = delegate(DoubleCurve presetCurve)
                {
                    var doubleCurve = new DoubleCurve(min, max, true);
                    doubleCurve.minCurve.keys = CurveEditorWindow.GetNormalizedKeys(presetCurve.minCurve.keys, m_CurveEditor);
                    doubleCurve.minCurve.postWrapMode = presetCurve.minCurve.postWrapMode;
                    doubleCurve.minCurve.preWrapMode = presetCurve.minCurve.preWrapMode;

                    doubleCurve.maxCurve.keys = CurveEditorWindow.GetNormalizedKeys(presetCurve.maxCurve.keys, m_CurveEditor);
                    doubleCurve.maxCurve.postWrapMode = presetCurve.maxCurve.postWrapMode;
                    doubleCurve.maxCurve.preWrapMode = presetCurve.maxCurve.preWrapMode;

                    m_MinCurve = doubleCurve.minCurve;
                    m_MaxCurve = doubleCurve.maxCurve;

                    m_CurveEditor.SelectNone();
                    RefreshShownCurves();
                    SendEvent("CurveChanged", true);
                };

                // We set the curve to save when showing the popup to ensure to scale the current state of the curve
                m_CurvePresets = new DoubleCurvePresetsContentsForPopupWindow(new DoubleCurve(min, max, true), presetSelectedCallback);
                m_CurvePresets.InitIfNeeded();
                m_CurvePresets.GetPresetLibraryEditor().GetCurrentLib().useRanges = false;
            }
        }

        public static void SetCurves(SerializedProperty max, SerializedProperty min, SerializedProperty multiplier, Color color)
        {
            instance.m_Color = color;

            if (max == null)
                instance.m_MaxCurve = null;
            else
                instance.m_MaxCurve = max.hasMultipleDifferentValues ? new AnimationCurve() : max.animationCurveValue;

            if (min == null)
                instance.m_MinCurve = null;
            else
                instance.m_MinCurve = min.hasMultipleDifferentValues ? new AnimationCurve() : min.animationCurveValue;

            instance.m_MultiplierProperty = multiplier;
            instance.RefreshShownCurves();
        }

        public static void ShowPopup(GUIView viewToUpdate)
        {
            instance.Show(viewToUpdate, null);
        }

        void SetAxisUiScalarsCallback(Vector2 newAxisScalars)
        {
            if (m_MultiplierProperty == null)
                return;

            m_MultiplierProperty.floatValue = newAxisScalars.y;

            // We must apply the changes as this is called outside of the OnGUI code and changes made will not be applied.
            m_MultiplierProperty.serializedObject.ApplyModifiedProperties();
        }

        Vector2 GetAxisUiScalarsCallback()
        {
            if (m_MultiplierProperty == null)
                return Vector2.one;

            if (m_MultiplierProperty.floatValue < 0)
            {
                m_MultiplierProperty.floatValue = Mathf.Abs(m_MultiplierProperty.floatValue);
                m_MultiplierProperty.serializedObject.ApplyModifiedProperties();
            }

            return new Vector2(1, m_MultiplierProperty.floatValue);
        }

        CurveWrapper GetCurveWrapper(AnimationCurve curve, int id)
        {
            CurveWrapper cw = new CurveWrapper();
            cw.id = id;
            cw.groupId = -1;
            cw.color = m_Color;
            cw.hidden = false;
            cw.readOnly = false;
            cw.getAxisUiScalarsCallback = GetAxisUiScalarsCallback;
            cw.setAxisUiScalarsCallback = SetAxisUiScalarsCallback;
            cw.renderer = new NormalCurveRenderer(curve);
            cw.renderer.SetWrap(curve.preWrapMode, curve.postWrapMode);
            return cw;
        }

        CurveWrapper[] GetCurveWrapperArray()
        {
            int id = "Curve".GetHashCode();

            if (m_MaxCurve != null)
            {
                var maxWrapper = GetCurveWrapper(m_MaxCurve, id);
                if (m_MinCurve != null)
                {
                    var minWrapper = GetCurveWrapper(m_MinCurve, id + 1);
                    minWrapper.regionId = maxWrapper.regionId = 1;
                    return new[] { maxWrapper, minWrapper };
                }
                else
                {
                    return new[] { maxWrapper };
                }
            }

            return new CurveWrapper[] {};
        }

        Rect GetCurveEditorRect()
        {
            return new Rect(0, 0, position.width, position.height - k_PresetsHeight);
        }

        void OnEnable()
        {
            if (s_SharedMinMaxCurveEditor && s_SharedMinMaxCurveEditor != this)
                s_SharedMinMaxCurveEditor.Close();
            s_SharedMinMaxCurveEditor = this;
            Init(null);
        }

        void OnDestroy()
        {
            m_CurvePresets.GetPresetLibraryEditor().UnloadUsedLibraries();
        }

        void OnDisable()
        {
            m_CurveEditor.OnDisable();
            if (s_SharedMinMaxCurveEditor == this)
                s_SharedMinMaxCurveEditor = null;
        }

        void RefreshShownCurves()
        {
            m_CurveEditor.animationCurves = GetCurveWrapperArray();
            UpdateRegionDomain();
        }

        void UpdateRegionDomain()
        {
            // Calculate region domain for drawing the shaded region between 2 curves.
            var domain = new Vector2(float.MaxValue, float.MinValue);
            if (m_MaxCurve != null && m_MinCurve != null)
            {
                foreach (var animationCurve in new[] { m_MaxCurve, m_MinCurve })
                {
                    if (animationCurve.length > 0)
                    {
                        var keys = animationCurve.keys;
                        domain.x = Mathf.Min(domain.x, keys.First().time);
                        domain.y = Math.Max(domain.y, keys.Last().time);
                    }
                }
            }
            m_CurveEditor.settings.curveRegionDomain = domain;
        }

        public void Show(GUIView viewToUpdate, CurveEditorSettings settings)
        {
            delegateView = viewToUpdate;
            Init(settings);
            ShowAuxWindow();
            titleContent = EditorGUIUtility.TrTextContent("Curve Editor");

            // deal with window size
            minSize = new Vector2(k_WindowMinSize, k_WindowMinSize + k_PresetsHeight);
            maxSize = new Vector2(k_WindowMaxSize, k_WindowMaxSize);
        }

        void DrawPresetSwatchArea()
        {
            GUI.Box(new Rect(0, position.height - k_PresetsHeight, position.width, k_PresetsHeight), "", s_Styles.curveSwatchArea);
            Color curveColor = m_Color;
            curveColor.a *= 0.6f;
            float yPos = position.height - k_PresetsHeight + (k_PresetsHeight - k_PresetSwatchHeight) * 0.5f;
            InitCurvePresets();
            var curveLibrary = m_CurvePresets.GetPresetLibraryEditor().GetCurrentLib();
            if (curveLibrary != null)
            {
                GUIContent guiContent = EditorGUIUtility.TempContent(string.Empty);
                for (int i = 0; i < curveLibrary.Count(); i++)
                {
                    Rect swatchRect = new Rect(k_PresetSwatchMargin + (k_PresetSwatchWidth + k_PresetSwatchSeperation) * i, yPos, k_PresetSwatchWidth, k_PresetSwatchHeight);
                    guiContent.tooltip = curveLibrary.GetName(i);
                    if (GUI.Button(swatchRect, guiContent, s_Styles.curveSwatch))
                    {
                        AnimationCurve max = m_CurveEditor.animationCurves[0].curve;
                        AnimationCurve min = m_CurveEditor.animationCurves.Length > 1 ? m_CurveEditor.animationCurves[1].curve : null;
                        var animCurve = curveLibrary.GetPreset(i) as DoubleCurve;

                        max.keys = CurveEditorWindow.GetDenormalizedKeys(animCurve.maxCurve.keys, m_CurveEditor);
                        max.postWrapMode = animCurve.maxCurve.postWrapMode;
                        max.preWrapMode = animCurve.maxCurve.preWrapMode;

                        if (min != null)
                        {
                            min.keys = CurveEditorWindow.GetDenormalizedKeys(animCurve.minCurve.keys, m_CurveEditor);
                            min.postWrapMode = animCurve.minCurve.postWrapMode;
                            min.preWrapMode = animCurve.minCurve.preWrapMode;
                        }

                        m_CurveEditor.SelectNone();
                        RefreshShownCurves();
                        SendEvent("CurveChanged", true);
                    }
                    if (Event.current.type == EventType.Repaint)
                        curveLibrary.Draw(swatchRect, i);

                    if (swatchRect.xMax > position.width - 2 * k_PresetSwatchMargin)
                        break;
                }
            }

            // Dropdown
            Rect presetDropDownButtonRect = new Rect(k_PresetSwatchMargin - k_PresetsDropdownButtonSize, yPos + k_PresetSwatchSeperation, k_PresetsDropdownButtonSize, k_PresetsDropdownButtonSize);
            if (EditorGUI.DropdownButton(presetDropDownButtonRect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.inspectorTitlebarText))
            {
                if (m_MaxCurve != null)
                {
                    AnimationCurve max = m_CurveEditor.animationCurves[0].curve;
                    AnimationCurve maxCopy = new AnimationCurve(CurveEditorWindow.GetNormalizedKeys(max.keys, m_CurveEditor));
                    maxCopy.postWrapMode = max.postWrapMode;
                    maxCopy.preWrapMode = max.preWrapMode;

                    AnimationCurve minCopy = null;
                    if (m_MinCurve != null)
                    {
                        AnimationCurve min = m_CurveEditor.animationCurves[1].curve;
                        minCopy = new AnimationCurve(CurveEditorWindow.GetNormalizedKeys(min.keys, m_CurveEditor));
                        minCopy.postWrapMode = min.postWrapMode;
                        minCopy.preWrapMode = min.preWrapMode;
                    }

                    m_CurvePresets.doubleCurveToSave = new DoubleCurve(minCopy, maxCopy, true);
                    PopupWindow.Show(presetDropDownButtonRect, m_CurvePresets);
                }
            }
        }

        void OnGUI()
        {
            bool gotMouseUp = (Event.current.type == EventType.MouseUp);

            if (delegateView == null)
            {
                m_MinCurve = null;
                m_MaxCurve = null;
            }

            if (s_Styles == null)
                s_Styles = new CurveEditorWindow.Styles();

            // Curve Editor
            m_CurveEditor.rect = GetCurveEditorRect();
            m_CurveEditor.hRangeLocked = Event.current.shift;
            m_CurveEditor.vRangeLocked = EditorGUI.actionKey;

            GUI.changed = false;

            GUI.Label(m_CurveEditor.drawRect, GUIContent.none, s_Styles.curveEditorBackground);
            m_CurveEditor.OnGUI();

            DrawPresetSwatchArea();

            if (Event.current.type == EventType.Used && gotMouseUp)
            {
                DoUpdateCurve(false);
                SendEvent("CurveChangeCompleted", true);
            }
            else if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            {
                DoUpdateCurve(true);
            }
        }

        public void UpdateCurve()
        {
            DoUpdateCurve(false);
        }

        void DoUpdateCurve(bool exitGUI)
        {
            bool minChanged = m_CurveEditor.animationCurves.Length > 0 && m_CurveEditor.animationCurves[0] != null && m_CurveEditor.animationCurves[0].changed;
            bool maxChanged = m_CurveEditor.animationCurves.Length > 1 && m_CurveEditor.animationCurves[1] != null && m_CurveEditor.animationCurves[1].changed;

            if (minChanged || maxChanged)
            {
                if (minChanged)
                    m_CurveEditor.animationCurves[0].changed = false;

                if (maxChanged)
                    m_CurveEditor.animationCurves[1].changed = false;

                RefreshShownCurves();
                SendEvent("CurveChanged", exitGUI);
            }
        }

        void SendEvent(string eventName, bool exitGUI)
        {
            if (delegateView)
            {
                Event e = EditorGUIUtility.CommandEvent(eventName);
                Repaint();
                delegateView.SendEvent(e);
                if (exitGUI)
                    GUIUtility.ExitGUI();
            }
            GUI.changed = true;
        }
    }
}
