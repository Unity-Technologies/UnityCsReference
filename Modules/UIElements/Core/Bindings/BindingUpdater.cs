// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    class BindingUpdater
    {
        private sealed class CastDataSourceVisitor : ConcreteTypeVisitor
        {
            public DataBinding Binding { get; set; }
            public BindingContext bindingContext { get; set; }
            public BindingResult result { get; set; }

            public void Reset()
            {
                Binding = default;
                bindingContext = default;
                result = default;
            }

            protected override void VisitContainer<TContainer>(ref TContainer container)
            {
                result = Binding.UpdateUI(bindingContext, ref container);
            }
        }

        private sealed class UIPathVisitor : PathVisitor
        {
            public DataBinding binding { get; set; }
            public BindingUpdateStage direction { get; set; }
            public BindingContext bindingContext { get; set; }
            public BindingResult result { get; set; }

            public override void Reset()
            {
                base.Reset();
                binding = default;
                direction = BindingUpdateStage.UpdateUI;
                bindingContext = default;
                result = default;
                ReadonlyVisit = true;
            }

            protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
            {
                result = direction switch
                {
                    BindingUpdateStage.UpdateUI => binding.UpdateUI(bindingContext, ref value),
                    BindingUpdateStage.UpdateSource => binding.UpdateSource(bindingContext, ref value),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        private static readonly CastDataSourceVisitor s_VisitDataSourceAsRootVisitor = new ();
        private static readonly UIPathVisitor s_VisitDataSourceAtPathVisitor = new ();

        public bool ShouldProcessBindingAtStage(Binding bindingObject, BindingUpdateStage stage, bool versionChanged, bool dirty)
        {
            return bindingObject switch
            {
                DataBinding dataBinding => ShouldProcessBindingAtStage(dataBinding, stage, versionChanged, dirty),
                CustomBinding customBinding => ShouldProcessBindingAtStage(customBinding, stage, versionChanged, dirty),
                _ => throw new InvalidOperationException($"Binding type `{TypeUtility.GetTypeDisplayName(bindingObject.GetType())}` is not supported. This is an internal bug. Please report using `Help > Report a Bug...` ")
            };
        }

        private static bool ShouldProcessBindingAtStage(DataBinding dataBinding, BindingUpdateStage stage, bool versionChanged, bool dirty)
        {
            switch (stage)
            {
                case BindingUpdateStage.UpdateUI:
                {
                    if (dataBinding.bindingMode == BindingMode.ToSource)
                        return false;

                    if (dataBinding.updateTrigger == BindingUpdateTrigger.EveryUpdate || dirty)
                        return true;

                    if (dataBinding.bindingMode == BindingMode.ToTargetOnce)
                        return false;

                    return dataBinding.updateTrigger == BindingUpdateTrigger.OnSourceChanged && versionChanged;
                }
                case BindingUpdateStage.UpdateSource:
                {
                    if (dataBinding.bindingMode is BindingMode.ToTarget or BindingMode.ToTargetOnce)
                        return false;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
            return true;
        }

        private bool ShouldProcessBindingAtStage(CustomBinding customBinding, BindingUpdateStage stage, bool versionChanged, bool dirty)
        {
            switch (stage)
            {
                case BindingUpdateStage.UpdateUI:
                {
                    return customBinding.updateTrigger switch
                    {
                        BindingUpdateTrigger.EveryUpdate => true,
                        BindingUpdateTrigger.OnSourceChanged when versionChanged || dirty => true,
                        _ => dirty
                    };
                }
                case BindingUpdateStage.UpdateSource:
                    // Not supported
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }

        public BindingResult UpdateUI(in BindingContext context, Binding bindingObject)
        {
            return bindingObject switch
            {
                DataBinding dataBinding => UpdateUI(in context, dataBinding),
                CustomBinding customBinding => UpdateUI(in context, customBinding),
                _ => throw new InvalidOperationException($"Binding type `{TypeUtility.GetTypeDisplayName(bindingObject.GetType())}` is not supported. This is an internal bug. Please report using `Help > Report a Bug...` ")
            };
        }

        public BindingResult UpdateSource(in BindingContext context, Binding bindingObject)
        {
            return bindingObject switch
            {
                DataBinding dataBinding => UpdateDataSource(in context, dataBinding),
                CustomBinding customBinding => UpdateDataSource(in context, customBinding),
                _ => throw new InvalidOperationException($"Binding type `{TypeUtility.GetTypeDisplayName(bindingObject.GetType())}` is not supported. This is an internal bug. Please report using `Help > Report a Bug...` ")
            };
        }

        private BindingResult UpdateUI(in BindingContext context, DataBinding dataBinding)
        {
            var target = context.targetElement;
            var resolvedDataSource = context.dataSource;
            var resolvedSourcePath = context.dataSourcePath;

            // We can't extra a value from a null data source, so we can do an early exit here.
            if (null == resolvedDataSource)
            {
                var name = string.IsNullOrEmpty(target.name) ? TypeUtility.GetTypeDisplayName(target.GetType()) : target.name;
                var message = $"[UI Toolkit] Could not bind '{name}' because there is no data source.";
                return new BindingResult(BindingStatus.Pending, message);
            }

            // Try to bind against the data source directly.
            if (resolvedSourcePath.IsEmpty)
            {
                // For primitive, strings and enums, we cannot use visitation, since those are not proper containers, so we need to use reflection
                if (!TypeTraits.IsContainer(resolvedDataSource.GetType()))
                {
                    return TryUpdateUIWithNonContainer(context, dataBinding, resolvedDataSource);
                }

                var visitRoot = VisitRoot(dataBinding, ref resolvedDataSource, in context);

                if (!visitRoot.succeeded)
                {
                    var message = GetVisitationErrorString(visitRoot.visitationReturnCode, context);
                    return new BindingResult(BindingStatus.Failure, message);
                }

                return s_VisitDataSourceAsRootVisitor.result;
            }

            var visitAtPath = VisitAtPath(dataBinding, BindingUpdateStage.UpdateUI, ref resolvedDataSource, resolvedSourcePath, in context);

            if (!visitAtPath.succeeded)
            {
                var message = GetVisitationErrorString(visitAtPath.visitationReturnCode, context);
                return new BindingResult(BindingStatus.Failure, message);
            }

            if (visitAtPath.atPathReturnCode != VisitReturnCode.Ok)
            {
                var message = GetExtractValueErrorString(visitAtPath.atPathReturnCode, context.dataSource, resolvedSourcePath);
                return new BindingResult(BindingStatus.Failure, message);
            }

            return visitAtPath.bindingResult;
        }

        private BindingResult UpdateUI(in BindingContext context, CustomBinding customBinding)
        {
            return customBinding.Update(in context);
        }

        private BindingResult UpdateDataSource(in BindingContext context, DataBinding dataBinding)
        {
            var target = context.targetElement;
            var resolvedDataSource = context.dataSource;
            var resolvedSourcePath = context.dataSourcePath;

            // We can't extract a value from a null data source, so we can do an early exit here.
            if (null == resolvedDataSource)
            {
                var name = string.IsNullOrEmpty(target.name) ? TypeUtility.GetTypeDisplayName(target.GetType()) : target.name;
                var message = $"[UI Toolkit] Could not set value on '{name}' because there is no data source.";
                return new BindingResult(BindingStatus.Pending, message);
            }

            // Changing the data source itself is not supported.
            if (resolvedSourcePath.IsEmpty)
            {
                var message = GetRootDataSourceError(resolvedDataSource);
                return new BindingResult(BindingStatus.Failure, message);
            }

            var visitAtPath = VisitAtPath(dataBinding, BindingUpdateStage.UpdateSource, ref target, context.bindingId, in context);

            if (!visitAtPath.succeeded)
            {
                var message = GetVisitationErrorString(visitAtPath.visitationReturnCode, context);
                return new BindingResult(BindingStatus.Failure, message);
            }

            if (visitAtPath.atPathReturnCode != VisitReturnCode.Ok)
            {
                var message = GetExtractValueErrorString(visitAtPath.atPathReturnCode, target, context.bindingId);
                return new BindingResult(BindingStatus.Failure, message);
            }

            return visitAtPath.bindingResult;
        }

        private BindingResult UpdateDataSource(in BindingContext context, CustomBinding customBinding)
        {
            return new BindingResult(BindingStatus.Pending);
        }

        static BindingResult TryUpdateUIWithNonContainer(in BindingContext context, DataBinding binding, object value)
        {
            var type = value.GetType();
            if (type.IsEnum)
            {
                var genericMethod = DataBinding.updateUIMethod.MakeGenericMethod(type);
                return (BindingResult) genericMethod.Invoke(binding, new object[] {context, value});
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                {
                    var v = (bool) value;
                    return binding.UpdateUI(in context, ref v);
                }
                case TypeCode.Byte:
                {
                    var v = (byte) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.Char:
                {
                    var v = (char) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.Double:
                {
                    var v = (double) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.Int16:
                {
                    var v = (short) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.Int32:
                {
                    var v = (int) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.Int64:
                {
                    var v = (long) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.SByte:
                {
                    var v = (sbyte) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.Single:
                {
                    var v = (float) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.String:
                {
                    var v = (string) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.UInt16:
                {
                    var v = (ushort) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.UInt32:
                {
                    var v = (uint) value;
                    return binding.UpdateUI(context, ref v);
                }
                case TypeCode.UInt64:
                {
                    var v = (ulong) value;
                    return binding.UpdateUI(context, ref v);
                }
                default:
                    return new BindingResult(BindingStatus.Failure, "[UI Toolkit] Unsupported primitive type");
            }
        }

        private static (bool succeeded, VisitReturnCode visitationReturnCode, BindingResult bindingResult) VisitRoot(
            DataBinding dataBinding,
            ref object container,
            in BindingContext context)
        {
            s_VisitDataSourceAsRootVisitor.Reset();
            s_VisitDataSourceAsRootVisitor.Binding = dataBinding;
            s_VisitDataSourceAsRootVisitor.bindingContext = context;

            var succeeded = PropertyContainer.TryAccept(s_VisitDataSourceAsRootVisitor, ref container, out var returnCode);
            return (succeeded, returnCode, s_VisitDataSourceAsRootVisitor.result);
        }

        private static (bool succeeded, VisitReturnCode visitationReturnCode, VisitReturnCode atPathReturnCode, BindingResult bindingResult) VisitAtPath<TContainer>(
            DataBinding dataBinding,
            BindingUpdateStage direction,
            ref TContainer container,
            in BindingId path,
            in BindingContext context)
        {
            // Run a visitor to extract the value at the given path and continue to update the UI with a resolved value.
            s_VisitDataSourceAtPathVisitor.Reset();
            s_VisitDataSourceAtPathVisitor.binding = dataBinding;
            s_VisitDataSourceAtPathVisitor.direction = direction;
            s_VisitDataSourceAtPathVisitor.Path = path;
            s_VisitDataSourceAtPathVisitor.bindingContext = context;

            var succeeded = PropertyContainer.TryAccept(s_VisitDataSourceAtPathVisitor, ref container, out var returnCode);
            return (succeeded, returnCode, s_VisitDataSourceAtPathVisitor.ReturnCode, s_VisitDataSourceAtPathVisitor.result);
        }

        // Internal for tests
        internal static string GetVisitationErrorString(VisitReturnCode returnCode, in BindingContext context)
        {
            var prefix = $"[UI Toolkit] Could not bind target of type '<b>{context.targetElement.GetType().Name}</b>' at path '<b>{context.bindingId}</b>':";
            switch (returnCode)
            {
                case VisitReturnCode.InvalidContainerType:
                    return $"{prefix} the data source cannot be a primitive, a string or an enum.";
                case VisitReturnCode.MissingPropertyBag:
                    return $"{prefix} the data source is missing a property bag.";
                case VisitReturnCode.InvalidPath:
                    return $"{prefix} the path from the data source to the target is either invalid or contains a null value.";
                case VisitReturnCode.Ok: // Can't extract an error message from a success.
                case VisitReturnCode.NullContainer: // Should be checked before trying to extract a value.
                case VisitReturnCode.InvalidCast: // We should only extract a value here, so we can't have an invalid cast.
                case VisitReturnCode.AccessViolation: // We should only extract a value here, so there shouldn't be an access violation.
                    throw new InvalidOperationException($"{prefix} internal data binding error. Please report this using the '<b>Help/Report a bug...</b>' menu item.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Internal for tests
        internal static string GetExtractValueErrorString(VisitReturnCode returnCode, object target, in PropertyPath path)
        {
            var prefix = $"[UI Toolkit] Could not retrieve the value at path '<b>{path}</b>' for source of type '<b>{target?.GetType().Name}</b>':";
            switch (returnCode)
            {
                case VisitReturnCode.InvalidContainerType:
                    return $"{prefix} the source cannot be a primitive, a string or an enum.";
                case VisitReturnCode.MissingPropertyBag:
                    return $"{prefix} the source is missing a property bag.";
                case VisitReturnCode.InvalidPath:
                    return $"{prefix} the path from the source to the target is either invalid or contains a null value.";
                case VisitReturnCode.Ok: // Can't extract an error message from a success.
                case VisitReturnCode.NullContainer: // Should be checked before trying to extract a value.
                case VisitReturnCode.InvalidCast: // We should only extract a value here, so we can't have an invalid cast.
                case VisitReturnCode.AccessViolation: // We should only extract a value here, so there shouldn't be an access violation.
                    throw new InvalidOperationException($"{prefix} internal data binding error. Please report this using the '<b>Help/Report a bug...</b>' menu item.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Internal for tests
        internal static string GetSetValueErrorString(VisitReturnCode returnCode, object source, in PropertyPath sourcePath, object target, in PropertyPath targetPath, object extractedValueFromSource)
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

        // Internal for tests
        internal static string GetRootDataSourceError(object target)
        {
            return $"[UI Toolkit] Could not set value for target of type '<b>{target.GetType().Name}</b>': no path was provided.";
        }
    }
}
