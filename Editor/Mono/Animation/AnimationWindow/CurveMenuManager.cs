// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace UnityEditor
{
    internal class ChangedCurve
    {
        public AnimationCurve curve;
        public int curveId;
        public EditorCurveBinding binding;

        public ChangedCurve(AnimationCurve curve, int curveId, EditorCurveBinding binding)
        {
            this.curve = curve;
            this.curveId = curveId;
            this.binding = binding;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            unchecked
            {
                hash = curve.GetHashCode();
                hash = 33 * hash + binding.GetHashCode();
            }
            return hash;
        }
    }

    internal class KeyIdentifier
    {
        public AnimationCurve curve;
        public int curveId;
        public int key;

        // Used by dopesheet
        public EditorCurveBinding binding;

        public KeyIdentifier(AnimationCurve _curve, int _curveId, int _keyIndex)
        {
            curve = _curve;
            curveId = _curveId;
            key = _keyIndex;
        }

        public KeyIdentifier(AnimationCurve _curve, int _curveId, int _keyIndex, EditorCurveBinding _binding)
        {
            curve = _curve;
            curveId = _curveId;
            key = _keyIndex;
            binding = _binding;
        }

        public Keyframe keyframe { get { return curve[key]; } }
    }

    internal interface CurveUpdater
    {
        void UpdateCurves(List<ChangedCurve> curve, string undoText);
    }

    internal class CurveMenuManager
    {
        CurveUpdater updater;

        public CurveMenuManager(CurveUpdater updater)
        {
            this.updater = updater;
        }

        public void AddTangentMenuItems(GenericMenu menu, List<KeyIdentifier> keyList)
        {
            bool anyKeys = (keyList.Count > 0);
            // Find out which qualities apply to all the keys
            bool allClampedAuto = anyKeys;
            bool allAuto = anyKeys;
            bool allFreeSmooth = anyKeys;
            bool allFlat = anyKeys;
            bool allBroken = anyKeys;
            bool allLeftFree = anyKeys;
            bool allLeftLinear = anyKeys;
            bool allLeftConstant = anyKeys;
            bool allRightFree = anyKeys;
            bool allRightLinear = anyKeys;
            bool allRightConstant = anyKeys;
            foreach (KeyIdentifier sel in keyList)
            {
                Keyframe key = sel.keyframe;
                TangentMode leftMode = AnimationUtility.GetKeyLeftTangentMode(key);
                TangentMode rightMode = AnimationUtility.GetKeyRightTangentMode(key);
                bool broken = AnimationUtility.GetKeyBroken(key);
                if (leftMode != TangentMode.ClampedAuto || rightMode != TangentMode.ClampedAuto) allClampedAuto = false;
                if (leftMode != TangentMode.Auto || rightMode != TangentMode.Auto) allAuto = false;
                if (broken || leftMode != TangentMode.Free || rightMode != TangentMode.Free) allFreeSmooth = false;
                if (broken || leftMode != TangentMode.Free || key.inTangent != 0 || rightMode != TangentMode.Free || key.outTangent != 0) allFlat = false;
                if (!broken) allBroken = false;
                if (!broken || leftMode  != TangentMode.Free) allLeftFree = false;
                if (!broken || leftMode  != TangentMode.Linear) allLeftLinear = false;
                if (!broken || leftMode  != TangentMode.Constant) allLeftConstant = false;
                if (!broken || rightMode != TangentMode.Free) allRightFree = false;
                if (!broken || rightMode != TangentMode.Linear) allRightLinear = false;
                if (!broken || rightMode != TangentMode.Constant) allRightConstant = false;
            }
            if (anyKeys)
            {
                menu.AddItem(EditorGUIUtility.TextContent("Clamped Auto"),         allClampedAuto, SetClampedAuto, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Auto"), allAuto, SetAuto, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Free Smooth"),  allFreeSmooth, SetEditable, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Flat"),         allFlat, SetFlat, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Broken"),       allBroken, SetBroken, keyList);
                menu.AddSeparator("");
                menu.AddItem(EditorGUIUtility.TextContent("Left Tangent/Free"),      allLeftFree, SetLeftEditable, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Left Tangent/Linear"),    allLeftLinear, SetLeftLinear, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Left Tangent/Constant"),  allLeftConstant, SetLeftConstant, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Right Tangent/Free"),     allRightFree, SetRightEditable, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Right Tangent/Linear"),   allRightLinear, SetRightLinear, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Right Tangent/Constant"), allRightConstant, SetRightConstant, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Both Tangents/Free"),     allRightFree && allLeftFree, SetBothEditable, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Both Tangents/Linear"),   allRightLinear && allLeftLinear, SetBothLinear, keyList);
                menu.AddItem(EditorGUIUtility.TextContent("Both Tangents/Constant"), allRightConstant && allLeftConstant, SetBothConstant, keyList);
            }
            else
            {
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Clamped Auto"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Auto"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Free Smooth"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Flat"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Broken"));
                menu.AddSeparator("");
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Left Tangent/Free"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Left Tangent/Linear"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Left Tangent/Constant"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Right Tangent/Free"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Right Tangent/Linear"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Right Tangent/Constant"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Both Tangents/Free"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Both Tangents/Linear"));
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Both Tangents/Constant"));
            }
        }

        // Popup menu callbacks for tangents
        public void SetClampedAuto(object keysToSet) { SetBoth(TangentMode.ClampedAuto, (List<KeyIdentifier>)keysToSet); }
        public void SetAuto(object keysToSet) { SetBoth(TangentMode.Auto, (List<KeyIdentifier>)keysToSet); }
        public void SetEditable(object keysToSet) { SetBoth(TangentMode.Free, (List<KeyIdentifier>)keysToSet); }
        public void SetFlat(object keysToSet) { SetBoth(TangentMode.Free, (List<KeyIdentifier>)keysToSet); Flatten((List<KeyIdentifier>)keysToSet); }
        public void SetBoth(TangentMode mode, List<KeyIdentifier> keysToSet)
        {
            List<ChangedCurve> changedCurves = new List<ChangedCurve>();
            foreach (KeyIdentifier keyToSet in keysToSet)
            {
                AnimationCurve animationCurve = keyToSet.curve;
                Keyframe key = keyToSet.keyframe;
                AnimationUtility.SetKeyBroken(ref key, false);
                AnimationUtility.SetKeyRightTangentMode(ref key, mode);
                AnimationUtility.SetKeyLeftTangentMode(ref key, mode);

                // Smooth Tangents based on neighboring nodes
                // Note: not needed since the UpdateTangentsFromModeSurrounding call below will handle it
                //if (mode == TangentMode.ClampedAuto) animationCurve.SmoothTangents(keyToSet.key, 0.0F);
                // Smooth tangents based on existing tangents
                if (mode == TangentMode.Free)
                {
                    float slope = CurveUtility.CalculateSmoothTangent(key);
                    key.inTangent = slope;
                    key.outTangent = slope;
                }
                animationCurve.MoveKey(keyToSet.key, key);
                AnimationUtility.UpdateTangentsFromModeSurrounding(animationCurve, keyToSet.key);

                ChangedCurve changedCurve = new ChangedCurve(animationCurve, keyToSet.curveId, keyToSet.binding);
                if (!changedCurves.Contains(changedCurve))
                    changedCurves.Add(changedCurve);
            }

            updater.UpdateCurves(changedCurves, "Set Tangents");
        }

        public void Flatten(List<KeyIdentifier> keysToSet)
        {
            List<ChangedCurve> changedCurves = new List<ChangedCurve>();
            foreach (KeyIdentifier keyToSet in keysToSet)
            {
                AnimationCurve animationCurve = keyToSet.curve;
                Keyframe key = keyToSet.keyframe;
                key.inTangent = 0;
                key.outTangent = 0;
                animationCurve.MoveKey(keyToSet.key, key);
                AnimationUtility.UpdateTangentsFromModeSurrounding(animationCurve, keyToSet.key);

                ChangedCurve changedCurve = new ChangedCurve(animationCurve, keyToSet.curveId, keyToSet.binding);
                if (!changedCurves.Contains(changedCurve))
                    changedCurves.Add(changedCurve);
            }

            updater.UpdateCurves(changedCurves, "Set Tangents");
        }

        public void SetBroken(object _keysToSet)
        {
            List<ChangedCurve> changedCurves = new List<ChangedCurve>();
            List<KeyIdentifier> keysToSet = (List<KeyIdentifier>)_keysToSet;
            foreach (KeyIdentifier keyToSet in keysToSet)
            {
                AnimationCurve animationCurve = keyToSet.curve;
                Keyframe key = keyToSet.keyframe;
                AnimationUtility.SetKeyBroken(ref key, true);
                if (AnimationUtility.GetKeyRightTangentMode(key) == TangentMode.ClampedAuto || AnimationUtility.GetKeyRightTangentMode(key) == TangentMode.Auto)
                    AnimationUtility.SetKeyRightTangentMode(ref key, TangentMode.Free);
                if (AnimationUtility.GetKeyLeftTangentMode(key) == TangentMode.ClampedAuto || AnimationUtility.GetKeyLeftTangentMode(key) == TangentMode.Auto)
                    AnimationUtility.SetKeyLeftTangentMode(ref key, TangentMode.Free);

                animationCurve.MoveKey(keyToSet.key, key);
                AnimationUtility.UpdateTangentsFromModeSurrounding(animationCurve, keyToSet.key);

                ChangedCurve changedCurve = new ChangedCurve(animationCurve, keyToSet.curveId, keyToSet.binding);
                if (!changedCurves.Contains(changedCurve))
                    changedCurves.Add(changedCurve);
            }

            updater.UpdateCurves(changedCurves, "Set Tangents");
        }

        public void SetLeftEditable(object keysToSet) { SetTangent(0, TangentMode.Free, (List<KeyIdentifier>)keysToSet); }
        public void SetLeftLinear(object keysToSet) { SetTangent(0, TangentMode.Linear, (List<KeyIdentifier>)keysToSet); }
        public void SetLeftConstant(object keysToSet) { SetTangent(0, TangentMode.Constant, (List<KeyIdentifier>)keysToSet); }
        public void SetRightEditable(object keysToSet) { SetTangent(1, TangentMode.Free, (List<KeyIdentifier>)keysToSet); }
        public void SetRightLinear(object keysToSet) { SetTangent(1, TangentMode.Linear, (List<KeyIdentifier>)keysToSet); }
        public void SetRightConstant(object keysToSet) { SetTangent(1, TangentMode.Constant, (List<KeyIdentifier>)keysToSet); }
        public void SetBothEditable(object keysToSet) { SetTangent(2, TangentMode.Free, (List<KeyIdentifier>)keysToSet); }
        public void SetBothLinear(object keysToSet) { SetTangent(2, TangentMode.Linear, (List<KeyIdentifier>)keysToSet); }
        public void SetBothConstant(object keysToSet) { SetTangent(2, TangentMode.Constant, (List<KeyIdentifier>)keysToSet); }
        public void SetTangent(int leftRight, TangentMode mode, List<KeyIdentifier> keysToSet)
        {
            List<ChangedCurve> changedCurves = new List<ChangedCurve>();

            foreach (KeyIdentifier keyToSet in keysToSet)
            {
                AnimationCurve animationCurve = keyToSet.curve;
                Keyframe key = keyToSet.keyframe;
                AnimationUtility.SetKeyBroken(ref key, true);
                if (leftRight == 2)
                {
                    AnimationUtility.SetKeyLeftTangentMode(ref key, mode);
                    AnimationUtility.SetKeyRightTangentMode(ref key, mode);
                }
                else
                {
                    if (leftRight == 0)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(ref key, mode);

                        // Make sure other tangent is handled correctly
                        if (AnimationUtility.GetKeyRightTangentMode(key) == TangentMode.ClampedAuto || AnimationUtility.GetKeyRightTangentMode(key) == TangentMode.Auto)
                            AnimationUtility.SetKeyRightTangentMode(ref key, TangentMode.Free);
                    }
                    else //if (leftRight == 1)
                    {
                        AnimationUtility.SetKeyRightTangentMode(ref key, mode);

                        // Make sure other tangent is handled correctly
                        if (AnimationUtility.GetKeyLeftTangentMode(key) == TangentMode.ClampedAuto || AnimationUtility.GetKeyLeftTangentMode(key) == TangentMode.Auto)
                            AnimationUtility.SetKeyLeftTangentMode(ref key, TangentMode.Free);
                    }
                }

                if (mode == TangentMode.Constant && (leftRight == 0 || leftRight == 2))
                    key.inTangent = Mathf.Infinity;
                if (mode == TangentMode.Constant && (leftRight == 1 || leftRight == 2))
                    key.outTangent = Mathf.Infinity;

                animationCurve.MoveKey(keyToSet.key, key);
                AnimationUtility.UpdateTangentsFromModeSurrounding(animationCurve, keyToSet.key);
                // Debug.Log ("Before " + DebKey (key) + " after: " + DebKey (animationCurve[keyToSet.key]));

                ChangedCurve changedCurve = new ChangedCurve(animationCurve, keyToSet.curveId, keyToSet.binding);
                if (!changedCurves.Contains(changedCurve))
                    changedCurves.Add(changedCurve);
            }

            updater.UpdateCurves(changedCurves, "Set Tangents");
        }

        /*
            string DebKey (Keyframe key) {
                return System.String.Format ("time:{0} value:{1} inTangent:{2} outTangent{3}", key.time, key.value, key.inTangent, key.outTangent);
            }
        */
    }
} // namespace
