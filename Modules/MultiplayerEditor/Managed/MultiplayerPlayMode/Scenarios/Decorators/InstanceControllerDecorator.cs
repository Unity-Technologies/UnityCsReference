// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class InstanceControllerDecorator : InstanceController
    {
        internal static bool IsDecoratorWithSettings(Type decoratorType)
        {
            return GetBaseDecoratorWithSettingsType(decoratorType) != null;
        }

        internal static Type GetSettingsType(Type decoratorType)
        {
            var baseType = GetBaseDecoratorWithSettingsType(decoratorType)
                ?? throw new ArgumentException($"Decorator type {decoratorType.Name} is not a valid decorator type with settings.");
            return baseType.GetGenericArguments()[0];
        }

        static Type GetBaseDecoratorWithSettingsType(Type decoratorType)
        {
            var currentType = decoratorType;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(InstanceControllerDecorator<>))
                {
                    return currentType;
                }
                currentType = currentType.BaseType;
            }

            return null;
        }
    }

    abstract class InstanceControllerDecorator<TSettings> : InstanceControllerDecorator
        where TSettings : struct
    {
        [SerializeReference] IDecoratorItem m_DecoratorItem;

        internal protected TSettings Settings => m_DecoratorItem.GetSettings<TSettings>();

        internal static TSettings GetDefaultSettings() => new();

        internal static new T CreateInstance<T>(IInstanceItem instanceItem, OrchestratedScenario owner)
            where T : InstanceControllerDecorator<TSettings>
        {
            var controller = InstanceController.CreateInstance<T>(instanceItem, owner);
            controller.m_DecoratorItem = instanceItem.GetDecoratorItem<T>();
            return controller;
        }
    }
}
