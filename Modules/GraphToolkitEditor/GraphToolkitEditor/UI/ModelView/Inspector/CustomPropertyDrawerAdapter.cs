// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using SerializedObject = UnityEditor.SerializedObject;

namespace Unity.GraphToolkit.Editor
{
    abstract class FieldWrapper : ScriptableObject
    {
        /// <summary>
        /// The object wrapped by the wrapper.
        /// </summary>
        public object WrappedObject { get; set; }

        /// <summary>
        /// The model that owns the object.
        /// </summary>
        public Model Owner { get; set; }

        /// <summary>
        /// The target of the command to edit the wrapped object.
        /// </summary>
        public ICommandTarget CommandTarget { get; set; }

        /// <summary>
        /// The value of the wrapped object.
        /// </summary>
        public abstract object Value { get; set; }

        void OnValidate()
        {
            // OnValidate is called when a change is done in the UI by the users. We update the model with the new value by dispatching the appropriate command.
            switch (WrappedObject)
            {
                case Constant constant when Owner is GraphElementModel graphElementModelOwner:
                    CommandTarget.Dispatch(new UpdateConstantValueCommand(constant, Value, graphElementModelOwner));
                    break;
                case FieldInfo fieldInfo:
                    CommandTarget.Dispatch(new SetInspectedModelFieldCommand(Value, new[] { Owner }, fieldInfo));
                    break;
            }
        }
    }

    class FieldWrapper<T> : FieldWrapper
    {
        [SerializeField]
        T m_Value;

        public override object Value
        {
            get => m_Value;
            set
            {
                try
                {
                    switch (value)
                    {
                        case EnumValueReference enumValueReference:
                            Value = enumValueReference.Value;
                            break;
                        case Constant constant:
                            Value = constant.ObjectValue;
                            break;
                        default:
                            m_Value = (T)value;
                            break;
                    }
                }
                catch (Exception exception)
                {
                    Debug.Log($"Exception caught while updating the value of {WrappedObject}.");
                    Debug.LogException(exception);
                }
            }
        }
    }

    /// <summary>
    /// Adapter that enables support for building a field for types decorated with the <see cref="CustomPropertyDrawer"/> attribute.
    /// </summary>
    /// <remarks>
    /// This adapter is used to integrate Unity's <see cref="CustomPropertyDrawer"/> when constructing UI fields dynamically in GTK.
    /// It ensures that any type with a custom drawer defined can be correctly displayed and interacted with in the graph inspector and in the node's node options and port constant editors.
    /// </remarks>
    class CustomPropertyDrawerAdapter
    {
        // internal for tests
        internal static Dictionary<Type, Type> wrapperTypes = new(); // Value type to wrapper type (eg: MyType to UnityEditor.GraphToolsFoundation.WrapperTypes.Wrapper_MyType_{s_UniqueId++}).
        internal static ConditionalWeakTable<object, List<(string, FieldWrapper)>> objectWrappers = new(); // Associates a list of wrappers to a model (eg: all wrappers associated with a node). As long as the key is alive, the value stays alive.

        static int s_UniqueId;
        static ModuleBuilder s_ModuleBuilder;

        List<FieldWrapper> m_Wrappers;
        SerializedProperty m_Property;

        CustomPropertyDrawerAdapter(List<FieldWrapper> wrappers, SerializedProperty property)
        {
            m_Wrappers = wrappers;
            m_Property = property;
        }

        /// <summary>
        /// Creates a <see cref="CustomPropertyDrawerAdapter"/> for constants.
        /// </summary>
        /// <param name="constants">The constants.</param>
        /// <param name="commandTargetView">The <see cref="ICommandTarget"/> view to dispatch the command on.</param>
        /// <returns>The new <see cref="CustomPropertyDrawerAdapter"/>.</returns>
        /// <remarks>The <see cref="CustomPropertyDrawerAdapter"/> is used by <see cref="ConstantField"/> to build the field with a type decorated with the <see cref="CustomPropertyDrawer"/> attribute.</remarks>
        internal static CustomPropertyDrawerAdapter Create(IReadOnlyList<Constant> constants, ICommandTarget commandTargetView)
        {
            var wrappers = new List<FieldWrapper>();

            for (var i = 0; i < constants.Count; i++)
            {
                // Get the type of data held by the constant.
                var constantValueType = constants[i].GetTypeHandle().Resolve();

                // Skip if the type doesn't have a custom property drawer.
                if (!EditorBridge.HasCustomPropertyDrawer(constantValueType))
                    continue;

                // Wrap the constant into a ScriptableObject that has a serialized field m_Value of the same data type as the Constant.
                var wrapper = GetOrCreateConstantWrapper(constants[i], constants[i].OwnerModel, commandTargetView);
                if (wrapper != null)
                {
                    wrappers.Add(wrapper);
                }
            }

            return wrappers.Count > 0 ? TryCreateCustomPropertyDrawerAdapter(wrappers) : null;
        }

