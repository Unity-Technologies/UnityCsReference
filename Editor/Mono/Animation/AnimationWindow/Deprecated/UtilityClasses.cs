// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace UnityEditor
{
    internal static class CurveUtility
    {
        private static Texture2D iconKey;
        private static Texture2D iconCurve;

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
    }
} // namespace
