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
            bool allLeftWeighted = anyKeys;
            bool allLeftFree = anyKeys;
            bool allLeftLinear = anyKeys;
            bool allLeftConstant = anyKeys;
            bool allRightWeighted = anyKeys;
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


                if ((key.weightedMode & WeightedMode.In) == WeightedMode.None) allLeftWeighted = false;
                if ((key.weightedMode & WeightedMode.Out) == WeightedMode.None) allRightWeighted = false;
            }
            if (anyKeys)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Clamped Auto"),         allClampedAuto, SetClampedAuto, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Auto"), allAuto, SetAuto, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Free Smooth"),  allFreeSmooth, SetEditable, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Flat"),         allFlat, SetFlat, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Broken"),       allBroken, SetBroken, keyList);
                menu.AddSeparator("");
                menu.AddItem(EditorGUIUtility.TrTextContent("Left Tangent/Free"),      allLeftFree, SetLeftEditable, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Left Tangent/Linear"),    allLeftLinear, SetLeftLinear, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Left Tangent/Constant"),  allLeftConstant, SetLeftConstant, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Left Tangent/Weighted"),  allLeftWeighted, ToggleLeftWeighted, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Right Tangent/Free"),     allRightFree, SetRightEditable, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Right Tangent/Linear"),   allRightLinear, SetRightLinear, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Right Tangent/Constant"), allRightConstant, SetRightConstant, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Right Tangent/Weighted"),  allRightWeighted, ToggleRightWeighted, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Both Tangents/Free"),     allRightFree && allLeftFree, SetBothEditable, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Both Tangents/Linear"),   allRightLinear && allLeftLinear, SetBothLinear, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Both Tangents/Constant"), allRightConstant && allLeftConstant, SetBothConstant, keyList);
                menu.AddItem(EditorGUIUtility.TrTextContent("Both Tangents/Weighted"), allRightWeighted && allLeftWeighted, ToggleBothWeighted, keyList);
            }
            else
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Weighted"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Auto"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Free Smooth"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Flat"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Broken"));
                menu.AddSeparator("");
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Left Tangent/Free"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Left Tangent/Linear"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Left Tangent/Constant"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Left Tangent/Weighted"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Right Tangent/Free"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Right Tangent/Linear"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Right Tangent/Constant"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Right Tangent/Weighted"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Both Tangents/Free"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Both Tangents/Linear"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Both Tangents/Constant"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Both Tangents/Weighted"));
            }
        }

        public void ToggleLeftWeighted(object keysToSet) { ToggleWeighted(WeightedMode.In, (List<KeyIdentifier>)keysToSet); }
        public void ToggleRightWeighted(object keysToSet) { ToggleWeighted(WeightedMode.Out, (List<KeyIdentifier>)keysToSet); }
        public void ToggleBothWeighted(object keysToSet) { ToggleWeighted(WeightedMode.Both, (List<KeyIdentifier>)keysToSet); }
        public void ToggleWeighted(WeightedMode weightedMode, List<KeyIdentifier> keysToSet)
        {
            bool allWeighted = keysToSet.TrueForAll(key => (key.keyframe.weightedMode & weightedMode) == weightedMode);

            List<ChangedCurve> changedCurves = new List<ChangedCurve>();
            foreach (KeyIdentifier keyToSet in keysToSet)
            {
                AnimationCurve animationCurve = keyToSet.curve;
                Keyframe key = keyToSet.keyframe;

                bool weighted = (key.weightedMode & weightedMode) == weightedMode;
                if (weighted == allWeighted)
                {
                    WeightedMode lastWeightedMode = key.weightedMode;
                    key.weightedMode = weighted ? key.weightedMode & ~weightedMode : key.weightedMode | weightedMode;

                    if (key.weightedMode != WeightedMode.None)
                    {
                        TangentMode rightTangentMode = AnimationUtility.GetKeyRightTangentMode(key);
                        TangentMode leftTangentMode = AnimationUtility.GetKeyLeftTangentMode(key);

                        if ((lastWeightedMode & WeightedMode.Out) == WeightedMode.None && (key.weightedMode & WeightedMode.Out) == WeightedMode.Out)
                        {
                            if (rightTangentMode == TangentMode.Linear || rightTangentMode == TangentMode.Constant)
                                AnimationUtility.SetKeyRightTangentMode(ref key, TangentMode.Free);
                            if (keyToSet.key < (animationCurve.length - 1))
                                key.outWeight = 1 / 3.0f;
                        }

                        if ((lastWeightedMode & WeightedMode.In) == WeightedMode.None && (key.weightedMode & WeightedMode.In) == WeightedMode.In)
                        {
                            if (leftTangentMode == TangentMode.Linear || leftTangentMode == TangentMode.Constant)
                                AnimationUtility.SetKeyLeftTangentMode(ref key, TangentMode.Free);
                            if (keyToSet.key > 0)
                                key.inWeight = 1 / 3.0f;
                        }
                    }

                    animationCurve.MoveKey(keyToSet.key, key);
                    AnimationUtility.UpdateTangentsFromModeSurrounding(animationCurve, keyToSet.key);

                    ChangedCurve changedCurve = new ChangedCurve(animationCurve, keyToSet.curveId, keyToSet.binding);
                    if (!changedCurves.Contains(changedCurve))
                        changedCurves.Add(changedCurve);
                }
            }

            updater.UpdateCurves(changedCurves, "Toggle Weighted");
        }

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
