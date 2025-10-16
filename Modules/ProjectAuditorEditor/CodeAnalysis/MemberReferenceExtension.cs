// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    // This mirrors some of the functionality found in Mono.Cecil but gives us control over its implementation and optimization.
    // Prevents string builder re-allocation on each call to FullName
    static class MemberReferenceExtension
    {
        [ThreadStatic]
        static StringBuilder ts_FullNameBuilder;

        internal static string FastFullName(this MemberReference member, StringBuilder builder = null)
        {
            switch (member)
            {
                case MethodReference _:
                case GenericInstanceType _:
                {
                    builder ??= ts_FullNameBuilder;
                    if (builder == null)
                    {
                        builder = new StringBuilder();
                        ts_FullNameBuilder = builder;
                    }
                    builder.Clear();
                    member.AppendFullName(builder);
                    return builder.ToString();
                }
                default:
                    return member.FullName;
            }
        }

        static void AppendFullName(this MemberReference member, StringBuilder builder)
        {
            switch (member)
            {
                case MethodReference method:
                    method.AppendFullName(builder);
                    return;
                case GenericInstanceType genericType:
                    builder.Append(genericType.ElementType.FullName);
                    builder.Append("<");
                    var parameters = genericType.GenericArguments;
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var parameter = parameters[i];
                        if (i > 0)
                            builder.Append(",");
                        parameter.AppendFullName(builder);
                    }
                    builder.Append(">");
                    return;
                default:
                    builder.Append(member.FullName);
                    break;
            }

        }

        static void AppendFullName(this MethodReference method, StringBuilder builder)
        {
            method.ReturnType.AppendFullName(builder);
            builder.Append(" ");
            if (method.DeclaringType != null)
            {
                method.DeclaringType.AppendFullName(builder);
                builder.Append("::");
            }
            builder.Append(method.Name);
            if (method is GenericInstanceMethod { HasGenericArguments: true } genericMethod)
            {
                builder.Append("<");
                var parameters = genericMethod.GenericArguments;
                for (var i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];
                    if (i > 0)
                        builder.Append(",");
                    parameter.AppendFullName(builder);
                }
                builder.Append(">");
            }
            builder.Append("(");
            if (method.HasParameters)
            {
                var parameters = method.Parameters;
                for (var i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];
                    if (i > 0)
                        builder.Append(",");
                    if (parameter.ParameterType.IsSentinel)
                        builder.Append("...,");
                    parameter.ParameterType.AppendFullName(builder);
                }
            }
            builder.Append(")");
        }
    }
}
