// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    internal struct ComponentTypeInArchetype: IEquatable<ComponentTypeInArchetype>
    {
        public readonly TypeIndex TypeIndex;

        public bool IsBuffer => TypeIndex.IsBuffer;
        public bool IsTransform => TypeIndex.IsTransform;
        public bool IsCleanupComponent => TypeIndex.IsCleanupComponent;
        public bool IsCleanupSharedComponent => TypeIndex.IsCleanupSharedComponent;
        public bool IsSharedComponent => TypeIndex.IsSharedComponentType;
        public bool IsZeroSized => TypeIndex.IsZeroSized;
        public bool IsChunkComponent => TypeIndex.IsChunkComponent;
        public bool HasEntityReferences => TypeIndex.HasEntityReferences;
        public bool IsEnableable => TypeIndex.IsEnableable;
        public bool IsManagedComponent => TypeIndex.IsManagedComponentInternal;
        public bool IsBakeOnlyType => TypeIndex.IsBakingOnlyType;
        public bool IsChunkSerializable => TypeIndex.IsChunkSerializable;

        public ComponentTypeInArchetype(ComponentType type)
        {
            TypeIndex = type.TypeIndex;
        }

        public ComponentTypeInArchetype(TypeIndex typeIndex)
        {
            TypeIndex = typeIndex;
        }

        public static bool operator==(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex == rhs.TypeIndex;
        }

        public static bool operator!=(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex;
        }

        // The comparison of ComponentTypeInArchetype is used to sort the type arrays in Archetypes
        // The type flags in the upper bits of the type index force the component types into the following order:
        // 1. Entity (Always has type index = 1)
        // 2. Non zero sized IComponentData
        // 3. Non zero sized ICleanupComponentData
        // 4. Dynamic buffer components (IBufferElementData)
        // 5. Cleanup dynamic buffer components (ICleanupBufferElementData)
        // 6. Zero sized IComponentData
        // 7. Zero sized ICleanupComponentData
        // 8. Shared components (ISharedComponentData)
        // 9. Shared cleanup components (ICleanupSharedComponentData)
        //10. Chunk IComponentData
        //11. Chunk ICleanupComponentData
        //12. Chunk Dynamic buffer components (IBufferElementData)
        //13. Chunk cleanup dynamic buffer components (ICleanupBufferElementData)

        public static bool operator<(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex < rhs.TypeIndex;
        }

        public static bool operator>(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex > rhs.TypeIndex;
        }

        public static bool operator<=(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return !(lhs > rhs);
        }

        public static bool operator>=(ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return !(lhs < rhs);
        }

        public static unsafe bool CompareArray(ComponentTypeInArchetype* type1, int typeCount1,
            ComponentTypeInArchetype* type2, int typeCount2)
        {
            if (typeCount1 != typeCount2)
                return false;
            for (var i = 0; i < typeCount1; ++i)
                if (type1[i] != type2[i])
                    return false;
            return true;
        }

        public ComponentType ToComponentType()
        {
            ComponentType type;
            type.TypeIndex = TypeIndex;
            type.AccessModeType = ComponentType.AccessMode.ReadWrite;
            return type;
        }

        public override string ToString()
        {
            return ToComponentType().ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ComponentTypeInArchetype) return (ComponentTypeInArchetype)obj == this;

            return false;
        }

        public override int GetHashCode()
        {
            return TypeIndex.GetHashCode();
        }

        public bool Equals(ComponentTypeInArchetype other)
        {
            return TypeIndex == other.TypeIndex;
        }
    }
}
