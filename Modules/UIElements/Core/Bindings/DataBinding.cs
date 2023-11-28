// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Binding mode to control how a binding is updated.
    /// </summary>
    /// <remarks>To let the data binding system know that the value in the UI changed, use <see cref="VisualElement.NotifyPropertyChanged"/>.</remarks>
    public enum BindingMode
    {
        /// <summary>
        /// Changes on the data source will be replicated in the UI.
        /// Changes on the UI will be replicated to the data source.
        /// </summary>
        TwoWay,

        /// <summary>
        /// Changes will only be replicated from the UI to the data source for this binding.
        /// </summary>
        ToSource,

        /// <summary>
        /// Changes will only be replicated from the source to the UI for this binding.
        /// </summary>
        ToTarget,

        /// <summary>
        /// Changes will only be replicated once, from the source to the UI. This binding will be ignored on subsequent updates.
        /// </summary>
        ToTargetOnce,
    }

    /// <summary>
    ///  Binding type that enables data synchronization between a property of a data source and a property of a <see cref="VisualElement"/>.
    /// </summary>
    public partial class DataBinding : Binding, IDataSourceProvider
    {
        private static MethodInfo s_UpdateUIMethodInfo;
        internal static MethodInfo updateUIMethod => s_UpdateUIMethodInfo ??= CacheReflectionInfo();

        private static MethodInfo CacheReflectionInfo()
        {
            foreach (var method in typeof(DataBinding).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (method.Name != nameof(UpdateUI))
                    continue;

                if (method.GetParameters().Length != 2) continue;
                return s_UpdateUIMethodInfo = method;
            }

            throw new InvalidOperationException($"Could not find method {nameof(UpdateUI)} by reflection. This is an internal bug. Please report using `Help > Report a Bug...` ");
        }

        private BindingMode m_BindingMode;

        private ConverterGroup m_SourceToUiConverters;
        private ConverterGroup m_UiToSourceConverters;

        /// <summary>
        /// Object that serves as a local source for the binding, and is particularly useful when the data source is not
        /// part of the UI hierarchy, such as a static localization table. If this object is null, the binding resolves
        /// the data source using its normal resolution method.
        /// </summary>
        /// <remarks>
        /// Using a local source does not prevent children of the target from using the hierarchy source.
        /// </remarks>
        [CreateProperty]
        public object dataSource { get; set; }

        /// <summary>
        /// The possible data source types that can be assigned to the binding.
        /// </summary>
        /// <remarks>
        /// This information is only used by the UI Builder as a hint to provide some completion to the data source path field when the effective data source cannot be specified at design time.
        /// </remarks>
        internal Type dataSourceType { get; set; }

        internal string dataSourceTypeString
        {
            get => UxmlUtility.TypeToString(dataSourceType);
            set => dataSourceType = UxmlUtility.ParseType(value);
        }

        /// <summary>
        /// Path from the data source to the value.
        /// </summary>
        [CreateProperty]
        public PropertyPath dataSourcePath { get; set; }

        internal string dataSourcePathString
        {
            get => dataSourcePath.ToString();
            set => dataSourcePath = new PropertyPath(value);
        }

        /// <summary>
        /// Controls how this binding should be updated.
        /// The default value is <see cref="BindingMode.TwoWay"/>.
        /// </summary>
        [CreateProperty]
        public BindingMode bindingMode
        {
            get => m_BindingMode;
            set
            {
                if (m_BindingMode == value)
                    return;

                m_BindingMode = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Returns the <see cref="ConverterGroup"/> used when trying to convert data from the data source to a UI property.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public ConverterGroup sourceToUiConverters
        {
            get
            {
                return m_SourceToUiConverters ??= new ConverterGroup(string.Empty);
            }
        }

        /// <summary>
        /// Returns the <see cref="ConverterGroup"/> used when trying to convert data from a UI property back to the data source.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public ConverterGroup uiToSourceConverters
        {
            get
            {
                return m_UiToSourceConverters ??= new ConverterGroup(string.Empty);
            }
        }

        List<string> m_SourceToUIConvertersString;
        internal string sourceToUiConvertersString
        {
            get => m_SourceToUIConvertersString != null ? string.Join(", ", m_SourceToUIConvertersString) : null;
            set
            {
                m_SourceToUIConvertersString = UxmlUtility.ParseStringListAttribute(value);
                if (m_SourceToUIConvertersString != null)
                {
                    foreach (var id in m_SourceToUIConvertersString)
                    {
                        if (ConverterGroups.TryGetConverterGroup(id, out var group))
                        {
                            ApplyConverterGroupToUI(group);
                        }
                    }
                }
            }
        }

        List<string> m_UiToSourceConvertersString;
        internal string uiToSourceConvertersString
        {
            get => m_UiToSourceConvertersString != null ? string.Join(", ", m_UiToSourceConvertersString) : null;
            set
            {
                m_UiToSourceConvertersString = UxmlUtility.ParseStringListAttribute(value);
                if (m_UiToSourceConvertersString != null)
                {
                    foreach (var id in m_UiToSourceConvertersString)
                    {
                        if (ConverterGroups.TryGetConverterGroup(id, out var group))
                        {
                            ApplyConverterGroupToSource(group);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="DataBinding"/>.
        /// </summary>
        public DataBinding()
        {
            updateTrigger = BindingUpdateTrigger.OnSourceChanged;
        }

        /// <summary>
        /// Applies a <see cref="ConverterGroup"/> to this binding that will be used when converting data between a
        /// UI control to a data source.
        /// </summary>
        /// <remarks>
        /// Converter groups can be queried using <see cref="ConverterGroups.TryGetConverterGroup"/>.
        /// </remarks>
        /// <param name="group">The converter group.</param>
        public void ApplyConverterGroupToSource(ConverterGroup group)
        {
            var localToSource = uiToSourceConverters;
            localToSource.registry.Apply(group.registry);
        }

        /// <summary>
        /// Applies a <see cref="ConverterGroup"/> to this binding that will be used when converting data between a
        /// data source to a UI control.
        /// </summary>
        /// <remarks>
        /// Converter groups can be queried using <see cref="ConverterGroups.TryGetConverterGroup"/>.
        /// </remarks>
        /// <param name="group">The converter group.</param>
        public void ApplyConverterGroupToUI(ConverterGroup group)
        {
            var localToUI = sourceToUiConverters;
            localToUI.registry.Apply(group.registry);
        }

        /// <summary>
        /// Callback called to allow derived classes to update the UI with the resolved value from the data source.
        /// </summary>
        /// <param name="context">Context object containing the necessary information to resolve a binding.</param>
        /// <param name="value">The resolved value from the data source.</param>
        /// <typeparam name="TValue">The type of the <paramref name="value"/></typeparam>
        /// <returns>A <see cref="BindingResult"/> indicating if the binding update succeeded or not.</returns>
        protected internal virtual BindingResult UpdateUI<TValue>(in BindingContext context, ref TValue value)
        {
            var target = context.targetElement;

            // When a field is delayed, we should avoid setting the value.
            var focusController = target.focusController;
            if (null != focusController && focusController.IsFocused(target) && target is IDelayedField {isDelayed: true})
            {
                // Only skip setting the value when the actual input field is focused.
                var leaf = focusController.GetLeafFocusedElement();
                if (leaf is TextElement textElement && textElement.ClassListContains("unity-text-element--inner-input-field-component"))
                {
                    return new BindingResult(BindingStatus.Pending);
                }
            }

            var succeeded = sourceToUiConverters.TrySetValue(ref target, context.bindingId, value, out var returnCode);
            if (succeeded)
                return default;

            var message = GetSetValueErrorString(returnCode, context.dataSource, context.dataSourcePath, target, context.bindingId, value);
            return new BindingResult(BindingStatus.Failure, message);
        }

        /// <summary>
        /// Callback called to allow derived classes to update the data source with the resolved value when a change from the UI is detected.
        /// </summary>
        /// <param name="context">Context object containing the necessary information to resolve a binding.</param>
        /// <param name="value">The resolved value from the data source.</param>
        /// <typeparam name="TValue">The type of the <paramref name="value"/></typeparam>
        /// <returns>A <see cref="BindingResult"/> indicating if the binding update succeeded or not.</returns>
        protected internal virtual BindingResult UpdateSource<TValue>(in BindingContext context, ref TValue value)
        {
            var target = context.dataSource;
            var succeeded = uiToSourceConverters.TrySetValue(ref target, context.dataSourcePath, value, out var returnCode);
            if (succeeded)
                return default;

            var message = GetSetValueErrorString(returnCode, context.targetElement, context.bindingId, context.dataSource, context.dataSourcePath, value);
            return new BindingResult(BindingStatus.Failure, message);
        }

        // Internal for tests
        internal static string GetSetValueErrorString<TValue>(VisitReturnCode returnCode, object source, in PropertyPath sourcePath, object target, in BindingId targetPath, TValue extractedValueFromSource)
        {
            var prefix = $"[UI Toolkit] Could not set value for target of type '<b>{target.GetType().Name}</b>' at path '<b>{targetPath}</b>':";
            switch (returnCode)
            {
                case VisitReturnCode.MissingPropertyBag:
                    return $"{prefix} the type '{target.GetType().Name}' is missing a property bag.";
                case VisitReturnCode.InvalidPath:
                    return $"{prefix} the path is either invalid or contains a null value.";
                case VisitReturnCode.InvalidCast:
                    if (sourcePath.IsEmpty)
                    {
                        if (PropertyContainer.TryGetValue(ref target, targetPath, out object obj) && null != obj)
                        {
                            return null == extractedValueFromSource
                                ? $"{prefix} could not convert from '<b>null</b>' to '<b>{obj.GetType().Name}</b>'."
                                : $"{prefix} could not convert from type '<b>{extractedValueFromSource.GetType().Name}</b>' to type '<b>{obj.GetType().Name}</b>'.";
                        }
                    }

                    if (PropertyContainer.TryGetProperty(ref source, sourcePath, out var property))
                    {
                        if (PropertyContainer.TryGetValue(ref target, targetPath, out object obj) && null != obj)
                        {
                            return null == extractedValueFromSource
                                ? $"{prefix} could not convert from '<b>null ({property.DeclaredValueType().Name})</b>' to '<b>{obj.GetType().Name}</b>'."
                                : $"{prefix} could not convert from type '<b>{extractedValueFromSource.GetType().Name}</b>' to type '<b>{obj.GetType().Name}</b>'.";
                        }
                    }

                    return $"{prefix} conversion failed.";
                case VisitReturnCode.AccessViolation:
                    return $"{prefix} the path is read-only.";
                case VisitReturnCode.Ok: // Can't extract an error message from a success.
                case VisitReturnCode.NullContainer: // Should be checked before trying to set a value.
                case VisitReturnCode.InvalidContainerType: // Target should always be a VisualElement
                    throw new InvalidOperationException($"{prefix} internal data binding error. Please report this using the '<b>Help/Report a bug...</b>' menu item.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
