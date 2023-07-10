// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Hierarchy;

namespace UnityEngine.UIElements;

/// <summary>
/// The <see cref="VisualNodePropertyRegistry"/> handles all visual node property data access.
/// </summary>
class VisualNodePropertyRegistry
{
    /// <summary>
    /// The type index represents a C# static value shared between all types.
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    struct TypeIndex<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int Index;
    }

    /// <summary>
    /// Class to hold a hierarchy property as a polymorphic object.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    class HierarchyPropertyBinding<TProperty> where TProperty : unmanaged
    {
        public readonly HierarchyPropertyUnmanaged<TProperty> Property;

        public HierarchyPropertyBinding(HierarchyPropertyUnmanaged<TProperty> property)
            => Property = property;
    }

    static int s_InternalPropertyCount;
    static int s_HierarchyPropertyCount;

    /// <summary>
    /// Registers a statically known internal property. This is matches with compile time types in native.
    /// </summary>
    /// <typeparam name="TProperty">The property type to register.</typeparam>
    public static void RegisterInternalProperty<TProperty>()
    {
        if (TypeIndex<TProperty>.Index != 0)
            throw new InvalidOperationException($"{nameof(TProperty)} has already been registered");

        // Use a negative index to identify internal properties.
        TypeIndex<TProperty>.Index = -(++s_InternalPropertyCount);
    }

    /// <summary>
    /// Registers a property type in to the generic hierarchy property system.
    /// </summary>
    /// <typeparam name="TProperty">The property type to register.</typeparam>
    public static void RegisterHierarchyProperty<TProperty>()
    {
        if (TypeIndex<TProperty>.Index != 0)
            throw new InvalidOperationException($"{nameof(TProperty)} has already been registered");

        TypeIndex<TProperty>.Index = ++s_HierarchyPropertyCount;
    }

    /// <summary>
    /// The internal manager that owns the properties.
    /// </summary>
    readonly VisualManager m_Manager;

    /// <summary>
    /// Pointer to the raw property memory. This is used for known internal properties.
    /// </summary>
    readonly unsafe VisualNodePropertyData*[] m_InternalPropertyData;

    /// <summary>
    /// Polymorphic hierarchy properties. This is used to cache the reference to hierarchy properties.
    /// </summary>
    readonly ChunkAllocatingArray<object> m_Bindings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualNodePropertyRegistry"/> class.
    /// </summary>
    /// <param name="manager">The manager that holds the property data.</param>
    public VisualNodePropertyRegistry(VisualManager manager)
    {
        m_Manager = manager ?? throw new ArgumentNullException(nameof(manager));

        // Fetch the internal pointers for known property types.
        unsafe
        {
            m_InternalPropertyData = new VisualNodePropertyData*[s_InternalPropertyCount];
            for (var i = 0; i < s_InternalPropertyCount; i++)
                m_InternalPropertyData[i] = (VisualNodePropertyData*) m_Manager.GetPropertyPtr(i);
        }
    }

    /// <summary>
    /// Checks if the given type is registered as an internal property.
    /// </summary>
    /// <param name="typeIndex">The type index to check.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsInternalProperty(int typeIndex)
        => typeIndex < 0;

    /// <summary>
    /// Gets the strongly typed internal property for the given type index.
    /// </summary>
    /// <param name="typeIndex">The type index to get the property for.</param>
    /// <typeparam name="T">The property type.</typeparam>
    /// <returns>The property accessor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe VisualNodeProperty<T> GetInternalProperty<T>(int typeIndex) where T : unmanaged
        => new(m_InternalPropertyData[-typeIndex - 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    HierarchyPropertyUnmanaged<T> GetHierarchyProperty<T>(int typeIndex) where T : unmanaged
    {
        // @TODO This requires exposing Hierarchy native to managed bindings.
        /*
        var bindingIndex = typeIndex - 1;
        m_Bindings[bindingIndex] ??= new HierarchyPropertyBinding<T>(m_Manager.Hierarchy.GetOrCreatePropertyUnmanaged<T>(nameof(T)));
        return ((HierarchyPropertyBinding<T>) m_Bindings[bindingIndex]).Property;
        */
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the property value of the specified type for the given handle.
    /// </summary>
    /// <param name="handle">The handle to get the property data for.</param>
    /// <typeparam name="T">The property type.</typeparam>
    /// <returns>The property value.</returns>
    public T GetProperty<T>(VisualNodeHandle handle) where T : unmanaged
    {
        var typeIndex = TypeIndex<T>.Index;

        if (typeIndex == 0)
            throw new InvalidOperationException("The property type has not been registered");

        if (IsInternalProperty(typeIndex))
        {
            return GetInternalProperty<T>(typeIndex)[handle];
        }
        else
        {
            return GetHierarchyProperty<T>(typeIndex).GetValue(UnsafeUtility.As<VisualNodeHandle, HierarchyNode>(ref handle));
        }
    }

    /// <summary>
    /// Sets the property value of the specified type for the given handle.
    /// </summary>
    /// <param name="handle">The handle to set the property data for.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="T">The property type.</typeparam>
    /// <returns>The property value.</returns>
    public void SetProperty<T>(VisualNodeHandle handle, in T value) where T : unmanaged
    {
        var typeIndex = TypeIndex<T>.Index;

        if (typeIndex == 0)
            throw new InvalidOperationException("The property type has not been registered");

        if (IsInternalProperty(typeIndex))
        {
            GetInternalProperty<T>(typeIndex)[handle] = value;
        }
        else
        {
            GetHierarchyProperty<T>(typeIndex).SetValue(UnsafeUtility.As<VisualNodeHandle, HierarchyNode>(ref handle), value);
        }
    }

    /// <summary>
    /// Gets the specified property type for the given handle and returns it as a mutable reference.
    /// </summary>
    /// <param name="handle">The handle to get the property data for.</param>
    /// <typeparam name="T">The property type.</typeparam>
    /// <returns>The property value by reference.</returns>
    /// <remarks>
    /// This can only be done for internal properties.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetPropertyRef<T>(VisualNodeHandle handle) where T : unmanaged
    {
        var typeIndex = TypeIndex<T>.Index;

        if (typeIndex == 0)
            throw new InvalidOperationException("The property type has not been registered");

        if (typeIndex > 0)
            throw new InvalidOperationException("The property type is not an internal property");

        return ref GetInternalProperty<T>(typeIndex)[handle];
    }
}
