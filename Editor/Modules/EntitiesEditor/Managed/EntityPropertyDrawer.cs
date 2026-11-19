// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    [CustomPropertyDrawer(typeof(Entity))]
    sealed class EntityPropertyDrawer : PropertyDrawer
    {
        // The entity can be renamed or destroyed without the property changing
        const long k_NameRefreshIntervalMs = 250;

        static readonly string k_NoneText = L10n.Tr("None (Entity)", null);
        static readonly string k_InvalidFormat = L10n.Tr("Invalid (Entity {{{0}:{1}}})", null);
        static readonly string k_UnnamedText = L10n.Tr("Entity", null);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new TextField(preferredLabel) { isReadOnly = true };

            var entity = (Entity)property.boxedValue;
            PropertyDrawerUtility.TrackProperty(field, property, changed =>
            {
                entity = (Entity)changed.boxedValue;
                field.SetValueWithoutNotify(GetDisplayName(entity));
            });
            field.schedule.Execute(() => field.SetValueWithoutNotify(GetDisplayName(entity))).Every(k_NameRefreshIntervalMs);
            field.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2 && TryGetName(entity, out _))
                    Selection.activeEntityId = entity;
            });

            return field;
        }

        static string GetDisplayName(Entity entity)
        {
            if (entity == Entity.Null)
                return k_NoneText;

            if (!TryGetName(entity, out var name))
                return string.Format(k_InvalidFormat, entity.Index, entity.Version);

            var displayName = name.IsEmpty ? k_UnnamedText : name.ToString();
            return $"{displayName} {{{entity.Index}:{entity.Version}}}";
        }

        // False if the entity doesn't exist. The name is empty if the entity's world can't be read right now.
        static bool TryGetName(Entity entity, out FixedString64Bytes name)
        {
            name = default;
            if (entity == Entity.Null)
                return false;

            try
            {
                if (!GlobalEntityManager.Exists(entity))
                    return false;
                GlobalEntityManager.GetName(entity, out name);
            }
            catch (InvalidOperationException)
            {
                // The owning world is in an exclusive entity transaction
            }

            return true;
        }
    }
}
