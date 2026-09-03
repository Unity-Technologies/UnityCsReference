// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Discovers methods decorated with <see cref="GraphMenuAttribute"/>,
    /// <see cref="StateMachineMenuAttribute"/>, <see cref="BlackboardMenuAttribute"/> or
    /// <see cref="ConditionMenuAttribute"/> and invokes them at menu-build time with the
    /// appropriate context.
    /// </summary>
    static partial class MenuCommandRegistry
    {
        [AutoStaticsCleanupOnCodeReload] // lazily rebuilt cache; cleared by Invalidate(), repopulated by EnsureBuilt()
        static Action<MenuContext>[] s_GraphHandlers;
        [AutoStaticsCleanupOnCodeReload] // lazily rebuilt cache; cleared by Invalidate(), repopulated by EnsureBuilt()
        static Action<MenuContext>[] s_StateMachineHandlers;
        [AutoStaticsCleanupOnCodeReload] // lazily rebuilt cache; cleared by Invalidate(), repopulated by EnsureBuilt()
        static Action<MenuContext>[] s_BlackboardHandlers;
        [AutoStaticsCleanupOnCodeReload] // lazily rebuilt cache; cleared by Invalidate(), repopulated by EnsureBuilt()
        static Action<MenuContext>[] s_ConditionHandlers;

        internal static void InvokeGraphHandlers(MenuContext context)
        {
            EnsureBuilt();
            Invoke(s_GraphHandlers, context);
            Invoke(s_StateMachineHandlers, context);
        }

        internal static void InvokeBlackboardHandlers(MenuContext context)
        {
            EnsureBuilt();
            Invoke(s_BlackboardHandlers, context);
        }

        internal static void InvokeConditionHandlers(MenuContext context)
        {
            EnsureBuilt();
            Invoke(s_ConditionHandlers, context);
        }

        static void Invoke(Action<MenuContext>[] handlers, MenuContext context)
        {
            if (handlers == null)
                return;

            foreach (var action in handlers)
            {
                try
                {
                    action(context);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Discards the cached invoker tables. Exposed so tests that add new
        /// decorated methods at runtime can force a refresh.
        /// </summary>
        internal static void Invalidate()
        {
            s_GraphHandlers = null;
            s_StateMachineHandlers = null;
            s_BlackboardHandlers = null;
            s_ConditionHandlers = null;
        }

        static void EnsureBuilt()
        {
            if (s_GraphHandlers != null)
                return;

            // s_GraphHandlers is the sentinel for all four tables, so it has to be assigned last.
            s_BlackboardHandlers = BuildHandlers<BlackboardMenuAttribute>(static attr => attr.GraphType, null);
            s_ConditionHandlers = BuildHandlers<ConditionMenuAttribute>(static attr => attr.StateMachineType, typeof(ConditionMenuContext));
            s_StateMachineHandlers = BuildHandlers<StateMachineMenuAttribute>(static attr => attr.StateMachineType, typeof(StateMachineMenuContext));
            s_GraphHandlers = BuildHandlers<GraphMenuAttribute>(static attr => attr.GraphType, typeof(GraphMenuContext));
        }

        static Action<MenuContext>[] BuildHandlers<TAttribute>(Func<TAttribute, Type> getOwnerType, Type requiredContextType)
            where TAttribute : Attribute
        {
            var attributeName = typeof(TAttribute).Name.Replace("Attribute", string.Empty);

            // Sort discovered methods by full name so menu order is stable
            // across runs and across .NET runtime versions.
            var methods = new List<MethodInfo>();
            foreach (var method in TypeCache.GetMethodsWithAttribute<TAttribute>())
                methods.Add(method);

            methods.Sort(static (a, b) => string.CompareOrdinal(FullName(a), FullName(b)));

            var handlers = new List<Action<MenuContext>>(methods.Count);
            foreach (var method in methods)
            {
                if (TryBuildInvoker(method, attributeName, getOwnerType, requiredContextType, out var invoke))
                    handlers.Add(invoke);
            }
            return handlers.ToArray();
        }

        static bool TryBuildInvoker<TAttribute>(
            MethodInfo method,
            string attributeName,
            Func<TAttribute, Type> getOwnerType,
            Type requiredContextType,
            out Action<MenuContext> invoke)
            where TAttribute : Attribute
        {
            invoke = null;

            if (!method.IsStatic)
            {
                LogAttributeUsageWarning(attributeName, method, "the method must be static.");
                return false;
            }

            if (method.ReturnType != typeof(void))
            {
                LogAttributeUsageWarning(attributeName, method, "the method must return void.");
                return false;
            }

            if (method.IsGenericMethodDefinition || (method.DeclaringType?.ContainsGenericParameters ?? false))
            {
                LogAttributeUsageWarning(attributeName, method, "generic methods and methods declared on generic types are not supported.");
                return false;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 ||
                (parameters[0].ParameterType != typeof(GraphMenuContext) &&
                 parameters[0].ParameterType != typeof(StateMachineMenuContext) &&
                 parameters[0].ParameterType != typeof(ConditionMenuContext)))
            {
                LogAttributeUsageWarning(attributeName, method,
                    $"the method must take a single '{nameof(GraphMenuContext)}', '{nameof(StateMachineMenuContext)}' or '{nameof(ConditionMenuContext)}' parameter.");
                return false;
            }

            var contextType = parameters[0].ParameterType;
            var ownerTypes = new List<Type>();
            foreach (var attribute in method.GetCustomAttributes<TAttribute>())
            {
                var ownerType = getOwnerType(attribute);
                if (!TryValidateOwnerType(attributeName, method, ownerType, contextType, requiredContextType))
                    continue;
                ownerTypes.Add(ownerType);
            }

            if (ownerTypes.Count == 0)
                return false;

            var ownerTypesArray = ownerTypes.ToArray();
            if (contextType == typeof(GraphMenuContext))
                invoke = BindInvoker<GraphMenuContext>(method, ownerTypesArray);
            else if (contextType == typeof(StateMachineMenuContext))
                invoke = BindInvoker<StateMachineMenuContext>(method, ownerTypesArray);
            else
                invoke = BindInvoker<ConditionMenuContext>(method, ownerTypesArray);
            return true;
        }

        static Action<MenuContext> BindInvoker<TContext>(MethodInfo method, Type[] ownerTypes)
            where TContext : MenuContext
        {
            var action = (Action<TContext>)Delegate.CreateDelegate(typeof(Action<TContext>), method);
            return context =>
            {
                if (context is TContext typedContext && IsOwnerTypeSupported(typedContext, ownerTypes))
                    action(typedContext);
            };
        }

        static bool TryValidateOwnerType(string attributeName, MethodInfo method, Type ownerType, Type contextType, Type requiredContextType)
        {
            if (ownerType == null)
            {
                LogAttributeUsageWarning(attributeName, method, "the type must not be null.");
                return false;
            }

            var isGraph = typeof(Graph).IsAssignableFrom(ownerType);
            var isStateMachine = typeof(StateMachine).IsAssignableFrom(ownerType);

            if (!isGraph && !isStateMachine)
            {
                LogAttributeUsageWarning(attributeName, method,
                    $"'{ownerType.Name}' is neither a {nameof(Graph)} nor a {nameof(StateMachine)} subclass.");
                return false;
            }

            // A blackboard handler passes no required context type: the owner decides which one it takes.
            var expectedContextType = requiredContextType ??
                (isStateMachine ? typeof(StateMachineMenuContext) : typeof(GraphMenuContext));
            var requiredOwnerType = expectedContextType == typeof(GraphMenuContext) ? typeof(Graph) : typeof(StateMachine);

            if (!requiredOwnerType.IsAssignableFrom(ownerType))
            {
                LogAttributeUsageWarning(attributeName, method, $"'{ownerType.Name}' must be a {requiredOwnerType.Name} subclass.");
                return false;
            }

            if (contextType != expectedContextType)
            {
                LogAttributeUsageWarning(attributeName, method,
                    $"'{ownerType.Name}' requires a '{expectedContextType.Name}' parameter, not '{contextType.Name}'.");
                return false;
            }

            return true;
        }

        static bool IsOwnerTypeSupported(MenuContext context, Type[] ownerTypes)
        {
            var owner = context switch
            {
                GraphMenuContext graphContext => (object)graphContext.Graph,
                StateMachineMenuContext stateMachineContext => stateMachineContext.StateMachine,
                ConditionMenuContext conditionContext => conditionContext.StateMachine,
                _ => null
            };

            if (owner == null)
                return false;

            var ownerRuntimeType = owner.GetType();
            foreach (var type in ownerTypes)
            {
                if (type != null && type.IsAssignableFrom(ownerRuntimeType))
                    return true;
            }
            return false;
        }

        static void LogAttributeUsageWarning(string attributeName, MethodInfo method, string reason)
        {
            Debug.LogWarning($"[{attributeName}] '{FullName(method)}' is ignored: {reason}");
        }

        static string FullName(MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
        }
    }
}
