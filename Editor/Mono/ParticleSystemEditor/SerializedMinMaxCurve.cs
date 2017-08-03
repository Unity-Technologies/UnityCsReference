// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    // Must be in sync with ParticleSystemCurves.h
    internal enum MinMaxCurveState
    {
        k_Scalar = 0,
        k_Curve = 1,
        k_TwoCurves = 2,
        k_TwoScalars = 3
    };

    internal class SerializedMinMaxCurve
    {
        public SerializedProperty scalar;
        public SerializedProperty minScalar;
        public SerializedProperty maxCurve;
        public SerializedProperty minCurve;
        private SerializedProperty minMaxState;

        public ModuleUI m_Module;       // Module that owns this SerializedMinMaxCurve
        private string m_Name;          // Curve name. Used for creating unique name based on ParticleSystem name + Module name + m_Name
        public GUIContent m_DisplayName;
        private bool m_SignedRange;     // True if curve can have negative values
        public float m_DefaultCurveScalar; // Used when switching to curve from scalar and scalar is 0. Ensures a valid y axis range (must be positive)
        public float m_RemapValue;      // Used for remap UI values e.g for rotation: state is in radians but we want UI to show it in degrees
        public bool m_AllowConstant;
        public bool m_AllowRandom;
        public bool m_AllowCurves;

        public SerializedMinMaxCurve(ModuleUI m, GUIContent displayName)
        {
            Init(m, displayName, "curve", false, false, true);
        }

        public SerializedMinMaxCurve(ModuleUI m, GUIContent displayName, string name)
        {
            Init(m, displayName, name, false, false, true);
        }

        public SerializedMinMaxCurve(ModuleUI m, GUIContent displayName, bool signedRange)
        {
            Init(m, displayName, "curve", signedRange, false, true);
        }

        public SerializedMinMaxCurve(ModuleUI m, GUIContent displayName, string name, bool signedRange)
        {
            Init(m, displayName, name, signedRange, false, true);
        }

        public SerializedMinMaxCurve(ModuleUI m, GUIContent displayName, string name, bool signedRange, bool useProp0)
        {
            Init(m, displayName, name, signedRange, useProp0, true);
        }

        public SerializedMinMaxCurve(ModuleUI m, GUIContent displayName, string name, bool signedRange, bool useProp0, bool addCurveIfNeeded)
        {
            Init(m, displayName, name, signedRange, useProp0, addCurveIfNeeded);
        }

        void Init(ModuleUI m, GUIContent displayName, string uniqueName, bool signedRange, bool useProp0, bool addCurveIfNeeded)
        {
            m_Module = m;
            m_DisplayName = displayName;
            m_Name = uniqueName;
            m_SignedRange = signedRange;
            m_RemapValue = 1.0f;
            m_DefaultCurveScalar = 1.0f;
            m_AllowConstant = true;
            m_AllowRandom = true;
            m_AllowCurves = true;

            scalar = useProp0 ? m.GetProperty0(m_Name, "scalar") : m.GetProperty(m_Name, "scalar");
            minScalar = useProp0 ? m.GetProperty0(m_Name, "minScalar") : m.GetProperty(m_Name, "minScalar");
            maxCurve = useProp0 ? m.GetProperty0(m_Name, "maxCurve") : m.GetProperty(m_Name, "maxCurve");
            minCurve = useProp0 ? m.GetProperty0(m_Name, "minCurve") : m.GetProperty(m_Name, "minCurve");
            minMaxState = useProp0 ? m.GetProperty0(m_Name, "minMaxState") : m.GetProperty(m_Name, "minMaxState");

            // Reconstruct added curves when we initialize
            if (addCurveIfNeeded)
            {
                if (state == MinMaxCurveState.k_Curve || state == MinMaxCurveState.k_TwoCurves)
                {
                    if (m_Module.m_ParticleSystemUI.m_ParticleEffectUI.IsParticleSystemUIVisible(m_Module.m_ParticleSystemUI))
                        m.GetParticleSystemCurveEditor().AddCurveDataIfNeeded(GetUniqueCurveName(), CreateCurveData(Color.black));
                }
            }
            m.AddToModuleCurves(maxCurve); // It is enough just to add max
        }

        public MinMaxCurveState state
        {
            get { return (MinMaxCurveState)minMaxState.intValue; }
            set { SetMinMaxState(value, true); }
        }

        public bool stateHasMultipleDifferentValues
        {
            get { return minMaxState.hasMultipleDifferentValues; }
        }

        public bool signedRange
        {
            get { return m_SignedRange; }
        }

        public float maxConstant
        {
            get
            {
                return scalar.floatValue;
            }

            set
            {
                if (!signedRange)
                    value = Mathf.Max(value, 0f);

                scalar.floatValue = value;
            }
        }

        public float minConstant
        {
            get
            {
                return minScalar.floatValue;
            }

            set
            {
                if (!signedRange)
                    value = Mathf.Max(value, 0f);

                minScalar.floatValue = value;
            }
        }

        // Callback for Curve Editor to get axis labels
        public Vector2 GetAxisScalars()
        {
            return new Vector2(m_Module.GetXAxisScalar(), scalar.floatValue * m_RemapValue);
        }

        // Callback for Curve Editor to set axis labels back
        public void SetAxisScalars(Vector2 axisScalars)
        {
            // X axis: TODO: We do not support changing the X values in the curve editor yet
            //m_Module.SetXAxisScalar (axisScalars.x);

            // Y axis:
            float remap = (m_RemapValue == 0.0f) ? 1.0f : m_RemapValue;
            scalar.floatValue = (axisScalars.y / remap);
        }

        public void RemoveCurveFromEditor()
        {
            ParticleSystemCurveEditor sce = m_Module.GetParticleSystemCurveEditor();
            if (sce.IsAdded(GetMinCurve(), maxCurve))
                sce.RemoveCurve(GetMinCurve(), maxCurve);
        }

        public bool OnCurveAreaMouseDown(int button, Rect drawRect, Rect curveRanges)
        {
            if (button == 0)
            {
                ToggleCurveInEditor();
                return true;
            }

            if (button == 1)
            {
                SerializedProperty minCurve = GetMinCurve();
                AnimationCurveContextMenu.Show(drawRect,
                    maxCurve != null ? maxCurve.Copy() : null,
                    minCurve != null ? minCurve.Copy() : null,
                    scalar != null ? scalar.Copy() : null,
                    curveRanges, m_Module.GetParticleSystemCurveEditor());
                return true;
            }

            return false;
        }

        public ParticleSystemCurveEditor.CurveData CreateCurveData(Color color)
        {
            System.Diagnostics.Debug.Assert(state != MinMaxCurveState.k_Scalar); // We should not create curve data for scalars

            return new ParticleSystemCurveEditor.CurveData(GetUniqueCurveName(), m_DisplayName, GetMinCurve(), maxCurve, color, m_SignedRange, GetAxisScalars, SetAxisScalars, m_Module.foldout);
        }

        SerializedProperty GetMinCurve()
        {
            return state == MinMaxCurveState.k_TwoCurves ? minCurve : null;
        }

        public void ToggleCurveInEditor()
        {
            ParticleSystemCurveEditor sce = m_Module.GetParticleSystemCurveEditor();
            if (sce.IsAdded(GetMinCurve(), maxCurve))
                sce.RemoveCurve(GetMinCurve(), maxCurve);
            else
                sce.AddCurve(CreateCurveData(sce.GetAvailableColor()));
        }

        public void SetMinMaxState(MinMaxCurveState newState, bool addToCurveEditor)
        {
            if (newState == state)
                return;

            MinMaxCurveState oldState = state;
            ParticleSystemCurveEditor sce = m_Module.GetParticleSystemCurveEditor();

            if (sce.IsAdded(GetMinCurve(), maxCurve))
            {
                sce.RemoveCurve(GetMinCurve(), maxCurve);
            }

            switch (newState)
            {
                case MinMaxCurveState.k_Curve: SetCurveRequirements(); break;
                case MinMaxCurveState.k_TwoCurves: SetCurveRequirements(); break;
            }

            // Assign state AFTER matching data to new state AND removing curve from curveEditor since it uses current 'state'
            minMaxState.intValue = (int)newState;

            if (addToCurveEditor)
            {
                // Add curve to CurveEditor if needed
                // Keep added to the editor if it was added before
                switch (newState)
                {
                    case MinMaxCurveState.k_TwoCurves:
                    case MinMaxCurveState.k_Curve:
                        sce.AddCurve(CreateCurveData(sce.GetAvailableColor()));
                        break;
                    case MinMaxCurveState.k_Scalar:
                    case MinMaxCurveState.k_TwoScalars:
                        // Scalar do not add anything to the curve editor
                        break;
                    default:
                        Debug.LogError("Unhandled enum value");
                        break;
                }
            }

            // Ensure we draw new icons for properties
            UnityEditorInternal.AnimationCurvePreviewCache.ClearCache();
        }

        void SetCurveRequirements()
        {
            // Abs negative values if we change to curve mode (in curve mode the sign is transfered to the curve)
            scalar.floatValue = Mathf.Abs(scalar.floatValue);

            // Ensure proper y-axis value (0 does not create a valid range)
            if (scalar.floatValue == 0)
                scalar.floatValue = m_DefaultCurveScalar;
        }

        public string GetUniqueCurveName()
        {
            return SerializedModule.Concat(m_Module.GetUniqueModuleName(m_Module.serializedObject.targetObject), m_Name);
        }

        static bool AnimationCurveSupportsProcedural(AnimationCurve curve, ref string failureReason)
        {
            switch (AnimationUtility.IsValidPolynomialCurve(curve))
            {
                case AnimationUtility.PolynomialValid.Valid:
                    return true;
                case AnimationUtility.PolynomialValid.InvalidPreWrapMode:
                    failureReason = "Unsupported curve pre-wrap mode. Loop and ping-pong do not support procedural mode.";
                    break;
                case AnimationUtility.PolynomialValid.InvalidPostWrapMode:
                    failureReason = "Unsupported curve post-wrap mode. Loop and ping-pong do not support procedural mode.";
                    break;
                case AnimationUtility.PolynomialValid.TooManySegments:
                    failureReason = "Curve uses too many keys. Procedural mode does not support more than " + AnimationUtility.GetMaxNumPolynomialSegmentsSupported() + " keys";
                    if (curve.keys[0].time != 0.0f || curve.keys[curve.keys.Length - 1].time != 1.0f)
                        failureReason += " (Additional keys are added to curves that do not start at 0, or do not end at 1)";
                    failureReason += ".";
                    break;
            }
            return false;
        }

        public bool SupportsProcedural(ref string failureReason)
        {
            string maxCurveFailureReason = "Max Curve: ";
            bool isMaxCurveValid = AnimationCurveSupportsProcedural(maxCurve.animationCurveValue, ref maxCurveFailureReason);
            if (!isMaxCurveValid)
                failureReason = maxCurveFailureReason;

            if ((state != MinMaxCurveState.k_TwoCurves) && (state != MinMaxCurveState.k_TwoScalars))
                return isMaxCurveValid;
            else
            {
                string minCurveFailureReason = "Min Curve: ";
                bool isMinCurveValid = AnimationCurveSupportsProcedural(minCurve.animationCurveValue, ref minCurveFailureReason);
                if (isMinCurveValid)
                    failureReason += minCurveFailureReason;
                return isMaxCurveValid && isMinCurveValid;
            }
        }
    }
} // namespace UnityEditor
