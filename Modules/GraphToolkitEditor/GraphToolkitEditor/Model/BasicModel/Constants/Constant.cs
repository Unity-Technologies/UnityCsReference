// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for constants.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class Constant
    {
        GraphElementModel m_OwnerModel;

        /// <summary>
        /// The <see cref="GraphElementModel"/> that owns this constant.
        /// </summary>
        public virtual GraphElementModel OwnerModel
        {
            get => m_OwnerModel;
            set => m_OwnerModel = value;
        }

        /// <summary>
        /// The method to be called after a constant value has been set.
        /// </summary>
        public Action<object> SetterMethod { get; set; }

        /// <summary>
        /// The current value.
        /// </summary>
        public abstract object ObjectValue { get; set; }

        /// <summary>
        /// The default value.
        /// </summary>
        public abstract object DefaultValue { get; }

        /// <summary>
        /// The type of the value.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// Initializes the constant after creation.
        /// </summary>
        /// <param name="constantTypeHandle">The type of value held by this constant.</param>
        public virtual void Initialize(TypeHandle constantTypeHandle)
        {
            if (Type.IsListOrArray())
            {
                ObjectValue = Activator.CreateInstance(Type, 1);
                var elementType = Type.IsArray ? Type.GetElementType() : Type.GetGenericArguments()[0];
                var defaultValue = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;

                if (Type.IsArray)
                    (ObjectValue as Array).SetValue(defaultValue, 0);
                else if (ObjectValue is IList list)
                    list.Add(defaultValue);
            }
            else
            {
                // We ignore constantTypeHandle. Our type is solely determined by T.
                ObjectValue = DefaultValue;
            }
        }

        /// <summary>
        /// Clones the constant.
        /// </summary>
        /// <returns>The cloned constant.</returns>
        public abstract Constant Clone();

        /// <summary>
        /// Gets the <see cref="TypeHandle"/> of the value.
        /// </summary>
        /// <returns>The <see cref="TypeHandle"/> of the value.</returns>
        public virtual TypeHandle GetTypeHandle()
        {
            return Type.GenerateTypeHandle();
        }

        /// <summary>
        /// Tells whether this constant can accept values to type <paramref name="t"/>.
        /// </summary>
        /// <param name="t">The type of value.</param>
        /// <returns>True if this constant can accept values to type <paramref name="t"/>, false otherwise.</returns>
        public virtual bool IsAssignableFrom(Type t)
        {
            return Type.IsAssignableFrom(t);
        }

        /// <summary>
        /// Try to get the value stored in this constant as a <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value"> The value that will be filled with the constant value if possible, or default otherwise.</param>
        /// <typeparam name="T"> The type of value wanted.</typeparam>
        /// <returns>True if the constant contained a compatible value. False otherwise.</returns>
        public bool TryGetValue<T>(out T value)
        {
            var defaultValue = ObjectValue;
            if (defaultValue is EnumValueReference evr)
            {
                defaultValue = evr.ValueAsEnum();
            }
            var constantType = Type;

            if (defaultValue == null && constantType.IsClass && typeof(T).IsAssignableFrom(constantType)) //for classes null is a valid value, provided the type matches
            {
                value = default;
                return true;
            }
            if (defaultValue is T t)
            {
                value = t;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Try to set the value of this constant to a <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value"> The value that is set.</param>
        /// <typeparam name="T"> The type of value given.</typeparam>
        /// <returns>True if the constant was compatible and the value was set. False otherwise.</returns>
        public bool TrySetValue<T>(T value)
        {
            var defaultValue = ObjectValue;
            var constantType = Type;

            if (value == null && (!constantType.IsClass || !typeof(T).IsAssignableFrom(constantType))) //for classes null is a valid value, provided the type matches
            {
                return false;
            }

            if (!constantType.IsAssignableFrom(typeof(T)))
            {
                return false;
            }
            ObjectValue = value;

            return true;
        }

        /// <summary>
        /// Try to set the value of this constant at the given index.
        /// Use <see cref="ResizeCollection(int)"/> to change the size of the collection beforehand.
        /// </summary>
        /// <param name="index">Index in the collection</param>
        /// <param name="value">The value that is set.</param>
        /// <returns>True if the value was successfully set at a valid index. False otherwise.</returns>
        public bool TrySetValueAt(int index, object value)
        {
            var count = Type.IsListOrArray() ? ((IList)ObjectValue).Count : 1;
            if (index < 0 || index >= count)
                return false;

            if (Type.IsArray)
            {
                Array array = (Array)ObjectValue;
                array.SetValue(value, index);
            }
            else if (ObjectValue is IList list)
                list[index] = value;

            return true;
        }

        /// <summary>
        /// Resize the collection to the new size.
        /// </summary>
        /// <param name="newSize">New size of the collection.</param>
        public void ResizeCollection(int newSize)
        {
            if (Type.IsListOrArray())
            {
                var elementType = Type.IsArray ? Type.GetElementType() : Type.GetGenericArguments()[0];
                var defaultValue = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;

                if (Type.IsArray)
                {
                    Array oldArray = (Array)ObjectValue;
                    Array newArray = Array.CreateInstance(elementType, newSize);
                    int lengthToCopy = Math.Min(oldArray.Length, newSize);
                    Array.Copy(oldArray, newArray, lengthToCopy);

                    for (int i = lengthToCopy; i < newSize; i++)
                        newArray.SetValue(defaultValue, i);

                    ObjectValue = newArray;
                }
                else if (ObjectValue is IList list)
                {
                    if (list.Count < newSize)
                    {
                        while (list.Count < newSize)
                            list.Add(defaultValue);
                    }
                    else if (list.Count > newSize)
                    {
                        while (list.Count > newSize)
                            list.RemoveAt(list.Count - 1);
                    }
                }
                
            }
        }
    }
}
