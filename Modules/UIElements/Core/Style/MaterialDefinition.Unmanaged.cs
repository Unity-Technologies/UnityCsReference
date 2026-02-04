// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements;

[StructLayout(LayoutKind.Sequential)]
struct UnmanagedMaterialPropertyValue : IEquatable<UnmanagedMaterialPropertyValue>
{
    public int name; // Note: this cannot be serialized (see docs for Shader.PropertyToID)
    public MaterialPropertyValueType type;
    public Vector4 packedValue;
    public EntityId textureValue;

    public static implicit operator UnmanagedMaterialPropertyValue(MaterialPropertyValue mpv)
    {
        return new UnmanagedMaterialPropertyValue
        {
            name = Shader.PropertyToID(mpv.name),
            type = mpv.type,
            packedValue = mpv.packedValue,
            textureValue = mpv.textureValue != null ? mpv.textureValue.GetEntityId() : EntityId.None
        };
    }

    public bool Equals(UnmanagedMaterialPropertyValue other)
    {
        return name == other.name
               && type == other.type
               && packedValue == other.packedValue
               && textureValue == other.textureValue;
    }

    public float GetFloat() { return packedValue.x; }
    public Vector4 GetVector() { return packedValue; }
    public Color GetColor() { return new Color(packedValue.x, packedValue.y, packedValue.z, packedValue.w); }

    public void SetFloat(float v) { packedValue = new Vector4(v, 0, 0, 0); }
    public void SetVector(Vector4 v) { packedValue = v; }
    public void SetColor(Color c) { packedValue = new Vector4(c.r, c.g, c.b, c.a); }
}

// Forcing size to 16 bytes to synchronize with the size of the equivalent struct in native.
// See "Modules/UIElements/Core/Native/Style/InheritedTypes.h"
[StructLayout(LayoutKind.Sequential, Size = 16)]
struct UnmanagedMaterialDefinition : IEquatable<UnmanagedMaterialDefinition>
{
    public static readonly UnmanagedMaterialDefinition Empty = new();

    public UnmanagedRefCountedList<UnmanagedMaterialPropertyValue> propertyValues;
    public EntityId material;

    public UnmanagedMaterialDefinition(MaterialDefinition definition)
    {
        material = definition.material != null ? definition.material.GetEntityId() : EntityId.None;

        List<MaterialPropertyValue> values = definition.propertyValues;

        if (values == null || values.Count == 0)
            return;

        propertyValues.CopyFrom(values);
    }

    public UnmanagedMaterialDefinition(EntityId material, ReadOnlySpan<UnmanagedMaterialPropertyValue> props)
    {
        this.material = material;
        propertyValues.CopyFrom(props);
    }

    public void CopyFrom(UnmanagedMaterialDefinition other)
    {
        material = other.material;
        propertyValues.CopyFrom(other.propertyValues);
    }

    public void CopyFrom(MaterialDefinition other)
    {
        material = other.material != null ? other.material.GetEntityId() : EntityId.None;
        propertyValues.CopyFrom(other.propertyValues);
    }

    public void Dispose()
    {
        propertyValues.Clear();
    }

    public bool Equals(UnmanagedMaterialDefinition other)
    {
        return material.Equals(other.material) && propertyValues.Equals(other.propertyValues);
    }

    public override bool Equals(object obj)
    {
        return obj is UnmanagedMaterialDefinition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(material, propertyValues);
    }

    public static bool operator ==(UnmanagedMaterialDefinition left, UnmanagedMaterialDefinition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UnmanagedMaterialDefinition left, UnmanagedMaterialDefinition right)
    {
        return !left.Equals(right);
    }

    internal MaterialPropertyBlock BuildPropertyBlock()
    {
        if (propertyValues == null || propertyValues.Count == 0)
            return null;

        var mpb = new MaterialPropertyBlock();
        foreach (var value in propertyValues)
        {
            switch (value.type)
            {
                case MaterialPropertyValueType.Float:
                    mpb.SetFloat(value.name, value.GetFloat());
                    break;
                case MaterialPropertyValueType.Vector:
                    mpb.SetVector(value.name, value.GetVector());
                    break;
                case MaterialPropertyValueType.Color:
                    mpb.SetColor(value.name, value.GetColor());
                    break;
                case MaterialPropertyValueType.Texture:
                    if (value.textureValue != EntityId.None)
                        mpb.SetTexture(value.name, (Texture)Resources.EntityIdToObject(value.textureValue));
                    break;
            }
        }
        return mpb;
    }
}