        /// <summary>
        /// Creates a <see cref="CustomPropertyDrawerAdapter"/> for a field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="commandTargetView">The <see cref="ICommandTarget"/> view to dispatch the command on.</param>
        /// <param name="owners">The models that own the field.</param>
        /// <returns>The new <see cref="CustomPropertyDrawerAdapter"/>.</returns>
        /// <remarks>The <see cref="CustomPropertyDrawerAdapter"/> is used by <see cref="ModelPropertyField{TValue}"/> to build the field with a type decorated with the <see cref="CustomPropertyDrawer"/> attribute.</remarks>
        internal static CustomPropertyDrawerAdapter Create(string fieldName, ICommandTarget commandTargetView, IReadOnlyList<Model> owners)
        {
            if (owners == null || owners.Count == 0)
                return null;

            // Get the field info associated with the field name.
            var fieldInfo = owners[0].GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                return null;

            var wrappers = new List<FieldWrapper>();
            for (var i = 0; i < owners.Count; i++)
            {
                if (EditorBridge.HasCustomPropertyDrawer(fieldInfo.FieldType))
                {
                    // Wrap the constant into a ScriptableObject that has a serialized field m_Value of the same type as the field.
                    var wrapper = GetOrCreateFieldWrapper(fieldInfo, owners[i], commandTargetView);
                    if (wrapper != null)
                    {
                        wrappers.Add(wrapper);
                    }
                }
            }

            return wrappers.Count > 0 ? TryCreateCustomPropertyDrawerAdapter(wrappers) : null;
        }

        /// <summary>
        /// Updates the value of the <see cref="FieldWrapper{T}"/>s associated with this adapter.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True </returns>
        internal bool UpdateDisplayedValue(object value)
        {
            // Update each wrapper with the new value from the models.
            foreach (var constantWrapper in m_Wrappers)
                constantWrapper.Value = value;

            return true;
        }

        /// <summary>
        /// Builds the field.
        /// </summary>
        /// <param name="labelText">The label displayed in the field.</param>
        /// <returns>The UI for the field.</returns>
        /// <remarks>
        /// We forcefully bind the <see cref="SerializedProperty"/> associated with the adapter to a <see cref="PropertyField"/>. We cannot control what users use to draw their property.
        /// <see cref="PropertyField"/> handles <see cref="SerializedObject.Update"/> and <see cref="SerializedObject.ApplyModifiedProperties"/> internally to effectively update the <see cref="SerializedObject"/>.
        /// It also calls <see cref="PropertyDrawer.CreatePropertyGUI"/>, we do not need to call it manually.
        /// </remarks>
        internal VisualElement Build(string labelText)
        {
            // We force the use of a PropertyField: we cannot control what users use to draw their property, but we need to make sure the SerializeObject is updated properly.
            // PropertyField handles SerializedObject.Update() and ApplyModifiedProperties() internally. It also calls PropertyDrawer.CreatePropertyGUI, we do not need to call it manually.
            var propertyField = new PropertyField(m_Property, labelText);
            propertyField.Bind(m_Property.serializedObject);

            return propertyField;
        }

        static ModuleBuilder GetOrCreateModuleBuilder()
        {
            if (s_ModuleBuilder == null)
            {
                // Create a new dynamic assembly.
                var assemblyName = new AssemblyName
                {
                    Name = "WrapperTypesAssembly"
                };

                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run); // The dynamic assembly can be executed, but not saved on disk.
                s_ModuleBuilder = assemblyBuilder.DefineDynamicModule("WrapperTypesModule");
            }

            // returns a ModuleBuilder used later to define types.
            return s_ModuleBuilder;
        }

        static Type GetOrCreateWrapperType(Type valueType)
        {
            // Only create the wrapper type if it doesn't already exist.
            if (!wrapperTypes.TryGetValue(valueType, out var wrapperType))
            {
                var genericBaseType = typeof(FieldWrapper<>);
                Type[] typeArgs = { valueType };
                var baseType = genericBaseType.MakeGenericType(typeArgs);

                // Creates a new type inside the dynamic module.
                var typeName = $"UnityEditor.GraphToolsFoundation.WrapperTypes.Wrapper_{valueType.Name}_{s_UniqueId++}"; // unique for each wrapper type
                var typeBuilder = GetOrCreateModuleBuilder().DefineType(typeName, TypeAttributes.Public, baseType);
                typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
                wrapperType = typeBuilder.CreateType();

                wrapperTypes.Add(valueType, wrapperType);
            }

            return wrapperType;
        }

