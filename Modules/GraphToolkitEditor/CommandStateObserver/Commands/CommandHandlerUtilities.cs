// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Unity.GraphToolkit.CSO
{
    static class CommandHandlerUtilities
    {
        [UnityRestricted]
        internal enum BindingState
        {
            Unbound,
            Bound,
        }

        public static void GetDefaultCommandHandlerCandidates<TCommand>(List<MethodInfo> outCandidates)
        {
            var candidateMethods = typeof(TCommand).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
            foreach (var methodInfo in candidateMethods)
            {
                if (methodInfo.Name == "DefaultCommandHandler")
                {
                    outCandidates.Add(methodInfo);
                }
            }
        }

        public static TCommandHandler GetDefaultCommandHandler<TCommandHandler, TCommand>()
            where TCommandHandler : Delegate
        {
            List<MethodInfo> candidateMethods = new();
            GetDefaultCommandHandlerCandidates<TCommand>(candidateMethods);

            TCommandHandler handler = null;
            foreach (var methodInfo in candidateMethods)
            {
                TCommandHandler h = null;
                try
                {
                    h = (TCommandHandler)Delegate.CreateDelegate(typeof(TCommandHandler), methodInfo);
                }
                catch
                {
                    // ignored: try next one.
                }

                if (h != null)
                {
                    if (handler != null)
                    {
                        throw new AmbiguousMatchException($"Can not choose default command handler: more than one method in {typeof(TCommand).FullName} is assignable to {typeof(TCommandHandler).FullName}");
                    }

                    handler = h;
                }
            }

            if (handler == null)
            {
                throw new InvalidOperationException($"No default command handler found for {typeof(TCommand).FullName}. Command handler should be assignable to {typeof(TCommandHandler).FullName}");
            }

            return handler;
        }
    }
}
