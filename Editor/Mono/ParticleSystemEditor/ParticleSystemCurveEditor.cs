// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class ParticleSystemCurveEditor
{
    private List<CurveData> m_AddedCurves;
    private CurveEditor m_CurveEditor;
    static CurveEditorSettings m_CurveEditorSettings = new CurveEditorSettings();
    private Color[] m_Colors;
    private List<Color> m_AvailableColors;
    private DoubleCurvePresetsContentsForPopupWindow m_DoubleCurvePresets;

    // Presets
    public const float k_PresetsHeight = 30f;

    internal class Styles
    {
        public GUIStyle curveEditorBackground = "CurveEditorBackground";
        public GUIStyle curveSwatch = "PopupCurveEditorSwatch";
        public GUIStyle curveSwatchArea = "PopupCurveSwatchBackground";
        public GUIStyle yAxisHeader = new GUIStyle(ParticleSystemStyles.Get().label);
        public GUIContent optimizeCurveText = new GUIContent("Optimize", "Click to optimize curve. Optimized curves are defined by having at most 3 keys, with a key at both ends, and do not support loop or ping pong wrapping.");
        public GUIContent removeCurveText = new GUIContent("Remove", "Remove selected curve(s)");
        public GUIContent curveLibraryPopup = new GUIContent("", "Open curve library");
        public GUIContent presetTooltip = new GUIContent();
    }
    internal static Styles s_Styles;


    public class CurveData
    {
        // Can be single curve (using m_Max) or a region (using m_Min and m_Max)
        public SerializedProperty m_Max, m_Min;
        public bool m_SignedRange;
        public Color m_Color;
        public string m_UniqueName;
        public GUIContent m_DisplayName;
        public CurveWrapper.GetAxisScalarsCallback m_GetAxisScalarsCallback;
        public CurveWrapper.SetAxisScalarsCallback m_SetAxisScalarsCallback;
        public int m_MaxId, m_MinId;
        public bool m_Visible;
        private static int s_IdCounter;

        // Region if min and max is valid, Curve if min is null
        public CurveData(string name, GUIContent displayName, SerializedProperty min, SerializedProperty max, Color color, bool signedRange,
                         CurveWrapper.GetAxisScalarsCallback getAxisScalars, CurveWrapper.SetAxisScalarsCallback setAxisScalars, bool visible)
        {
            m_UniqueName = name;
            m_DisplayName = displayName;
            m_SignedRange = signedRange;

            m_Min = min;
            m_Max = max;
            if (m_Min != null)
                m_MinId = ++s_IdCounter;
            if (m_Max != null)
                m_MaxId = ++s_IdCounter;
            m_Color = color;
            m_GetAxisScalarsCallback = getAxisScalars;
            m_SetAxisScalarsCallback = setAxisScalars;
            m_Visible = visible;

            if (m_Max == null || m_MaxId == 0)
            {
                Debug.LogError("Max curve should always be valid! (Min curve can be null)");
            }
        }

        public bool IsRegion()
        {
            return m_Min != null;
        }
    }

    public void OnDisable()
    {
        m_CurveEditor.OnDisable();
        Undo.undoRedoPerformed -= UndoRedoPerformed;
    }

    public void OnDestroy()
    {
        m_DoubleCurvePresets.GetPresetLibraryEditor().UnloadUsedLibraries();
    }

    public void Refresh()
    {
        ContentChanged();
        UnityEditorInternal.AnimationCurvePreviewCache.ClearCache();
    }

    public void Init()
    {
        if (m_AddedCurves != null)
            return;

        m_AddedCurves = new List<CurveData>();

        // Colors
        m_Colors = new Color[]
        {
            new Color(255 / 255f,    158 / 255f,   33 / 255f),          // orange
            new Color(223 / 255f,    54 / 255f,    148 / 255f),         // purple
            new Color(0f,          175 / 255f,   255 / 255f),           // blue
            new Color(255 / 255f,    235 / 255f,   0),                  // yellow
            new Color(50 / 255f,     255 / 255f,   68 / 255f),          // green
            new Color(250 / 255f,    0f,         0f),                   // red  (this is the first color used)
        };
        m_AvailableColors = new List<Color>(m_Colors);

        // Curve Editor
        m_CurveEditorSettings.useFocusColors = true;
        m_CurveEditorSettings.showAxisLabels = false;
        m_CurveEditorSettings.hRangeMin = 0.0f;
        m_CurveEditorSettings.vRangeMin = 0.0F;
        m_CurveEditorSettings.vRangeMax = 1.0f;
        m_CurveEditorSettings.hRangeMax = 1.0F;
        m_CurveEditorSettings.vSlider = false;
        m_CurveEditorSettings.hSlider = false;
        m_CurveEditorSettings.showWrapperPopups = true;
        m_CurveEditorSettings.rectangleToolFlags = CurveEditorSettings.RectangleToolFlags.MiniRectangleTool;
        m_CurveEditorSettings.hTickLabelOffset = 5;
        m_CurveEditorSettings.allowDraggingCurvesAndRegions = true;
        m_CurveEditorSettings.allowDeleteLastKeyInCurve = false;

        TickStyle hTS = new TickStyle();
        hTS.tickColor.color = new Color(0.0f, 0.0f, 0.0f, 0.2f);
        hTS.distLabel = 30;
        hTS.stubs = false;
        hTS.centerLabel = true;
        m_CurveEditorSettings.hTickStyle = hTS;

        TickStyle vTS = new TickStyle();
        vTS.tickColor.color = new Color(0.0f, 0.0f, 0.0f, 0.2f);
        vTS.distLabel = 20;
        vTS.stubs = false;
        vTS.centerLabel = true;
        m_CurveEditorSettings.vTickStyle = vTS;

        m_CurveEditor = new CurveEditor(new Rect(0, 0, 1000, 100), CreateCurveWrapperArray(), false);
        m_CurveEditor.settings = m_CurveEditorSettings;
        m_CurveEditor.leftmargin = 40;
        m_CurveEditor.rightmargin = m_CurveEditor.topmargin = m_CurveEditor.bottommargin = 25;
        m_CurveEditor.SetShownHRangeInsideMargins(m_CurveEditorSettings.hRangeMin, m_CurveEditorSettings.hRangeMax);
        m_CurveEditor.SetShownVRangeInsideMargins(m_CurveEditorSettings.vRangeMin, m_CurveEditorSettings.hRangeMax);
        m_CurveEditor.ignoreScrollWheelUntilClicked = false;

        Undo.undoRedoPerformed += UndoRedoPerformed;
    }

    void UndoRedoPerformed()
    {
        ContentChanged();
    }

    void UpdateRangeBasedOnShownCurves()
    {
        bool hasSignedRange = false;
        for (int i = 0; i < m_AddedCurves.Count; i++)
            hasSignedRange |= m_AddedCurves[i].m_SignedRange;

        float newMinRange = hasSignedRange ? -1.0F : 0.0F;
        if (newMinRange != m_CurveEditorSettings.vRangeMin)
        {
            m_CurveEditorSettings.vRangeMin = newMinRange;
            m_CurveEditor.settings = m_CurveEditorSettings;
            m_CurveEditor.SetShownVRangeInsideMargins(m_CurveEditorSettings.vRangeMin, m_CurveEditorSettings.hRangeMax);
        }
    }

    // Public interface

    public bool IsAdded(SerializedProperty min, SerializedProperty max)
    {
        return FindIndex(min, max) != -1;
    }

    public bool IsAdded(SerializedProperty max)
    {
        return FindIndex(null, max) != -1;
    }

    public void AddCurve(CurveData curveData)
    {
        Add(curveData);
    }

    public void RemoveCurve(SerializedProperty max)
    {
        RemoveCurve(null, max);
    }

    public void RemoveCurve(SerializedProperty min, SerializedProperty max)
    {
        if (Remove(FindIndex(min, max)))
        {
            ContentChanged();
            UpdateRangeBasedOnShownCurves();
        }
    }

    public Color GetCurveColor(SerializedProperty max)
    {
        int index = FindIndex(max);
        if (index >= 0 && index < m_AddedCurves.Count)
        {
            return m_AddedCurves[index].m_Color;
        }
        return new Color(0.8f, 0.8f, 0.8f, 0.7f);
    }

    public void AddCurveDataIfNeeded(string curveName, CurveData curveData)
    {
        // We check if a curve was added by checking our inspector state if a color was set for curveName
        Vector3 c = SessionState.GetVector3(curveName, Vector3.zero);

        if (c != Vector3.zero)
        {
            Color color = new Color(c.x, c.y, c.z);
            curveData.m_Color = color;
            AddCurve(curveData);

            // Remove color from available list
            for (int i = 0; i < m_AvailableColors.Count; ++i)
            {
                if (SameColor(m_AvailableColors[i], color))
                {
                    m_AvailableColors.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void SetVisible(SerializedProperty curveProp, bool visible)
    {
        int index = FindIndex(curveProp);
        if (index >= 0)
        {
            m_AddedCurves[index].m_Visible = visible;
        }
    }

    static bool SameColor(Color c1, Color c2)
    {
        return Mathf.Abs(c1.r - c2.r) < 0.01f &&
            Mathf.Abs(c1.g - c2.g) < 0.01f &&
            Mathf.Abs(c1.b - c2.b) < 0.01f;
    }

    // Private
    //---------
    private int FindIndex(SerializedProperty prop)
    {
        return FindIndex(null, prop);
    }

    private int FindIndex(SerializedProperty min, SerializedProperty max)
    {
        if (max == null)
            return -1;

        if (min == null)
        {
            // Just check max
            for (int i = 0; i < m_AddedCurves.Count; ++i)
                if (m_AddedCurves[i].m_Max == max)
                    return i;
        }
        else
        {
            for (int i = 0; i < m_AddedCurves.Count; ++i)
                if (m_AddedCurves[i].m_Max == max && m_AddedCurves[i].m_Min == min)
                    return i;
        }

        return -1;
    }

    private void Add(CurveData cd)
    {
        m_CurveEditor.SelectNone(); // ensures that newly added curves are moved to front (selected keys will stay on top)

        m_AddedCurves.Add(cd);
        ContentChanged();

        // Cache info
        SessionState.SetVector3(cd.m_UniqueName, new Vector3(cd.m_Color.r, cd.m_Color.g, cd.m_Color.b));

        UpdateRangeBasedOnShownCurves();
    }

    private bool Remove(int index)
    {
        if (index >= 0 && index < m_AddedCurves.Count)
        {
            // Make color available again
            Color color = m_AddedCurves[index].m_Color;
            m_AvailableColors.Add(color);

            // Remove from inspector state
            string curveName = m_AddedCurves[index].m_UniqueName;
            SessionState.EraseVector3(curveName);

            // Remove from list
            m_AddedCurves.RemoveAt(index);

            // When no added curves reset available colors
            if (m_AddedCurves.Count == 0)
            {
                //if (m_AvailableColors.Count != m_Colors.Length)
                //  Debug.LogError ("Color count mismatch : " + m_AvailableColors.Count + " != " + m_Colors.Length);

                m_AvailableColors = new List<Color>(m_Colors);
            }

            return true;
        }

        Debug.Log("Invalid index in ParticleSystemCurveEditor::Remove");
        return false;
    }

    private void RemoveTopMost()
    {
        int topMostCurveID;
        if (m_CurveEditor.GetTopMostCurveID(out topMostCurveID))
        {
            for (int j = 0; j < m_AddedCurves.Count; ++j)
            {
                CurveData cd = m_AddedCurves[j];
                if (cd.m_MaxId == topMostCurveID || cd.m_MinId == topMostCurveID)
                {
                    Remove(j);
                    ContentChanged();
                    UpdateRangeBasedOnShownCurves();
                    return;
                }
            }
        }
    }

    private void RemoveSelected()
    {
        bool anyRemoved = false;
        List<CurveSelection> selection = m_CurveEditor.selectedCurves;
        for (int i = 0; i < selection.Count; ++i)
        {
            int curveId = selection[i].curveID;
            for (int j = 0; j < m_AddedCurves.Count; ++j)
            {
                CurveData cd = m_AddedCurves[j];
                if (cd.m_MaxId == curveId || cd.m_MinId == curveId)
                {
                    anyRemoved |= Remove(j);
                    break;
                }
            }
        }
        if (anyRemoved)
        {
            ContentChanged();
            UpdateRangeBasedOnShownCurves();
        }
        m_CurveEditor.SelectNone();
    }

    private void RemoveAll()
    {
        bool anyRemoved = false;
        while (m_AddedCurves.Count > 0)
            anyRemoved |= Remove(0);

        if (anyRemoved)
        {
            ContentChanged();
            UpdateRangeBasedOnShownCurves();
        }
    }

    public Color GetAvailableColor()
    {
        // If no available colors left just use same colors again...
        if (m_AvailableColors.Count == 0)
            m_AvailableColors = new List<Color>(m_Colors);

        int i = m_AvailableColors.Count - 1;
        Color color = m_AvailableColors[i];
        m_AvailableColors.RemoveAt(i);
        return color;
    }

    public void OnGUI(Rect rect)
    {
        Init();
        if (s_Styles == null)
            s_Styles = new Styles();

        // Sizes
        Rect curveEditorRect = new Rect(rect.x, rect.y, rect.width, rect.height - k_PresetsHeight);
        Rect presetRect = new Rect(rect.x, rect.y + curveEditorRect.height, rect.width, k_PresetsHeight);

        // Background
        GUI.Label(curveEditorRect, GUIContent.none, s_Styles.curveEditorBackground);

        // Setup curve editor
        if (Event.current.type == EventType.Repaint)
            m_CurveEditor.rect = curveEditorRect;

        // Clamp the y axis to 1,000,000 for the particle curve.
        foreach (CurveWrapper cw in m_CurveEditor.animationCurves)
        {
            if (cw.getAxisUiScalarsCallback != null && cw.setAxisUiScalarsCallback != null)
            {
                Vector2 axisUiScalar = cw.getAxisUiScalarsCallback();
                if (axisUiScalar.y > 1000000)
                {
                    axisUiScalar.y = 1000000;
                    cw.setAxisUiScalarsCallback(axisUiScalar);
                }
            }
        }

        DoLabelForTopMostCurve(new Rect(rect.x + 4, rect.y, rect.width - 160, 20));
        DoRemoveSelectedButton(new Rect(curveEditorRect.x, curveEditorRect.y, curveEditorRect.width, 24));
        DoOptimizeCurveButton(new Rect(curveEditorRect.x, curveEditorRect.y, curveEditorRect.width, 24));
        presetRect.x += 30;
        presetRect.width -= 2 * 30;
        PresetCurveButtons(presetRect, rect);

        m_CurveEditor.OnGUI();

        SaveChangedCurves();
    }

    void DoLabelForTopMostCurve(Rect rect)
    {
        if (m_CurveEditor.IsDraggingCurveOrRegion() || m_CurveEditor.selectedCurves.Count <= 1)
        {
            int curveID;

            if (m_CurveEditor.GetTopMostCurveID(out curveID))
            {
                for (int i = 0; i < m_AddedCurves.Count; ++i)
                {
                    if (m_AddedCurves[i].m_MaxId == curveID || m_AddedCurves[i].m_MinId == curveID)
                    {
                        s_Styles.yAxisHeader.normal.textColor = m_AddedCurves[i].m_Color;
                        GUI.Label(rect, m_AddedCurves[i].m_DisplayName, s_Styles.yAxisHeader);
                        return;
                    }
                }
            }
        }
    }

    void SetConstantCurve(CurveWrapper cw, float constantValue)
    {
        Keyframe[] keys = new Keyframe[1];
        keys[0].time = 0.0f;
        keys[0].value = constantValue;
        cw.curve.keys = keys;
        cw.changed = true; // Used in SaveChangedCurves () later in OnGUI
    }

    void SetCurve(CurveWrapper cw, AnimationCurve curve)
    {
        Keyframe[] keys = new Keyframe[curve.keys.Length];
        System.Array.Copy(curve.keys, keys, keys.Length);
        cw.curve.keys = keys;
        cw.changed = true; // Used in SaveChangedCurves () later in OnGUI
    }

    void SetTopMostCurve(DoubleCurve doubleCurve)
    {
        int topMostCurveID;
        if (m_CurveEditor.GetTopMostCurveID(out topMostCurveID))
        {
            for (int j = 0; j < m_AddedCurves.Count; ++j)
            {
                CurveData cd = m_AddedCurves[j];
                if (cd.m_MaxId == topMostCurveID || cd.m_MinId == topMostCurveID)
                {
                    if (doubleCurve.signedRange == cd.m_SignedRange)
                    {
                        if (cd.m_MaxId > 0)
                        {
                            SetCurve(m_CurveEditor.GetCurveWrapperFromID(cd.m_MaxId), doubleCurve.maxCurve);
                        }

                        if (cd.m_MinId > 0)
                        {
                            SetCurve(m_CurveEditor.GetCurveWrapperFromID(cd.m_MinId), doubleCurve.minCurve);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Cannot assign a curves with different signed range");
                    }
                }
            }
        }
    }

    DoubleCurve CreateDoubleCurveFromTopMostCurve()
    {
        int topMostCurveID;
        if (m_CurveEditor.GetTopMostCurveID(out topMostCurveID))
        {
            for (int j = 0; j < m_AddedCurves.Count; ++j)
            {
                CurveData cd = m_AddedCurves[j];
                if (cd.m_MaxId == topMostCurveID || cd.m_MinId == topMostCurveID)
                {
                    AnimationCurve maxCurve = null, minCurve = null;
                    if (cd.m_Min != null)
                        minCurve = cd.m_Min.animationCurveValue;
                    if (cd.m_Max != null)
                        maxCurve = cd.m_Max.animationCurveValue;
                    return new DoubleCurve(minCurve, maxCurve, cd.m_SignedRange);
                }
            }
        }
        return null;
    }

    void PresetDropDown(Rect rect)
    {
        if (EditorGUI.DropdownButton(rect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.inspectorTitlebarText))
        {
            DoubleCurve doubleCurveToSaveAsPreset = CreateDoubleCurveFromTopMostCurve();
            if (doubleCurveToSaveAsPreset != null)
            {
                InitDoubleCurvePresets();
                if (m_DoubleCurvePresets != null)
                {
                    m_DoubleCurvePresets.doubleCurveToSave = CreateDoubleCurveFromTopMostCurve();
                    // We prioritize left first for normal inspector layout (where the inspector is on the right)
                    PopupWindow.Show(rect, m_DoubleCurvePresets);
                }
            }
        }
    }

    int m_LastTopMostCurveID = -1;
    void InitDoubleCurvePresets()
    {
        int topMostCurveID;
        if (m_CurveEditor.GetTopMostCurveID(out topMostCurveID))
        {
            if (m_DoubleCurvePresets == null || m_LastTopMostCurveID != topMostCurveID)
            {
                m_LastTopMostCurveID = topMostCurveID;
                // Selection callback for library window
                System.Action<DoubleCurve> presetSelectedCallback = delegate(DoubleCurve presetDoubleCurve)
                    {
                        SetTopMostCurve(presetDoubleCurve);
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    };
                DoubleCurve doubleCurveToSaveAsPreset = CreateDoubleCurveFromTopMostCurve();
                m_DoubleCurvePresets = new DoubleCurvePresetsContentsForPopupWindow(doubleCurveToSaveAsPreset, presetSelectedCallback);
                m_DoubleCurvePresets.InitIfNeeded();
            }
        }
    }

    void PresetCurveButtons(Rect position, Rect curveEditorRect)
    {
        if (m_CurveEditor.animationCurves.Length == 0)
            return;

        InitDoubleCurvePresets();
        if (m_DoubleCurvePresets == null)
            return;

        DoubleCurvePresetLibrary curveLibrary = m_DoubleCurvePresets.GetPresetLibraryEditor().GetCurrentLib();
        const int maxNumPresets = 9;
        int numPresets = (curveLibrary != null) ? curveLibrary.Count() : 0;
        int showNumPresets = Mathf.Min(numPresets, maxNumPresets);

        float swatchWidth = 30;
        float swatchHeight = 15;
        float spaceBetweenSwatches = 10;
        float presetButtonsWidth = showNumPresets * swatchWidth + (showNumPresets - 1) * spaceBetweenSwatches;
        float flexWidth = (position.width - presetButtonsWidth) * 0.5f;

        // Preset swatch area
        float curY = (position.height - swatchHeight) * 0.5f;
        float curX = 3.0f;
        if (flexWidth > 0)
            curX = flexWidth;

        PresetDropDown(new Rect(curX - 20 + position.x, curY + position.y, 16, 16));

        GUI.BeginGroup(position);

        Color curveColor = Color.white;
        curveColor.a *= 0.6f;
        for (int i = 0; i < showNumPresets; i++)
        {
            if (i > 0)
                curX += spaceBetweenSwatches;

            Rect swatchRect = new Rect(curX, curY, swatchWidth, swatchHeight);
            s_Styles.presetTooltip.tooltip = curveLibrary.GetName(i);
            if (GUI.Button(swatchRect, s_Styles.presetTooltip, GUIStyle.none))
            {
                DoubleCurve presetDoubleCurve = curveLibrary.GetPreset(i) as DoubleCurve;
                if (presetDoubleCurve != null)
                {
                    SetTopMostCurve(presetDoubleCurve);
                    m_CurveEditor.ClearSelection();
                }
            }
            if (Event.current.type == EventType.Repaint)
                curveLibrary.Draw(swatchRect, i);

            curX += swatchWidth;
        }
        GUI.EndGroup();
    }

    // Polynomial curves have limitations on how they have to be authored.
    // Since we don't enforce the layout, we have a button that enforces the curve layout instead.
    void DoOptimizeCurveButton(Rect rect)
    {
        bool optimizeButtonShown = false;
        Vector2 buttonSize = new Vector2(64, 14);
        Rect buttonRect = new Rect(rect.xMax - 80 - buttonSize.x, rect.y + (rect.height - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);

        if (!m_CurveEditor.IsDraggingCurveOrRegion())
        {
            int numValidPolynomialCurve = 0;
            List<CurveSelection> selection = m_CurveEditor.selectedCurves;
            if (selection.Count > 0)
            {
                for (int j = 0; j < selection.Count; ++j)
                {
                    CurveWrapper cw = m_CurveEditor.GetCurveWrapperFromSelection(selection[j]);
                    numValidPolynomialCurve += AnimationUtility.IsValidOptimizedPolynomialCurve(cw.curve) ? 1 : 0;
                }

                if (selection.Count != numValidPolynomialCurve)
                {
                    optimizeButtonShown = true;
                    if (GUI.Button(buttonRect, s_Styles.optimizeCurveText))
                    {
                        for (int j = 0; j < selection.Count; ++j)
                        {
                            CurveWrapper cw = m_CurveEditor.GetCurveWrapperFromSelection(selection[j]);
                            if (!AnimationUtility.IsValidOptimizedPolynomialCurve(cw.curve))
                            {
                                // Reset wrap mode
                                cw.curve.preWrapMode = WrapMode.Clamp;
                                cw.curve.postWrapMode = WrapMode.Clamp;
                                cw.renderer.SetWrap(WrapMode.Clamp, WrapMode.Clamp);

                                AnimationUtility.ConstrainToPolynomialCurve(cw.curve);
                                cw.changed = true; // Used in SaveChangedCurves () later in OnGUI
                            }
                        }
                        m_CurveEditor.SelectNone();
                    }
                }
            }
            else
            {
                // Check if top most curve can be optimized
                int topMostCurveID;
                if (m_CurveEditor.GetTopMostCurveID(out topMostCurveID))
                {
                    CurveWrapper cw = m_CurveEditor.GetCurveWrapperFromID(topMostCurveID);
                    if (!AnimationUtility.IsValidOptimizedPolynomialCurve(cw.curve))
                    {
                        optimizeButtonShown = true;
                        if (GUI.Button(buttonRect, s_Styles.optimizeCurveText))
                        {
                            // Reset wrap mode
                            cw.curve.preWrapMode = WrapMode.Clamp;
                            cw.curve.postWrapMode = WrapMode.Clamp;
                            cw.renderer.SetWrap(WrapMode.Clamp, WrapMode.Clamp);

                            AnimationUtility.ConstrainToPolynomialCurve(cw.curve);
                            cw.changed = true; // Used in SaveChangedCurves () later in OnGUI
                        }
                    }
                }
            }
        }

        if (!optimizeButtonShown)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                GUI.Button(buttonRect, s_Styles.optimizeCurveText);
            }
        }
    }

    void DoRemoveSelectedButton(Rect rect)
    {
        using (new EditorGUI.DisabledScope(m_CurveEditor.animationCurves.Length == 0))
        {
            Vector2 buttonSize = new Vector2(64, 14);
            Rect clearCurvesRect = new Rect(rect.x + rect.width - buttonSize.x - 10, rect.y + (rect.height - buttonSize.y) * 0.5f, buttonSize.x, buttonSize.y);
            if (GUI.Button(clearCurvesRect, s_Styles.removeCurveText))
            {
                if (m_CurveEditor.selectedCurves.Count > 0)
                    RemoveSelected();
                else
                    RemoveTopMost();
            }
        }
    }

    void SaveCurve(SerializedProperty prop, CurveWrapper cw)
    {
        if (cw.curve.keys.Length == 1)
        {
            cw.renderer.SetCustomRange(0.0f, 1.0f);
            cw.wrapColorMultiplier = Color.clear; // Hide wrapping when we only have 1 key
        }
        else
        {
            cw.renderer.SetCustomRange(0.0f, 0.0f);
            cw.wrapColorMultiplier = cw.color;
        }

        prop.animationCurveValue = cw.curve;
        cw.changed = false;
    }

    void SaveChangedCurves()
    {
        CurveWrapper[] curves = m_CurveEditor.animationCurves;

        bool refreshPreviews = false;
        for (int i = 0; i < curves.Length; ++i)
        {
            CurveWrapper cw = curves[i];
            if (cw.changed)
            {
                for (int j = 0; j < m_AddedCurves.Count; ++j)
                {
                    if (m_AddedCurves[j].m_MaxId == cw.id)
                    {
                        SaveCurve(m_AddedCurves[j].m_Max, cw);
                        break;
                    }

                    if (m_AddedCurves[j].IsRegion() && m_AddedCurves[j].m_MinId == cw.id)
                    {
                        SaveCurve(m_AddedCurves[j].m_Min, cw);
                        break;
                    }
                }
                refreshPreviews = true;
            }
        }

        if (refreshPreviews)
        {
            UnityEditorInternal.AnimationCurvePreviewCache.ClearCache();
            HandleUtility.Repaint();
        }
    }

    CurveWrapper CreateCurveWrapper(SerializedProperty curve, int id, int regionId, Color color, bool signedRange,
        CurveWrapper.GetAxisScalarsCallback getAxisScalarsCallback, CurveWrapper.SetAxisScalarsCallback setAxisScalarsCallback)
    {
        CurveWrapper cw = new CurveWrapper();
        cw.id = id;
        cw.regionId = regionId;
        cw.color = color;
        cw.renderer = new NormalCurveRenderer(curve.animationCurveValue);
        cw.renderer.SetWrap(curve.animationCurveValue.preWrapMode, curve.animationCurveValue.postWrapMode);
        if (cw.curve.keys.Length == 1)
        {
            cw.renderer.SetCustomRange(0.0f, 1.0f);
            cw.wrapColorMultiplier = Color.clear; // Hide wrapping when we only have 1 key
        }
        else
        {
            cw.renderer.SetCustomRange(0.0f, 0.0f);
            cw.wrapColorMultiplier = color;
        }
        cw.vRangeMin = signedRange ? -1.0F : 0.0F;
        cw.getAxisUiScalarsCallback = getAxisScalarsCallback;
        cw.setAxisUiScalarsCallback = setAxisScalarsCallback;
        return cw;
    }

    CurveWrapper[] CreateCurveWrapperArray()
    {
        List<CurveWrapper> curveWrappers = new List<CurveWrapper>();
        int regionCounter = 0;
        for (int i = 0; i < m_AddedCurves.Count; i++)
        {
            CurveData cd = m_AddedCurves[i];
            if (cd.m_Visible)
            {
                if (cd.m_Max != null && cd.m_Max.hasMultipleDifferentValues)
                    continue;
                if (cd.m_Min != null && cd.m_Min.hasMultipleDifferentValues)
                    continue;

                int regionId = -1;
                if (cd.IsRegion())
                    regionId = ++regionCounter;

                if (cd.m_Max != null)
                {
                    curveWrappers.Add(CreateCurveWrapper(cd.m_Max, cd.m_MaxId, regionId, cd.m_Color, cd.m_SignedRange, cd.m_GetAxisScalarsCallback, cd.m_SetAxisScalarsCallback));
                }

                if (cd.m_Min != null)
                {
                    curveWrappers.Add(CreateCurveWrapper(cd.m_Min, cd.m_MinId, regionId, cd.m_Color, cd.m_SignedRange, cd.m_GetAxisScalarsCallback, cd.m_SetAxisScalarsCallback));
                }
            }
        }
        return curveWrappers.ToArray();
    }

    void ContentChanged()
    {
        m_CurveEditor.animationCurves = CreateCurveWrapperArray();
        m_CurveEditorSettings.showAxisLabels = m_CurveEditor.animationCurves.Length > 0;
    }
}
