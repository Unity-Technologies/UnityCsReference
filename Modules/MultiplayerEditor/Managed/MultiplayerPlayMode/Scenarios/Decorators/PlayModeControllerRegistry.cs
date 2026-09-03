// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
using System;
using System.Collections.Generic;
using Unity.Multiplayer.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[InitializeOnLoad]
class PlayModeControllerRegistry
{
    static IControllerDatabase Service => ServiceSingleton<IControllerDatabase, ControllerDatabase>.Instance;

    internal static Hash128 Hash => Service.Hash;

    static PlayModeControllerRegistry()
    {
        RegisterDefaultControllers();
    }

    static void RegisterDefaultControllers()
    {
        RegisterInstanceController<MainEditorController>();
        RegisterInstanceController<CloneEditorController>();
        RegisterInstanceController<LocalPlayerController>();

        RegisterDecorator<LocalPlayerController, RunModeDecorator>();
    }

    internal static void RegisterInstanceController<TController>() where TController : PlayModeController
        => Service.RegisterInstanceController<TController>();

    internal static void RegisterScenarioController<TController>() where TController : PlayModeController
        => Service.RegisterScenarioController<TController>();

    internal static void RegisterDecorator<TDecorated, TDecorator>()
        where TDecorator : PlayModeControllerDecorator
        where TDecorated : PlayModeController
        => Service.RegisterDecorator<TDecorated, TDecorator>();

    internal static IReadOnlyCollection<Type> GetInstanceTypes()
        => Service.GetInstanceTypes();

    internal static IEnumerable<Type> GetInstanceTypesDerivedFrom(Type baseType)
        => Service.GetInstanceTypesDerivedFrom(baseType);

    internal static IReadOnlyCollection<Type> GetScenarioTypes()
        => Service.GetScenarioTypes();

    internal static IEnumerable<Type> GetScenarioTypesDerivedFrom(Type baseType)
        => Service.GetScenarioTypesDerivedFrom(baseType);

    internal static IReadOnlyCollection<Type> GetDecoratorTypes(Type decoratedType)
        => Service.GetDecoratorTypes(decoratedType);
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
