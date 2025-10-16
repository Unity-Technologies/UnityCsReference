// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit
{
    partial struct TypeHandle
    {
        /// <summary>
        /// The Automatic type.
        /// </summary>
        public static TypeHandle Automatic { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("__AUTOMATIC", "Automatic");

        /// <summary>
        /// The MissingType type.
        /// </summary>
        public static TypeHandle MissingType { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("__MISSINGTYPE");

        /// <summary>
        /// The UnknownType type.
        /// </summary>
        public static TypeHandle Unknown { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Unknown), "__UNKNOWN");

        /// <summary>
        /// The ExecutionFlow type.
        /// </summary>
        public static TypeHandle ExecutionFlow { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(ExecutionFlow), "__EXECUTIONFLOW");

        /// <summary>
        /// The Subgraph type.
        /// </summary>
        public static TypeHandle Subgraph { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Subgraph), "__SUBGRAPH");

        /// <summary>
        /// The MissingPort type.
        /// </summary>
        public static TypeHandle MissingPort { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(MissingPort));

        /// <summary>
        /// The C# bool type.
        /// </summary>
        public static TypeHandle Bool { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(bool));

        /// <summary>
        /// The C# void type.
        /// </summary>
        public static TypeHandle Void { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(void));

        /// <summary>
        /// The C# char type.
        /// </summary>
        public static TypeHandle Char { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(char));

        /// <summary>
        /// The C# double type.
        /// </summary>
        public static TypeHandle Double { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(double));

        /// <summary>
        /// The C# float type.
        /// </summary>
        public static TypeHandle Float { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(float));

        /// <summary>
        /// The C# int type.
        /// </summary>
        public static TypeHandle Int { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(int));

        /// <summary>
        /// The C# uint type.
        /// </summary>
        public static TypeHandle UInt { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(uint));

        /// <summary>
        /// The C# long type.
        /// </summary>
        public static TypeHandle Long { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(long));

        /// <summary>
        /// The C# object type.
        /// </summary>
        public static TypeHandle Object { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(object));

        /// <summary>
        /// The UnityEngine.GameObject type.
        /// </summary>
        public static TypeHandle GameObject { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(GameObject));

        /// <summary>
        /// The C# string type.
        /// </summary>
        public static TypeHandle String { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(string));

        /// <summary>
        /// The UnityEngine.Vector2 type.
        /// </summary>
        public static TypeHandle Vector2 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector2));

        /// <summary>
        /// The UnityEngine.Vector3 type.
        /// </summary>
        public static TypeHandle Vector3 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector3));

        /// <summary>
        /// The UnityEngine.Vector4 type.
        /// </summary>
        public static TypeHandle Vector4 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector4));

        /// <summary>
        /// The UnityEngine.Color type.
        /// </summary>
        public static TypeHandle Color { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Color));

        /// <summary>
        /// The UnityEngine.Quaternion type.
        /// </summary>
        public static TypeHandle Quaternion { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Quaternion));

        /// <summary>
        /// The UnityEngine.Texture type.
        /// </summary>
        public static TypeHandle Texture { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture));

        /// <summary>
        /// The UnityEngine.Texture2D type.
        /// </summary>
        public static TypeHandle Texture2D { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture2D));

        /// <summary>
        /// The UnityEngine.Texture2DArray type.
        /// </summary>
        public static TypeHandle Texture2DArray { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture2DArray));

        /// <summary>
        /// The UnityEngine.Texture3D type.
        /// </summary>
        public static TypeHandle Texture3D { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture3D));
    }
}
