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
            where THandlerWrapper : struct
        {
            return TypeCache.GetMethodsWithAttribute<TAttribute>()
                .Select(mi =>
                {
                    try
                    {
                        return LoadMethodWithAttribute(mi, generator, supportedSignatures).ToArray();
                    }
                    catch (Exception ex)
                    {
                        if (behavior == AttributeLoaderBehavior.ThrowOnValidation)
                            throw ex;
                        else
                        {
                            UnityEngine.Debug.LogWarning($"Cannot load method: \"{mi.Name}\" with attribute: \"{typeof(TAttribute).FullName}\" ({ex.Message})");
                        }
                        return null;
                    }
                })
                .Where(generator => generator != null)
                .SelectMany(generator => generator);
        }

        public static IEnumerable<THandlerWrapper> LoadAllMethodsWithAttribute<TAttribute, THandlerWrapper>(Func<MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature supportedSignature, AttributeLoaderBehavior behavior = AttributeLoaderBehavior.ThrowOnValidation)
            where TAttribute : Attribute
            where THandlerWrapper : struct
        {
            return LoadAllMethodsWithAttribute(generator, new[] { supportedSignature }, behavior);
        }

        public static IEnumerable<THandlerWrapper> LoadMethodWithAttribute<TAttribute, THandlerWrapper>(MethodInfo methodInfo, Func<MethodInfo, TAttribute, Delegate, THandlerWrapper> generator, MethodSignature[] supportedSignatures)
            where TAttribute : Attribute
            where THandlerWrapper : struct
        {
            foreach (var attribute in methodInfo.GetCustomAttributes<TAttribute>())
            {
                if (attribute == null)
                    throw new Exception($"Method {methodInfo.Name} should have an attribute of type {typeof(TAttribute)}.");
                int foundSignatures = 0;
                for (int i = 0; i < supportedSignatures.Length; i++)
                {
                    var supportedSignature = supportedSignatures[i];
                    if (!ValidateMethodSignature(methodInfo, supportedSignature))
                        continue;
                    foundSignatures++;
                    var handler = CreateDelegate(methodInfo, supportedSignature.delegateType);
                    yield return generator(methodInfo, attribute, handler);
                }
                if (foundSignatures == 0)
                    throw new ArgumentException($"Method {methodInfo.Name} doesn't have the correct signature.");
            }
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
    }
}
