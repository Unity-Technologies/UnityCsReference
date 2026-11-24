// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.Mathematics.Editor
{
    [CustomPropertyDrawer(typeof(quaternion))]
    class QuaternionDrawer : PostNormalizedVectorDrawer
    {
        protected override SerializedProperty GetVectorProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("value");
        }

        protected override double4 Normalize(double4 value)
        {
            return math.normalizesafe(new quaternion((float4)value)).value;
        }
    }
}
