// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper class to find a suitable <see cref="Constant{T}"/> type for a <see cref="TypeHandle"/>.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    static class TypeToConstantMapper
    {
        static Dictionary<TypeHandle, Type> s_TypeToConstantTypeCache;

        /// <summary>
        /// Maps <see cref="TypeHandle"/> to a type of <see cref="Constant{T}"/>.
        /// </summary>
        public static Type GetConstantType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantTypeCache == null)
            {
                s_TypeToConstantTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { TypeHandle.Bool, typeof(BooleanConstant) },
                    { TypeHandle.Double, typeof(DoubleConstant) },
                    { TypeHandle.Float, typeof(FloatConstant) },
                    { TypeHandle.Int, typeof(IntConstant) },
                    { TypeHandle.Quaternion, typeof(QuaternionConstant) },
                    { TypeHandle.String, typeof(StringConstant) },
                    { TypeHandle.Vector2, typeof(Vector2Constant) },
                    { TypeHandle.Vector3, typeof(Vector3Constant) },
                    { TypeHandle.Vector4, typeof(Vector4Constant) },
                    { typeof(Color).GenerateTypeHandle(), typeof(ColorConstant) },
                    { typeof(Mesh).GenerateTypeHandle(), typeof(MeshConstant) },
                    { typeof(Texture2D).GenerateTypeHandle(), typeof(Texture2DConstant) },
                    { typeof(Texture3D).GenerateTypeHandle(), typeof(Texture3DConstant) },
                    { typeof(AnimationCurve).GenerateTypeHandle(), typeof(AnimationCurveConstant) },
                    { typeof(Gradient).GenerateTypeHandle(), typeof(GradientConstant) },
                };
            }

            if (s_TypeToConstantTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve();
            if (t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstant);

            return null;
        }
    }
}
