// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.Search
{
    readonly struct MethodSignature
    {
        public readonly Type returnType;
        public readonly Type[] parameterTypes;
        public readonly Type delegateType;

        public MethodSignature(Type[] parameterTypes, Type returnType, Type delegateType)
        {
            this.parameterTypes = parameterTypes;
            this.returnType = returnType;
            this.delegateType = delegateType;
        }

        public static MethodSignature FromDelegate<TDelegate>()
            where TDelegate : Delegate
        {
            var mi = typeof(TDelegate).GetMethod("Invoke");
            if (mi == null)
                throw new Exception($"MethodInfo from delegate {typeof(TDelegate)} is null.");
            return new MethodSignature(mi.GetParameters().Select(p => p.ParameterType).ToArray(), mi.ReturnType, typeof(TDelegate));
        }
    }

    static class ReflectionUtils
    {
        public enum AttributeLoaderBehavior
        {
            ThrowOnValidation,
            DoNotThrowOnValidation
        }

        public static IEnumerable<THandlerWrapper> LoadAllMethodsWithAttribute<TAttribute, THandlerWrapper>(Func<MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature[] supportedSignatures, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
        {
            return LoadAllMethodsWithAttribute<TAttribute, THandlerWrapper>((_, mi, att, d) => generator(mi, att, d), supportedSignatures, behavior);
        }

        public static IEnumerable<THandlerWrapper> LoadAllMethodsWithAttribute<TAttribute, THandlerWrapper>(Func<IReadOnlyCollection<THandlerWrapper>, MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature[] supportedSignatures, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
        {
            return TypeCache.GetMethodsWithAttribute<TAttribute>()
                .Aggregate(new List<THandlerWrapper>(), (accumulated, mi) =>
                {
                    try
                    {
                        LoadMethodWithAttribute(mi, generator, supportedSignatures, accumulated, behavior);
                    }
                    catch (Exception ex)
                    {
                        if (behavior == AttributeLoaderBehavior.ThrowOnValidation)
                            throw ex;
                        else
                        {
                            LogError<TAttribute>(ex.Message, mi, AttributeLoaderBehavior.DoNotThrowOnValidation);
                        }

                        return accumulated;
                    }

                    return accumulated;
                })
                .Where(g => g != null);
        }

        public static IEnumerable<THandlerWrapper> LoadAllMethodsWithAttribute<TAttribute, THandlerWrapper>(Func<MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature supportedSignature, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
        {
            return LoadAllMethodsWithAttribute(generator, new[] { supportedSignature }, behavior);
        }

        public static IEnumerable<THandlerWrapper> LoadAllMethodsWithAttribute<TAttribute, THandlerWrapper>(Func<IReadOnlyCollection<THandlerWrapper>, MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature supportedSignature, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
        {
            return LoadAllMethodsWithAttribute(generator, new[] { supportedSignature }, behavior);
        }

        public static IEnumerable<THandlerWrapper> LoadMethodWithAttribute<TAttribute, THandlerWrapper>(MethodInfo methodInfo, Func<MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature[] supportedSignatures, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
        {
            var loaded = new List<THandlerWrapper>();
            LoadMethodWithAttribute<TAttribute, THandlerWrapper>(methodInfo, (_, mi, att, handler) => generator(mi, att, handler), supportedSignatures, loaded, behavior);
            return loaded;
        }

        public static void LoadMethodWithAttribute<TAttribute, THandlerWrapper>(MethodInfo methodInfo, Func<IReadOnlyCollection<THandlerWrapper>, MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature[] supportedSignatures, List<THandlerWrapper> loaded, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
        {
            if (!methodInfo.IsStatic)
            {
                LogError<TAttribute>($"Method {methodInfo.Name} should be static.", methodInfo, behavior);
                return;
            }

            foreach (var supportedSignature in supportedSignatures)
            {
                if (!ValidateMethodSignature(methodInfo, supportedSignature))
                    continue;
                var handler = CreateDelegate(methodInfo, supportedSignature.delegateType);
                foreach (var attribute in methodInfo.GetCustomAttributes<TAttribute>())
                {
                    try
                    {
                        if (attribute == null)
                            LogGeneratorError($"Method {methodInfo.Name} should have an attribute of type {typeof(TAttribute)}.", attribute, methodInfo, behavior);
                        var handlerWrapper = generator(loaded, methodInfo, attribute, handler);
                        loaded.Add(handlerWrapper);
                    }
                    catch (Exception ex)
                    {
                        if (behavior == AttributeLoaderBehavior.ThrowOnValidation)
                            throw ex;
                        LogGeneratorError(ex.Message, attribute, methodInfo, AttributeLoaderBehavior.DoNotThrowOnValidation);
                    }
                }

                return;
            }
            LogError<TAttribute>($"Method {methodInfo.Name} doesn't have the correct signature.", methodInfo, behavior);
        }

        public static Delegate CreateDelegate(MethodInfo mi, Type delegateType)
        {
            var thisClassType = typeof(ReflectionUtils);
            var method = thisClassType.GetMethod("CreateDelegate", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(MethodInfo) }, null);
            var typedMethod = method.MakeGenericMethod(delegateType);
            return typedMethod.Invoke(null, new object[] { mi }) as Delegate;
        }

        public static TDelegate CreateDelegate<TDelegate>(MethodInfo mi)
        {
            if (!(Delegate.CreateDelegate(typeof(TDelegate), mi) is TDelegate handler))
                throw new Exception($"Could not convert method {mi.Name} to delegate {typeof(TDelegate)}.");
            return handler;
        }

        public static bool ValidateMethodSignature(MethodInfo methodInfo, MethodSignature supportedSignature)
        {
            if (methodInfo == null)
                return false;

            if (methodInfo.ReturnType != supportedSignature.returnType)
            {
                return false;
            }

            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != supportedSignature.parameterTypes.Length)
            {
                return false;
            }

            for (var i = 0; i < supportedSignature.parameterTypes.Length; ++i)
            {
                if (parameterInfos[i].ParameterType != supportedSignature.parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        static void LogError<TAttribute>(string message, MethodInfo mi, AttributeLoaderBehavior behavior)
        {
            if (behavior == AttributeLoaderBehavior.ThrowOnValidation)
                throw new Exception(message);
            UnityEngine.Debug.LogWarning($"Cannot load method \"{GetMethodFullName(mi)}\" with attribute \"{typeof(TAttribute).FullName}\": {message}");
        }

        static void LogGeneratorError<TAttribute>(string message, TAttribute attribute, MethodInfo mi, AttributeLoaderBehavior behavior)
        {
            if (behavior == AttributeLoaderBehavior.ThrowOnValidation)
                throw new Exception(message);
            UnityEngine.Debug.LogWarning($"Cannot load method \"{GetMethodFullName(mi)}\" with attribute \"{attribute}\": {message}");
        }

        public static string GetMethodFullName(MethodInfo mi)
        {
            if (mi.DeclaringType == null)
                return mi.Name;
            return $"{mi.DeclaringType.FullName}.{mi.Name}";
        }
    }
}
