// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// This Interface is a utility used by the ListViewElement.uxml
    /// to bind the ListView label to a DisplayName.
    /// </summary>
    interface IListViewDisplayName
    {
        string DisplayName { get; }
    }

    interface IEnumDescription<TEnum> where TEnum : Enum
    {
        TEnum Type { get; }
    }

    /// <summary>
    /// Base class for a description ScriptableObject asset that will enforce a fixed size List of
    /// <typeparamref name="TDescription" /> based on the enum values of <typeparamref name="TEnum"/>.
    /// </summary>
    /// <typeparam name="TEnum">Enum type enforcing the number of elements in the <see cref="Descriptions"/> List.</typeparam>
    /// <typeparam name="TDescription">Struct containing the <typeparamref name="TEnum"/> and other data related to it.</typeparam>
    abstract class EnumBasedDescription<TEnum, TDescription>
        : ScriptableObject
        where TEnum : Enum
        where TDescription : IEnumDescription<TEnum>
    {
        public List<TDescription> Descriptions = new();

        protected virtual void OnValidate()
        {
            var allValues = new HashSet<TEnum>();
            foreach (var enumValue in Enum.GetValues(typeof(TEnum)))
            {
                allValues.Add((TEnum)enumValue);
            }

            // clearing up the duplicates types
            for (var index = 0; index < Descriptions.Count; index++)
            {
                var description = Descriptions[index];
                if (!allValues.Remove(description.Type))
                {
                    Descriptions.RemoveAt(index);
                    index--;
                }
            }

            // adding any missing enum value
            foreach (var category in allValues)
            {
                Descriptions.Add(CreateNewDescription(category));
            }
        }

        protected abstract TDescription CreateNewDescription(TEnum type);
    }

    /// <summary>
    /// Custom Editor for the <see cref="EnumBasedDescription{TEnum,TDescription}"/>. <br />
    /// This is a default Inspector that will disable the UI unless the editor is in
    /// developer mode, so the shipped content assets stay read-only.
    /// </summary>
    [CustomEditor(typeof(EnumBasedDescription<,>), true)]
    [CanEditMultipleObjects]
    class EnumBasedEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var inspector = new VisualElement();
            var property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                var propertyField = new PropertyField(property);
                if (enterChildren)
                {
                    propertyField.SetEnabled(false);
                    enterChildren = false;
                }
                inspector.Add(propertyField);
            }
            inspector.SetEnabled(Unsupported.IsDeveloperMode());
            return inspector;
        }
    }
}
