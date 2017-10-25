// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace UnityEditor
{
    internal enum WrapModeFixedCurve
    {
        Clamp = (int)WrapMode.ClampForever,
        Loop = (int)WrapMode.Loop,
        PingPong = (int)WrapMode.PingPong
    }

    [System.Serializable]
    internal class CurveEditorWindow : EditorWindow
    {
        enum NormalizationMode
        {
            None = 0,
            Normalize = 1,
            Denormalize = 2,
        }

        //const int kToolbarHeight = 17;
        const int kPresetsHeight = 46;

        static CurveEditorWindow s_SharedCurveEditor;

        CurveEditor m_CurveEditor;

        AnimationCurve m_Curve;
        Color m_Color;

        CurvePresetsContentsForPopupWindow m_CurvePresets;
        GUIContent m_GUIContent = new GUIContent();

        [SerializeField]
        GUIView delegateView;

        public static CurveEditorWindow instance
        {
            get
            {
                if (!s_SharedCurveEditor)
                    s_SharedCurveEditor = ScriptableObject.CreateInstance<CurveEditorWindow>();
                return s_SharedCurveEditor;
            }
        }

        public string currentPresetLibrary
        {
            get
            {
                InitCurvePresets();
                return m_CurvePresets.currentPresetLibrary;
            }
            set
            {
                InitCurvePresets();
                m_CurvePresets.currentPresetLibrary = value;
            }
        }

        public static AnimationCurve curve
        {
            get { return visible ? CurveEditorWindow.instance.m_Curve : null; }
            set
            {
                if (value == null)
                {
                    CurveEditorWindow.instance.m_Curve = null;
                }
                else
                {
                    CurveEditorWindow.instance.m_Curve = value;
                    CurveEditorWindow.instance.RefreshShownCurves();
                }
            }
        }

        public static Color color
        {
            get { return CurveEditorWindow.instance.m_Color; }
            set
            {
                CurveEditorWindow.instance.m_Color = value;
                CurveEditorWindow.instance.RefreshShownCurves();
            }
        }

        public static bool visible
        {
            get { return s_SharedCurveEditor != null; }
        }

        void OnEnable()
        {
            s_SharedCurveEditor = this;
            Init(null);
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

            // For each of horizontal and vertical axis, if we have a finite range for that axis, use that range,
            // otherwise use framing logic to determine shown range for that axis.
            bool frameH = true;
            bool frameV = true;
            if (m_CurveEditor.settings.hRangeMin != Mathf.NegativeInfinity && m_CurveEditor.settings.hRangeMax != Mathf.Infinity)
            {
                m_CurveEditor.SetShownHRangeInsideMargins(m_CurveEditor.settings.hRangeMin, m_CurveEditor.settings.hRangeMax);
                frameH = false;
            }
            if (m_CurveEditor.settings.vRangeMin != Mathf.NegativeInfinity && m_CurveEditor.settings.vRangeMax != Mathf.Infinity)
            {
                m_CurveEditor.SetShownVRangeInsideMargins(m_CurveEditor.settings.vRangeMin, m_CurveEditor.settings.vRangeMax);
                frameV = false;
            }

            m_CurveEditor.FrameSelected(frameH, frameV);

            titleContent = new GUIContent("Curve");

            // deal with window size
            minSize = new Vector2(240, 240 + kPresetsHeight);
            maxSize = new Vector2(10000, 10000);
        }

        CurveLibraryType curveLibraryType
        {
            get
            {
                if (m_CurveEditor.settings.hasUnboundedRanges)
                    return CurveLibraryType.Unbounded;
                return CurveLibraryType.NormalizedZeroToOne;
            }
        }

        // Returns true if a valid normalizationRect is returned (ranges are bounded)
        bool GetNormalizationRect(out Rect normalizationRect)
        {
            normalizationRect = new Rect();
            if (m_CurveEditor.settings.hasUnboundedRanges)
                return false;

            normalizationRect = new Rect(
                    m_CurveEditor.settings.hRangeMin,
                    m_CurveEditor.settings.vRangeMin,
                    m_CurveEditor.settings.hRangeMax - m_CurveEditor.settings.hRangeMin,
                    m_CurveEditor.settings.vRangeMax - m_CurveEditor.settings.vRangeMin);
            return true;
        }

        static Keyframe[] CopyAndScaleCurveKeys(Keyframe[] orgKeys, Rect rect, NormalizationMode normalization)
        {
            Keyframe[] scaledKeys = new Keyframe[orgKeys.Length];
            orgKeys.CopyTo(scaledKeys, 0);
            if (normalization == NormalizationMode.None)
                return scaledKeys;

            if (rect.width == 0f || rect.height == 0f || float.IsInfinity(rect.width) || float.IsInfinity(rect.height))
            {
                Debug.LogError("CopyAndScaleCurve: Invalid scale: " + rect);
                return scaledKeys;
            }

            float tangentMultiplier = rect.height / rect.width;
            switch (normalization)
            {
                case NormalizationMode.Normalize:
                    for (int i = 0; i < scaledKeys.Length; ++i)
                    {
                        scaledKeys[i].time = (orgKeys[i].time - rect.xMin) / rect.width;
                        scaledKeys[i].value = (orgKeys[i].value - rect.yMin) / rect.height;
                        if (!float.IsInfinity(orgKeys[i].inTangent))
                            scaledKeys[i].inTangent = orgKeys[i].inTangent / tangentMultiplier;
                        if (!float.IsInfinity(orgKeys[i].outTangent))
                            scaledKeys[i].outTangent = orgKeys[i].outTangent / tangentMultiplier;
                    }
                    break;
                case NormalizationMode.Denormalize:
                    // From normalized to real
                    for (int i = 0; i < scaledKeys.Length; ++i)
                    {
                        scaledKeys[i].time = orgKeys[i].time * rect.width + rect.xMin;
                        scaledKeys[i].value = orgKeys[i].value * rect.height + rect.yMin;
                        if (!float.IsInfinity(orgKeys[i].inTangent))
                            scaledKeys[i].inTangent = orgKeys[i].inTangent * tangentMultiplier;
                        if (!float.IsInfinity(orgKeys[i].outTangent))
                            scaledKeys[i].outTangent = orgKeys[i].outTangent * tangentMultiplier;
                    }
                    break;
            }

            return scaledKeys;
        }

        void InitCurvePresets()
        {
            if (m_CurvePresets == null)
            {
                // Selection callback for library window
                System.Action<AnimationCurve> presetSelectedCallback = delegate(AnimationCurve presetCurve)
                    {
                        ValidateCurveLibraryTypeAndScale();

                        // Scale curve up using ranges
                        m_Curve.keys = GetDenormalizedKeys(presetCurve.keys);
                        m_Curve.postWrapMode = presetCurve.postWrapMode;
                        m_Curve.preWrapMode = presetCurve.preWrapMode;

                        m_CurveEditor.SelectNone();
                        RefreshShownCurves();
                        SendEvent("CurveChanged", true);
                    };

                // We set the curve to save when showing the popup to ensure to scale the current state of the curve
                AnimationCurve curveToSaveAsPreset = null;
                m_CurvePresets = new CurvePresetsContentsForPopupWindow(curveToSaveAsPreset, curveLibraryType, presetSelectedCallback);
                m_CurvePresets.InitIfNeeded();
            }
        }

        void OnDestroy()
        {
            if (m_CurvePresets != null)
                m_CurvePresets.GetPresetLibraryEditor().UnloadUsedLibraries();
        }

        void OnDisable()
        {
            m_CurveEditor.OnDisable();
            if (s_SharedCurveEditor == this)
                s_SharedCurveEditor = null;
            else if (!this.Equals(s_SharedCurveEditor))
                throw new ApplicationException("s_SharedCurveEditor does not equal this");
        }

        private void RefreshShownCurves()
        {
            m_CurveEditor.animationCurves = GetCurveWrapperArray();
        }

        public void Show(GUIView viewToUpdate, CurveEditorSettings settings)
        {
            delegateView = viewToUpdate;
            m_OnCurveChanged = null;

            Init(settings);
            ShowAuxWindow();
        }

        System.Action<AnimationCurve> m_OnCurveChanged;

        public void Show(System.Action<AnimationCurve> onCurveChanged, CurveEditorSettings settings)
        {
            m_OnCurveChanged = onCurveChanged;
            delegateView = null;

            Init(settings);
            ShowAuxWindow();
        }

        internal class Styles
        {
            public GUIStyle curveEditorBackground = "PopupCurveEditorBackground";
            public GUIStyle miniToolbarPopup = "MiniToolbarPopup";
            public GUIStyle miniToolbarButton = "MiniToolbarButtonLeft";
            public GUIStyle curveSwatch = "PopupCurveEditorSwatch";
            public GUIStyle curveSwatchArea = "PopupCurveSwatchBackground";
            public GUIStyle curveWrapPopup = "PopupCurveDropdown";
        }
        internal static Styles ms_Styles;

        CurveWrapper[] GetCurveWrapperArray()
        {
            if (m_Curve == null)
                return new CurveWrapper[] {};
            CurveWrapper cw = new CurveWrapper();
            cw.id = "Curve".GetHashCode();
            cw.groupId = -1;
            cw.color = m_Color;
            cw.hidden = false;
            cw.readOnly = false;
            cw.renderer = new NormalCurveRenderer(m_Curve);
            cw.renderer.SetWrap(m_Curve.preWrapMode, m_Curve.postWrapMode);
            return new CurveWrapper[] { cw };
        }

        Rect GetCurveEditorRect()
        {
            //return new Rect(0, kToolbarHeight, position.width, position.height-kToolbarHeight);
            return new Rect(0, 0, position.width, position.height - kPresetsHeight);
        }

        static internal Keyframe[] GetLinearKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 0, 1, 1);
            keys[1] = new Keyframe(1, 1, 1, 1);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetLinearMirrorKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 1, -1, -1);
            keys[1] = new Keyframe(1, 0, -1, -1);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetEaseInKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 0, 0, 0);
            keys[1] = new Keyframe(1, 1, 2, 2);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetEaseInMirrorKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 1, -2, -2);
            keys[1] = new Keyframe(1, 0, 0, 0);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetEaseOutKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 0, 2, 2);
            keys[1] = new Keyframe(1, 1, 0, 0);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetEaseOutMirrorKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 1, 0, 0);
            keys[1] = new Keyframe(1, 0, -2, -2);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetEaseInOutKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 0, 0, 0);
            keys[1] = new Keyframe(1, 1, 0, 0);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetEaseInOutMirrorKeys()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 1, 0, 0);
            keys[1] = new Keyframe(1, 0, 0, 0);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal Keyframe[] GetConstantKeys(float value)
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, value, 0, 0);
            keys[1] = new Keyframe(1, value, 0, 0);
            SetSmoothEditable(ref keys);
            return keys;
        }

        static internal void SetSmoothEditable(ref Keyframe[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyBroken(ref keys[i], false);
                AnimationUtility.SetKeyLeftTangentMode(ref keys[i], TangentMode.Free);
                AnimationUtility.SetKeyRightTangentMode(ref keys[i], TangentMode.Free);
            }
        }

        Keyframe[] NormalizeKeys(Keyframe[] sourceKeys, NormalizationMode normalization)
        {
            Rect normalizationRect;
            if (!GetNormalizationRect(out normalizationRect))
                // No normalization rect, just return a copy of the source keyframes
                normalization = NormalizationMode.None;
            return CopyAndScaleCurveKeys(sourceKeys, normalizationRect, normalization);
        }

        Keyframe[] GetDenormalizedKeys(Keyframe[] sourceKeys)
        {
            return NormalizeKeys(sourceKeys, NormalizationMode.Denormalize);
        }

        Keyframe[] GetNormalizedKeys(Keyframe[] sourceKeys)
        {
            return NormalizeKeys(sourceKeys, NormalizationMode.Normalize);
        }

        void OnGUI()
        {
            bool gotMouseUp = (Event.current.type == EventType.MouseUp);

            if (delegateView == null && m_OnCurveChanged == null)
                m_Curve = null;

            if (ms_Styles == null)
                ms_Styles = new Styles();

            // Curve Editor
            m_CurveEditor.rect = GetCurveEditorRect();
            m_CurveEditor.hRangeLocked = Event.current.shift;
            m_CurveEditor.vRangeLocked = EditorGUI.actionKey;

            GUI.changed = false;

            GUI.Label(m_CurveEditor.drawRect, GUIContent.none, ms_Styles.curveEditorBackground);
            m_CurveEditor.OnGUI();

            // Preset swatch area
            GUI.Box(new Rect(0, position.height - kPresetsHeight, position.width, kPresetsHeight), "", ms_Styles.curveSwatchArea);
            Color curveColor = m_Color;
            curveColor.a *= 0.6f;
            const float margin = 45f;
            const float width = 40f;
            const float height = 25f;
            float yPos = position.height - kPresetsHeight + (kPresetsHeight - height) * 0.5f;
            InitCurvePresets();
            CurvePresetLibrary curveLibrary = m_CurvePresets.GetPresetLibraryEditor().GetCurrentLib();
            if (curveLibrary != null)
            {
                for (int i = 0; i < curveLibrary.Count(); i++)
                {
                    Rect swatchRect = new Rect(margin + (width + 5f) * i, yPos, width, height);
                    m_GUIContent.tooltip = curveLibrary.GetName(i);
                    if (GUI.Button(swatchRect, m_GUIContent, ms_Styles.curveSwatch))
                    {
                        AnimationCurve animCurve = curveLibrary.GetPreset(i) as AnimationCurve;
                        m_Curve.keys = GetDenormalizedKeys(animCurve.keys);
                        m_Curve.postWrapMode = animCurve.postWrapMode;
                        m_Curve.preWrapMode = animCurve.preWrapMode;
                        m_CurveEditor.SelectNone();
                        SendEvent("CurveChanged", true);
                    }
                    if (Event.current.type == EventType.Repaint)
                        curveLibrary.Draw(swatchRect, i);

                    if (swatchRect.xMax > position.width - 2 * margin)
                        break;
                }
            }

            Rect presetDropDownButtonRect = new Rect(margin - 20f, yPos + 5f, 20, 20);
            PresetDropDown(presetDropDownButtonRect);

            // For adding default preset curves
            //if (EditorGUI.DropdownButton(new Rect (position.width -26, yPos, 20, 20), GUIContent.none, FocusType.Passive, "OL Plus"))
            //  AddDefaultPresetsToCurrentLib ();

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

        void PresetDropDown(Rect rect)
        {
            if (EditorGUI.DropdownButton(rect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.inspectorTitlebarText))
            {
                if (m_Curve != null)
                {
                    if (m_CurvePresets == null)
                    {
                        Debug.LogError("Curve presets error");
                        return;
                    }

                    ValidateCurveLibraryTypeAndScale();

                    AnimationCurve copy = new AnimationCurve(GetNormalizedKeys(m_Curve.keys));
                    copy.postWrapMode = m_Curve.postWrapMode;
                    copy.preWrapMode = m_Curve.preWrapMode;

                    m_CurvePresets.curveToSaveAsPreset = copy;
                    PopupWindow.Show(rect, m_CurvePresets);
                }
            }
        }

        void ValidateCurveLibraryTypeAndScale()
        {
            Rect normalizationRect;
            if (GetNormalizationRect(out normalizationRect))
            {
                if (curveLibraryType != CurveLibraryType.NormalizedZeroToOne)
                    Debug.LogError("When having a normalize rect we should be using curve library type: NormalizedZeroToOne (normalizationRect: " + normalizationRect + ")");
            }
            else
            {
                if (curveLibraryType != CurveLibraryType.Unbounded)
                    Debug.LogError("When NOT having a normalize rect we should be using library type: Unbounded");
            }
        }

        // Polynomial curves have limitations on how they have to be authored.
        // Since we don't enforce the layout, we have a button that enforces the curve layout instead.
        /*
        void OptimizePolynomialCurve (Rect rect)
        {
            ///@TODO: only show this when editing shuriken curves....

            bool wasEnabled = GUI.enabled;

            bool isValidPolynomialCurve = true;
            for (int i=0;i<m_CurveEditor.animationCurves.Length;i++)
                isValidPolynomialCurve &= AnimationUtility.IsValidPolynomialCurve(m_CurveEditor.animationCurves[i].curve);

            GUI.enabled = !isValidPolynomialCurve;
            if (GUI.Button (rect, "Optimize Polynomial Curve"))
            {
                for (int i=0;i<m_CurveEditor.animationCurves.Length;i++)
                    AnimationUtility.ConstrainToPolynomialCurve(m_CurveEditor.animationCurves[i].curve);

                m_CurveEditor.SelectNone();
                SendEvent ("CurveChanged", true);
            }

            GUI.enabled = wasEnabled;
        }*/

        public void UpdateCurve()
        {
            DoUpdateCurve(false);
        }

        private void DoUpdateCurve(bool exitGUI)
        {
            if (m_CurveEditor.animationCurves.Length > 0
                && m_CurveEditor.animationCurves[0] != null
                && m_CurveEditor.animationCurves[0].changed)
            {
                m_CurveEditor.animationCurves[0].changed = false;
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

            if (m_OnCurveChanged != null)
            {
                m_OnCurveChanged(curve);
            }
            GUI.changed = true;
        }
    }
}
