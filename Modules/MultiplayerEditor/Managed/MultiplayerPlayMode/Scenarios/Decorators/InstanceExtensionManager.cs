// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Multiplayer.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[InitializeOnLoad]
class InstanceExtensionManager
{
    static IExtensionsDatabase Service => ServiceSingleton<IExtensionsDatabase, ExtensionsDatabase>.Instance;

    internal static Hash128 Hash => Service.Hash;

    static InstanceExtensionManager()
    {
        RegisterDefaultExtension();
    }

    static void RegisterDefaultExtension()
    {
        RegisterInstanceType<MainEditorController>();
        RegisterInstanceType<CloneEditorController>();
        RegisterInstanceType<LocalPlayerController>();
    }

    internal static void RegisterInstanceType<TController>() where TController : InstanceController
        => Service.RegisterInstanceType<TController>();

    internal static void RegisterDecorator<TDecorated, TDecorator>()
        where TDecorator : InstanceControllerDecorator
        where TDecorated : InstanceController
        => Service.RegisterDecorator<TDecorated, TDecorator>();

    internal static IReadOnlyCollection<Type> GetInstanceTypes()
        => Service.GetInstanceTypes();

    internal static IEnumerable<Type> GetInstanceTypesDerivedFrom(Type baseType)
        => Service.GetInstanceTypesDerivedFrom(baseType);

    internal static IReadOnlyCollection<Type> GetDecoratorTypes(Type decoratedType)
        => Service.GetDecoratorTypes(decoratedType);
}
