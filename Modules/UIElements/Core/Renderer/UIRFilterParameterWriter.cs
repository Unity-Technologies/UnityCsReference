// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Writes the parameters of a filter pass for one element, by shader property.
    /// </summary>
    /// <remarks>
    /// To draw the pass of many elements together, declare the properties with Unity's instancing macros
    /// (<c>UNITY_DEFINE_INSTANCED_PROP</c>) and enable GPU instancing on the material. UI Toolkit sets the properties
    /// whose names start with <c>_UIE_</c>. The writer is valid only during the
    /// <see cref="PostProcessingPass.writeParametersCallback"/> call.
    ///
    /// The <c>Set</c> methods copy the value that you pass. To compute a value directly where the shader reads it, such as
    /// a matrix, use <see cref="GetOrAddMatrixRef"/>, <see cref="GetOrAddVectorRef"/>, <see cref="GetOrAddColorRef"/>, or
    /// <see cref="GetOrAddFloatRef"/>. To read a value that the pass already wrote, for example through a parameter binding,
    /// use the <c>TryGet</c> methods.
    /// </remarks>
    public ref struct FilterParameterWriter
    {
        readonly FilterParameterValues m_Values;

        internal FilterParameterWriter(FilterParameterValues values)
        {
            m_Values = values;
        }

        /// <summary>Writes a float property.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int nameID, float value) => GetOrAddFloatRef(nameID) = value;

        /// <summary>Writes a float property.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value that the shader reads.</param>
        public void SetFloat(string name, float value) => SetFloat(Shader.PropertyToID(name), value);

        /// <summary>Writes a vector property.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVector(int nameID, Vector4 value) => GetOrAddVectorRef(nameID) = value;

        /// <summary>Writes a vector property.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value that the shader reads.</param>
        public void SetVector(string name, Vector4 value) => SetVector(Shader.PropertyToID(name), value);

        /// <summary>Writes a color property, as (r, g, b, a). No color space conversion is applied.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColor(int nameID, Color value) => GetOrAddColorRef(nameID) = value;

        /// <summary>Writes a color property, as (r, g, b, a). No color space conversion is applied.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value that the shader reads.</param>
        public void SetColor(string name, Color value) => SetColor(Shader.PropertyToID(name), value);

        /// <summary>Writes a matrix property.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMatrix(int nameID, in Matrix4x4 value) => GetOrAddMatrixRef(nameID) = value;

        /// <summary>Writes a matrix property.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value that the shader reads.</param>
        public void SetMatrix(string name, in Matrix4x4 value) => SetMatrix(Shader.PropertyToID(name), value);

        /// <summary>Returns a reference to the value of a float property, to write it in place, and adds the property if this
        /// pass hasn't written it.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <returns>The value that the shader reads. It starts at 0 if this pass hasn't written the property yet, or wrote it
        /// with another type.</returns>
        /// <remarks>The reference is valid until the <see cref="PostProcessingPass.writeParametersCallback"/> call returns. If
        /// the property is then written with another type, the reference no longer changes it.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref float GetOrAddFloatRef(int nameID) => ref GetRef<float>(nameID, FilterParameterKind.Float);

        /// <summary>Returns a reference to the value of a vector property, to write it in place, and adds the property if this
        /// pass hasn't written it.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <returns>The value that the shader reads. It starts at zero if this pass hasn't written the property yet, or wrote
        /// it with a type other than a vector or a color.</returns>
        /// <remarks>The reference is valid until the <see cref="PostProcessingPass.writeParametersCallback"/> call returns. If
        /// the property is then written with another type, the reference no longer changes it.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector4 GetOrAddVectorRef(int nameID) => ref GetRef<Vector4>(nameID, FilterParameterKind.Vector);

        /// <summary>Returns a reference to the value of a color property, as (r, g, b, a), to write it in place, and adds the
        /// property if this pass hasn't written it. No color space conversion is applied.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <returns>The value that the shader reads. It starts at zero if this pass hasn't written the property yet, or wrote
        /// it with a type other than a color or a vector.</returns>
        /// <remarks>A color property is a vector property: <see cref="GetOrAddVectorRef"/> with the same ID returns the same
        /// value. The reference is valid until the <see cref="PostProcessingPass.writeParametersCallback"/> call returns. If the
        /// property is then written with another type, the reference no longer changes it.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Color GetOrAddColorRef(int nameID) => ref GetRef<Color>(nameID, FilterParameterKind.Vector);

        /// <summary>Returns a reference to the value of a matrix property, to write it in place, and adds the property if this
        /// pass hasn't written it.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <returns>The value that the shader reads. It starts at zero if this pass hasn't written the property yet, or wrote
        /// it with another type.</returns>
        /// <remarks>The reference is valid until the <see cref="PostProcessingPass.writeParametersCallback"/> call returns. If
        /// the property is then written with another type, the reference no longer changes it.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Matrix4x4 GetOrAddMatrixRef(int nameID) => ref GetRef<Matrix4x4>(nameID, FilterParameterKind.Matrix);

        /// <summary>Reads a float property that this pass has written, for example through a parameter binding.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads, or 0 when the method returns false.</param>
        /// <returns>True if this pass has written the property as a float.</returns>
        /// <remarks>Unlike <see cref="GetOrAddFloatRef"/>, this method doesn't add the property. It doesn't read the value of
        /// the material.</remarks>
        public bool TryGetFloat(int nameID, out float value) => TryGet(nameID, FilterParameterKind.Float, out value);

        /// <summary>Reads a vector property that this pass has written, for example through a parameter binding.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads, or zero when the method returns false.</param>
        /// <returns>True if this pass has written the property as a vector or a color.</returns>
        /// <remarks>Unlike <see cref="GetOrAddVectorRef"/>, this method doesn't add the property. It doesn't read the value of
        /// the material.</remarks>
        public bool TryGetVector(int nameID, out Vector4 value) => TryGet(nameID, FilterParameterKind.Vector, out value);

        /// <summary>Reads a color property that this pass has written, for example through a parameter binding.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads, as (r, g, b, a), or zero when the method returns false. A
        /// parameter binding has already converted it to the color space of the pass.</param>
        /// <returns>True if this pass has written the property as a color or a vector.</returns>
        /// <remarks>Unlike <see cref="GetOrAddColorRef"/>, this method doesn't add the property. It doesn't read the value of
        /// the material.</remarks>
        public bool TryGetColor(int nameID, out Color value) => TryGet(nameID, FilterParameterKind.Vector, out value);

        /// <summary>Reads a matrix property that this pass has written.</summary>
        /// <param name="nameID">The property ID, from <see cref="Shader.PropertyToID"/>.</param>
        /// <param name="value">The value that the shader reads, or zero when the method returns false.</param>
        /// <returns>True if this pass has written the property as a matrix.</returns>
        /// <remarks>Unlike <see cref="GetOrAddMatrixRef"/>, this method doesn't add the property. It doesn't read the value of
        /// the material.</remarks>
        public bool TryGetMatrix(int nameID, out Matrix4x4 value) => TryGet(nameID, FilterParameterKind.Matrix, out value);

        unsafe bool TryGet<T>(int nameID, FilterParameterKind kind, out T value) where T : unmanaged
        {
            if (m_Values == null)
                throw new InvalidOperationException("The writer is only valid during the parameters callback.");
            if (m_Values.TryGetWritten(nameID, kind, out void* data))
            {
                value = *(T*)data;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe ref T GetRef<T>(int nameID, FilterParameterKind kind) where T : unmanaged
        {
            if (m_Values == null)
                throw new InvalidOperationException("The writer is only valid during the parameters callback.");
            if (FilterInstanceBatch.IsReserved(nameID))
                throw new ArgumentException("UI Toolkit sets this property.", nameof(nameID));
            return ref UnsafeUtility.AsRef<T>(m_Values.Write(nameID, kind));
        }
    }
}