        // Creates a wrapper for constants.
        static FieldWrapper GetOrCreateConstantWrapper(Constant constant, GraphElementModel owner, ICommandTarget commandTarget)
        {
            if (owner == null)
                return null;

            string id;
            var ownerModel = owner;
            PortModel ownerPort = null;
            if (ownerModel is PortModel port)
            {
                id = port.UniqueName;
                ownerModel = port.NodeModel;
                ownerPort = port;
            }
            else
            {
                switch (owner)
                {
                    case ISingleInputPortNodeModel singleInputPortNodeModel:
                        id = singleInputPortNodeModel.InputPort.UniqueName;
                        ownerPort = singleInputPortNodeModel.InputPort;
                        break;
                    case ISingleOutputPortNodeModel singleOutputPortNodeModel:
                        id = singleOutputPortNodeModel.OutputPort.UniqueName;
                        ownerPort = singleOutputPortNodeModel.OutputPort;
                        break;
                    default:
                        id = constant.ToString();
                        break;
                }
            }

            var wrappers = objectWrappers.GetOrCreateValue(ownerModel);

            // If a wrapper already exists for that constant, return it.
            foreach (var (wrapperId, existingWrapper) in wrappers)
            {
                if (existingWrapper.WrappedObject is Constant cst && cst.Type == constant.Type && string.Equals(wrapperId, id) && existingWrapper.CommandTarget == commandTarget)
                {
                    existingWrapper.WrappedObject = constant; // Constant might have been recreated e.g. by changing port types back and forth.
                    return existingWrapper;
                }
            }

            // Else, create a new one.
            var valueType = constant.GetTypeHandle().Resolve();
            var wrapperType = GetOrCreateWrapperType(valueType);
            var wrapper = ScriptableObject.CreateInstance(wrapperType) as FieldWrapper;
            if (wrapper != null)
            {
                wrapper.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSave;
                wrapper.Owner = ownerPort ?? owner;
                wrapper.CommandTarget = commandTarget;
                wrapper.WrappedObject = constant;
                wrapper.Value = constant.ObjectValue ?? constant.DefaultValue;
                wrappers.Add((id, wrapper));
            }

            return wrapper;
        }

        // Creates a wrapper for a field.
        static FieldWrapper GetOrCreateFieldWrapper(FieldInfo field, Model owner, ICommandTarget commandTarget)
        {
            var wrappers = objectWrappers.GetOrCreateValue(owner);

            // If a wrapper already exists for that field, return it.
            foreach (var (wrapperId, existingWrapper) in wrappers)
            {
                if (existingWrapper.WrappedObject is FieldInfo && string.Equals(wrapperId, field.Name))
                    return existingWrapper;
            }

            // Else, create a new one.
            var wrapperType = GetOrCreateWrapperType(field.FieldType);
            // Create a scriptable object with the wrapper type that was generated.
            var wrapper = ScriptableObject.CreateInstance(wrapperType) as FieldWrapper;
            if (wrapper != null)
            {
                wrapper.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSave;
                wrapper.Owner = owner;
                wrapper.CommandTarget = commandTarget;
                wrapper.WrappedObject = field;
                wrappers.Add((field.Name, wrapper));
            }

            return wrapper;
        }

        static CustomPropertyDrawerAdapter TryCreateCustomPropertyDrawerAdapter(List<FieldWrapper> wrappers)
        {
            // Wrap the wrappers (that are ScriptableObjects) in a SerializedObject.
            var targetObjects = new Object[wrappers.Count];
            for (var i = 0; i < wrappers.Count; i++)
                targetObjects[i] = wrappers[i];

            // Create a SerializedObject that inspects the serialized field (m_Value) of the targets.
            var serializedObject = new SerializedObject(targetObjects);

            // The field is exposed as SerializedProperty.
            var property = serializedObject.FindProperty("m_Value");

            return property != null ? new CustomPropertyDrawerAdapter(wrappers, property) : null;
        }

        internal static void RemoveCommandTarget(ICommandTarget commandTarget)
        {
            foreach (var wrappers in objectWrappers)
            {
                for (int i = 0; i < wrappers.Value.Count; ++i)
                {
                    var wrapper = wrappers.Value[i].Item2;
                    if (wrapper.CommandTarget == commandTarget)
                    {
                        wrappers.Value.RemoveAt(i);
                        Object.DestroyImmediate(wrapper);
                        --i;
                    }
                }
            }
        }

        internal class TestAccess
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static FieldWrapper GetOrCreateConstantWrapper(Constant constant, GraphElementModel owner, ICommandTarget commandTarget) =>
                CustomPropertyDrawerAdapter.GetOrCreateConstantWrapper(constant, owner, commandTarget);

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static Type GetOrCreateWrapperType(Type valueType) => CustomPropertyDrawerAdapter.GetOrCreateWrapperType(valueType);
        }
    }
}
