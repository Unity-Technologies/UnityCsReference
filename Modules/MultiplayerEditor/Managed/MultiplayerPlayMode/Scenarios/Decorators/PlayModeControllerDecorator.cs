// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class PlayModeControllerDecorator : PlayModeController
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
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(PlayModeControllerDecorator<>))
                {
                    return currentType;
                }
                currentType = currentType.BaseType;
            }

            return null;
        }
    }

    abstract class PlayModeControllerDecorator<TSettings> : PlayModeControllerDecorator
        where TSettings : struct
    {
        [SerializeReference] IDecoratorItem m_DecoratorItem;

        internal protected TSettings Settings => m_DecoratorItem.GetSettings<TSettings>();

        internal SerializedProperty GetSettingsSerializedProperty()
        {
            var itemProperty = GetControllerItemProperty();
            if (itemProperty == null || !GetInstanceItem().TryGetDecoratorItemIndex(GetType(), out var decoratorIndex))
                return null;

            return itemProperty
                .FindPropertyRelative(IPlayModeControllerItem.k_DecoratorsPropertyPath)
                .GetArrayElementAtIndex(decoratorIndex)
                .FindPropertyRelative(IDecoratorItem.k_SettingsPropertyPath);
        }

        internal static TSettings GetDefaultSettings() => new();

        internal static new T CreateInstance<T>(IPlayModeControllerItem instanceItem, OrchestratedScenario owner)
            where T : PlayModeControllerDecorator<TSettings>
        {
            var controller = PlayModeController.CreateInstance<T>(instanceItem, owner);
            controller.m_DecoratorItem = instanceItem.GetDecoratorItem<T>();
            return controller;
        }
    }
}
