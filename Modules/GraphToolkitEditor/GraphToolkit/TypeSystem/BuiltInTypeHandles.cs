// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphToolkit not yet converted
using System;
using Unity.GraphToolkit.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.GraphToolkit
{
    partial struct TypeHandle
    {
        /// <summary>
        /// The Automatic type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Automatic { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("__AUTOMATIC", "Automatic");

        /// <summary>
        /// The MissingType type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle MissingType { get; } = TypeHandleHelpers.GenerateCustomTypeHandle("__MISSINGTYPE");

        /// <summary>
        /// The UnknownType type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Unknown { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Unknown), "__UNKNOWN");

        /// <summary>
        /// The Untyped type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Untyped { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Untyped), "__EXECUTIONFLOW", "Untyped");

        /// <summary>
        /// The Subgraph type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Subgraph { get; } = TypeHandleHelpers.GenerateCustomTypeHandle(typeof(Subgraph), "__SUBGRAPH");

        /// <summary>
        /// The MissingPort type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle MissingPort { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(MissingPort));

        /// <summary>
        /// The C# bool type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Bool { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(bool));

        /// <summary>
        /// The C# void type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Void { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(void));

        /// <summary>
        /// The C# char type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Char { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(char));

        /// <summary>
        /// The C# double type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Double { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(double));

        /// <summary>
        /// The C# float type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Float { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(float));

        /// <summary>
        /// The C# int type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Int { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(int));

        /// <summary>
        /// The C# uint type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle UInt { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(uint));

        /// <summary>
        /// The C# long type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Long { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(long));

        /// <summary>
        /// The C# object type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Object { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(object));

        /// <summary>
        /// The UnityEngine.GameObject type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle GameObject { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(GameObject));

        /// <summary>
        /// The C# string type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle String { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(string));

        /// <summary>
        /// The UnityEngine.Vector2 type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Vector2 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector2));

        /// <summary>
        /// The UnityEngine.Vector3 type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Vector3 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector3));

        /// <summary>
        /// The UnityEngine.Vector4 type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Vector4 { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Vector4));

        /// <summary>
        /// The UnityEngine.Color type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Color { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Color));

        /// <summary>
        /// The UnityEngine.Quaternion type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Quaternion { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Quaternion));

        /// <summary>
        /// The UnityEngine.Texture type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Texture { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture));

        /// <summary>
        /// The UnityEngine.Texture2D type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Texture2D { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture2D));

        /// <summary>
        /// The UnityEngine.Texture2DArray type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Texture2DArray { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture2DArray));

        /// <summary>
        /// The UnityEngine.Texture3D type.
        /// </summary>
        [NoAutoStaticsCleanup] // type system constant; TypeHandle wraps a fixed string identifier that is stable across reloads
        public static TypeHandle Texture3D { get; } = TypeHandleHelpers.GenerateTypeHandle(typeof(Texture3D));
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
