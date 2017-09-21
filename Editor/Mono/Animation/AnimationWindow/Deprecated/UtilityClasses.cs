// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace UnityEditor
{
    internal static class CurveUtility
    {
        private static Texture2D iconKey;
        private static Texture2D iconCurve;


        public static int GetPathAndTypeID(string path, Type type)
        {
            return path.GetHashCode() * 27 ^ type.GetHashCode();
        }

        public static Texture2D GetIconCurve()
        {
            if (iconCurve == null)
                iconCurve = EditorGUIUtility.LoadIcon("animationanimated");
            return iconCurve;
        }

        public static Texture2D GetIconKey()
        {
            if (iconKey == null)
                iconKey = EditorGUIUtility.LoadIcon("animationkeyframe");
            return iconKey;
        }

        public static bool HaveKeysInRange(AnimationCurve curve, float beginTime, float endTime)
        {
            // Loop backwards
            for (int i = curve.length - 1; i >= 0; i--)
            {
                if (curve[i].time >= beginTime && curve[i].time < endTime)
                {
                    return true;
                }
            }
            return false;
        }

        public static void RemoveKeysInRange(AnimationCurve curve, float beginTime, float endTime)
        {
            // Loop backwards so key removals don't mess up order
            for (int i = curve.length - 1; i >= 0; i--)
            {
                if (curve[i].time >= beginTime && curve[i].time < endTime)
                {
                    curve.RemoveKey(i);
                }
            }
        }

        public static float CalculateSmoothTangent(Keyframe key)
        {
            if (key.inTangent == Mathf.Infinity) key.inTangent = 0;
            if (key.outTangent == Mathf.Infinity) key.outTangent = 0;
            return (key.outTangent + key.inTangent) * 0.5f;
        }

        // Move me to CurveEditor.cs
        public static void SetKeyModeFromContext(AnimationCurve curve, int keyIndex)
        {
            Keyframe key = curve[keyIndex];
            bool broken = false;
            bool smoothTangent = false;

            if (keyIndex > 0)
            {
                if (AnimationUtility.GetKeyBroken(curve[keyIndex - 1]))
                    broken = true;
                TangentMode prevTangentMode = AnimationUtility.GetKeyRightTangentMode(curve[keyIndex - 1]);
                if (prevTangentMode == TangentMode.ClampedAuto || prevTangentMode == TangentMode.Auto)
                    smoothTangent = true;
            }
            if (keyIndex < curve.length - 1)
            {
                if (AnimationUtility.GetKeyBroken(curve[keyIndex + 1]))
                    broken = true;
                TangentMode nextTangentMode = AnimationUtility.GetKeyLeftTangentMode(curve[keyIndex + 1]);
                if (nextTangentMode == TangentMode.ClampedAuto || nextTangentMode == TangentMode.Auto)
                    smoothTangent = true;
            }

            AnimationUtility.SetKeyBroken(ref key, broken);

            if (broken && !smoothTangent)
            {
                if (keyIndex > 0) AnimationUtility.SetKeyLeftTangentMode(ref key, AnimationUtility.GetKeyRightTangentMode(curve[keyIndex - 1]));
                if (keyIndex < curve.length - 1) AnimationUtility.SetKeyRightTangentMode(ref key, AnimationUtility.GetKeyLeftTangentMode(curve[keyIndex + 1]));

                // Keys at boundaries.  Make sure left and right tangent modes are the same.
                if (keyIndex == 0) AnimationUtility.SetKeyLeftTangentMode(ref key, AnimationUtility.GetKeyRightTangentMode(key));
                if (keyIndex == curve.length - 1) AnimationUtility.SetKeyRightTangentMode(ref key, AnimationUtility.GetKeyLeftTangentMode(key));
            }
            else
            {
                // If both neighbors or only neighbor are set to TangentMode.Auto or TangentMode.ClampedAuto, set new key to this mode as well.
                // If there are no neighbors, set new key to TangentMode.ClampedAuto.
                // Otherwise, fall back to TangentMode.Free.
                TangentMode mode = TangentMode.Free;
                if ((keyIndex == 0 || AnimationUtility.GetKeyRightTangentMode(curve[keyIndex - 1]) == TangentMode.ClampedAuto) &&
                    (keyIndex == curve.length - 1 || AnimationUtility.GetKeyLeftTangentMode(curve[keyIndex + 1]) == TangentMode.ClampedAuto))
                {
                    mode = TangentMode.ClampedAuto;
                }
                else if ((keyIndex == 0 || AnimationUtility.GetKeyRightTangentMode(curve[keyIndex - 1]) == TangentMode.Auto) &&
                         (keyIndex == curve.length - 1 || AnimationUtility.GetKeyLeftTangentMode(curve[keyIndex + 1]) == TangentMode.Auto))
                {
                    mode = TangentMode.Auto;
                }

                AnimationUtility.SetKeyLeftTangentMode(ref key, mode);
                AnimationUtility.SetKeyRightTangentMode(ref key, mode);
            }

            curve.MoveKey(keyIndex, key);
        }

        static public string GetClipName(AnimationClip clip)
        {
            if (clip == null)
                return "[No Clip]";

            string name = clip.name;

            if ((clip.hideFlags & HideFlags.NotEditable) != 0)
                name += " (Read-Only)";

            return name;
        }

        public static Color GetBalancedColor(Color c)
        {
            return new Color(
                0.15f + 0.75f * c.r,
                0.20f + 0.60f * c.g,
                0.10f + 0.90f * c.b
                );
        }

        public static Color GetPropertyColor(string name)
        {
            Color col = Color.white;

            int type = 0;
            if (name.StartsWith("m_LocalPosition")) type = 1;
            if (name.StartsWith("localEulerAngles")) type = 2;
            if (name.StartsWith("m_LocalScale")) type = 3;

            if (type == 1)
            {
                if (name.EndsWith(".x")) col = Handles.xAxisColor;
                else if (name.EndsWith(".y")) col = Handles.yAxisColor;
                else if (name.EndsWith(".z")) col = Handles.zAxisColor;
            }
            else if (type == 2)
            {
                if (name.EndsWith(".x")) col = AnimEditor.kEulerXColor;
                else if (name.EndsWith(".y")) col = AnimEditor.kEulerYColor;
                else if (name.EndsWith(".z")) col = AnimEditor.kEulerZColor;
            }
            else if (type == 3)
            {
                if (name.EndsWith(".x")) col = GetBalancedColor(new Color(0.7f, 0.4f, 0.4f));
                else if (name.EndsWith(".y")) col = GetBalancedColor(new Color(0.4f, 0.7f, 0.4f));
                else if (name.EndsWith(".z")) col = GetBalancedColor(new Color(0.4f, 0.4f, 0.7f));
            }
            else if (name.EndsWith(".x")) col = Handles.xAxisColor;
            else if (name.EndsWith(".y")) col = Handles.yAxisColor;
            else if (name.EndsWith(".z")) col = Handles.zAxisColor;
            else if (name.EndsWith(".w")) col = new Color(1.0f, 0.5f, 0.0f);
            else if (name.EndsWith(".r")) col = GetBalancedColor(Color.red);
            else if (name.EndsWith(".g")) col = GetBalancedColor(Color.green);
            else if (name.EndsWith(".b")) col = GetBalancedColor(Color.blue);
            else if (name.EndsWith(".a")) col = GetBalancedColor(Color.yellow);
            else if (name.EndsWith(".width")) col = GetBalancedColor(Color.blue);
            else if (name.EndsWith(".height")) col = GetBalancedColor(Color.yellow);
            else
            {
                float rand = Mathf.PI * 2 * (name.GetHashCode() % 1000);
                rand = rand - Mathf.Floor(rand);
                col = GetBalancedColor(Color.HSVToRGB(rand, 1, 1));
            }
            col.a = 1; // Some preference colors do not have full alpha
            return col;
        }
    }

    struct QuaternionCurveTangentCalculation
    {
        AnimationCurve eulerX;
        AnimationCurve eulerY;
        AnimationCurve eulerZ;

        public AnimationCurve GetCurve(int index)
        {
            if (index == 0)
                return eulerX;
            else if (index == 1)
                return eulerY;
            else
                return eulerZ;
        }

        public void SetCurve(int index, AnimationCurve curve)
        {
            if (index == 0)
                eulerX = curve;
            else if (index == 1)
                eulerY = curve;
            else
                eulerZ = curve;
        }

        private Vector3 EvaluateEulerCurvesDirectly(float time)
        {
            return new Vector3(
                eulerX.Evaluate(time),
                eulerY.Evaluate(time),
                eulerZ.Evaluate(time));
        }

        public float CalculateLinearTangent(int fromIndex, int toIndex, int componentIndex)
        {
            AnimationCurve curve = GetCurve(componentIndex);
            return CalculateLinearTangent(curve[fromIndex], curve[toIndex], componentIndex);
        }

        public float CalculateLinearTangent(Keyframe from, Keyframe to, int component)
        {
            float epsilon = 0.01f;
            Vector3 fromEuler = EvaluateEulerCurvesDirectly(to.time);
            Vector3 toEuler = EvaluateEulerCurvesDirectly(from.time);
            Quaternion fromQ = Quaternion.Euler(fromEuler);
            Quaternion toQ = Quaternion.Euler(toEuler);
            Quaternion slerped = Quaternion.Slerp(fromQ, toQ, epsilon);
            Vector3 euler = GetEulerFromQuaternion(slerped, fromEuler);
            switch (component)
            {
                case 0: return (euler.x - fromEuler.x) / epsilon / -(to.time - from.time);
                case 1: return (euler.y - fromEuler.y) / epsilon / -(to.time - from.time);
                case 2: return (euler.z - fromEuler.z) / epsilon / -(to.time - from.time);
                default: return 0;
            }
        }

        public float CalculateSmoothTangent(int index, int component)
        {
            AnimationCurve curve = GetCurve(component);
            if (curve.length < 2)
                return 0;

            // First keyframe
            // slope are set to be the linear slerped slope from this to the right key
            if (index <= 0)
                return CalculateLinearTangent(curve[0], curve[1], component);

            // last keyframe
            // slope is set to be the linear slerped slope from this to the left key
            if (index >= curve.length - 1)
                return CalculateLinearTangent(curve[curve.length - 1], curve[curve.length - 2], component);

            // Keys are on the left and right
            // Calculates the slopes from this key to the left key and the right key.
            float prevTime = curve[index - 1].time;
            float thisTime = curve[index].time;
            float nextTime = curve[index + 1].time;
            Vector3 prevEuler = EvaluateEulerCurvesDirectly(prevTime);
            Vector3 thisEuler = EvaluateEulerCurvesDirectly(thisTime);
            Vector3 nextEuler = EvaluateEulerCurvesDirectly(nextTime);
            Quaternion prevQ = Quaternion.Euler(prevEuler);
            Quaternion thisQ = Quaternion.Euler(thisEuler);
            Quaternion nextQ = Quaternion.Euler(nextEuler);
            if (prevQ.x * thisQ.x + prevQ.y * thisQ.y + prevQ.z * thisQ.z + prevQ.w * thisQ.w < 0)
                prevQ = new Quaternion(-prevQ.x, -prevQ.y, -prevQ.z, -prevQ.w);
            if (nextQ.x * thisQ.x + nextQ.y * thisQ.y + nextQ.z * thisQ.z + nextQ.w * thisQ.w < 0)
                nextQ = new Quaternion(-nextQ.x, -nextQ.y, -nextQ.z, -nextQ.w);

            Quaternion tangent = new Quaternion();
            float dx1 = thisTime - prevTime;
            float dx2 = nextTime - thisTime;
            for (int c = 0; c < 4; c++)
            {
                /*
                float dx1 = curve.GetKey (key).time - curve.GetKey (key-1).time;
                T dy1 = curve.GetKey (key).value - curve.GetKey (key-1).value;

                float dx2 = curve.GetKey (key+1).time - curve.GetKey (key).time;
                T dy2 = curve.GetKey (key+1).value - curve.GetKey (key).value;

                T m1 = SafeDeltaDivide(dy1, dx1);
                T m2 = SafeDeltaDivide(dy2, dx2);

                T m = (1.0F + b) * 0.5F * m1 + (1.0F - b) * 0.5F * m2;
                curve.GetKey (key).inSlope = m; curve.GetKey (key).outSlope = m;
                */

                float dy1 = thisQ[c] - prevQ[c];
                float dy2 = nextQ[c] - thisQ[c];
                float m1 = SafeDeltaDivide(dy1, dx1);
                float m2 = SafeDeltaDivide(dy2, dx2);
                tangent[c] = 0.5F * m1 + 0.5F * m2;
            }

            float small = Mathf.Abs(nextTime - prevTime) * 0.01f;
            Quaternion epsilonBeforeQ = new Quaternion(thisQ.x - tangent.x * small,
                    thisQ.y - tangent.y * small,
                    thisQ.z - tangent.z * small,
                    thisQ.w - tangent.w * small);
            Quaternion epsilonAfterQ = new Quaternion(thisQ.x + tangent.x * small,
                    thisQ.y + tangent.y * small,
                    thisQ.z + tangent.z * small,
                    thisQ.w + tangent.w * small);
            Vector3 epsilonBeforeEuler = GetEulerFromQuaternion(epsilonBeforeQ, thisEuler);
            Vector3 epsilonAfterEuler = GetEulerFromQuaternion(epsilonAfterQ, thisEuler);
            Vector3 tangentEuler = (epsilonAfterEuler - epsilonBeforeEuler) / (small * 2);
            return tangentEuler[component];
        }

        public static Vector3[] GetEquivalentEulerAngles(Quaternion quat)
        {
            Vector3 euler = quat.eulerAngles;
            Vector3[] eulers = new Vector3[2];
            eulers[0] = euler;
            eulers[1] = new Vector3(180 - euler.x, euler.y + 180, euler.z + 180);
            return eulers;
        }

        public static Vector3 GetEulerFromQuaternion(Quaternion q, Vector3 refEuler)
        {
            Vector3[] eulers = GetEquivalentEulerAngles(q);
            for (int i = 0; i < eulers.Length; i++)
            {
                eulers[i] = new Vector3(
                        Mathf.Repeat(eulers[i].x - refEuler.x + 180, 360) + refEuler.x - 180,
                        Mathf.Repeat(eulers[i].y - refEuler.y + 180, 360) + refEuler.y - 180,
                        Mathf.Repeat(eulers[i].z - refEuler.z + 180, 360) + refEuler.z - 180
                        );

                float xRot = Mathf.Repeat(eulers[i].x, 360);
                if (Mathf.Abs(xRot - 90) < 1.0f)
                {
                    float newCombiAngle = eulers[i].z - eulers[i].y;
                    float refCombiAngle = refEuler.z - refEuler.y;
                    float angleDiff = newCombiAngle - refCombiAngle;
                    eulers[i].z = refEuler.z + angleDiff * 0.5f;
                    eulers[i].y = refEuler.y - angleDiff * 0.5f;
                }
                if (Mathf.Abs(xRot - 270) < 1.0f)
                {
                    float newCombiAngle = eulers[i].z + eulers[i].y;
                    float refCombiAngle = refEuler.z + refEuler.y;
                    float angleDiff = newCombiAngle - refCombiAngle;
                    eulers[i].z = refEuler.z + angleDiff * 0.5f;
                    eulers[i].y = refEuler.y + angleDiff * 0.5f;
                }
            }

            //for (int i=0; i<eulers.Length; i++)
            //  debugPoints.Add(new Vector4(eulers[i].x, eulers[i].y, eulers[i].z, time));

            // Find out which euler is closest to reference
            Vector3 euler = eulers[0];
            float dist = (eulers[0] - refEuler).sqrMagnitude;
            for (int i = 1; i < eulers.Length; i++)
            {
                float newDist = (eulers[i] - refEuler).sqrMagnitude;
                if (newDist < dist)
                {
                    dist = newDist;
                    euler = eulers[i];
                }
            }

            return euler;
        }

        public static float SafeDeltaDivide(float dy, float dx)
        {
            if (dx == 0)
                return 0;
            return dy / dx;
        }

        public void UpdateTangentsFromMode(int componentIndex)
        {
            AnimationCurve curve = GetCurve(componentIndex);
            for (int i = 0; i < curve.length; i++)
                UpdateTangentsFromMode(i, componentIndex);
        }

        public void UpdateTangentsFromMode(int index, int componentIndex)
        {
            AnimationCurve curve = GetCurve(componentIndex);

            if (index < 0 || index >= curve.length)
                return;

            Keyframe key = curve[index];
            // Adjust linear tangent
            if (AnimationUtility.GetKeyLeftTangentMode(key) == TangentMode.Linear && index >= 1)
            {
                key.inTangent = CalculateLinearTangent(index, index - 1, componentIndex);
                curve.MoveKey(index, key);
            }
            if (AnimationUtility.GetKeyRightTangentMode(key) == TangentMode.Linear && index + 1 < curve.length)
            {
                key.outTangent = CalculateLinearTangent(index, index + 1, componentIndex);
                curve.MoveKey(index, key);
            }

            // Adjust smooth tangents
            if (AnimationUtility.GetKeyLeftTangentMode(key) == TangentMode.ClampedAuto || AnimationUtility.GetKeyRightTangentMode(key) == TangentMode.ClampedAuto)
            {
                key.inTangent = key.outTangent = CalculateSmoothTangent(index, componentIndex);
                curve.MoveKey(index, key);
            }
        }

        public static void UpdateTangentsFromMode(AnimationCurve curve, AnimationClip clip, EditorCurveBinding curveBinding)
        {
            //      if (RotationCurveInterpolation.GetModeFromCurveData(curveBinding) == RotationCurveInterpolation.Mode.NonBaked)
            //      {
            //          QuaternionCurveTangentCalculation tangentCalculator = new QuaternionCurveTangentCalculation();
            //
            //          int index = RotationCurveInterpolation.GetCurveIndexFromName (curveBinding.propertyName);
            //
            //          for (int i=0;i<3;i++)
            //          {
            //              if (i == index)
            //                  tangentCalculator.SetCurve(i, curve);
            //              else
            //              {
            //                  EditorCurveBinding tempBinding = curveBinding;
            //                      tempBinding.propertyName = "localEulerAngles." + RotationCurveInterpolation.kPostFix[i];
            //
            //                  AnimationCurve clipCurve = AnimationUtility.GetEditorCurve (clip, tempBinding);
            //
            //                  // We need all curves to do quaternion tangent smoothing
            //                  if (clipCurve == null)
            //                      return;
            //                  tangentCalculator.SetCurve(i, clipCurve);
            //              }
            //          }
            //
            //          tangentCalculator.UpdateTangentsFromMode(index);
            //      }
            //      else
            {
                AnimationUtility.UpdateTangentsFromMode(curve);
            }
        }
    }
} // namespace
