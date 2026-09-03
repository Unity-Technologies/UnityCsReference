// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    /// <summary>
    /// Renders the <c>[UxmlAttribute]</c> fields of a live object — a <see cref="VisualElement"/> or a
    /// boxed component — by mirroring it into a <see cref="UxmlSerializedData"/> wrapped in a
    /// <see cref="SerializedObject"/>, the same mechanism the authoring inspector and UI Builder use, so
    /// every serializable attribute type gets its regular drawer. Edits are read back from the mirror and
    /// committed through a supplied callback; <see cref="Refresh"/> re-syncs live values into the mirror.
    /// </summary>
    internal class UxmlAttributesDebugView : VisualElement
    {
        // Transient host so the serialized-data mirror can be wrapped in a SerializedObject.
        // The base-class qualifier matters: unqualified, the name binds to the nested VisualElement.UxmlSerializedData.
        class SerializedDataHost : ScriptableObject
        {
            [SerializeReference] public UnityEngine.UIElements.UxmlSerializedData data;
        }

        readonly UxmlSerializedDataDescription m_Description;
        readonly Func<object> m_ReadTarget;
        readonly Action<UxmlSerializedAttributeDescription, object> m_Commit;
        readonly bool m_ReadOnly;

        SerializedDataHost m_Host;
        SerializedObject m_SerializedObject;

        public UxmlAttributesDebugView(UxmlSerializedDataDescription description, Func<object> readTarget,
            Action<UxmlSerializedAttributeDescription, object> commit, bool readOnly,
            HashSet<string> excludedAttributeNames = null)
        {
            m_Description = description;
            m_ReadTarget = readTarget;
            m_Commit = commit;
            m_ReadOnly = readOnly || commit == null;

            if (description == null)
                return;

            foreach (var attribute in description.serializedAttributes)
            {
                // Skip attributes the host renders specially (e.g. class / style / data-source on an element).
                if (excludedAttributeNames != null && attribute.name != null && excludedAttributeNames.Contains(attribute.name))
                    continue;

                // UxmlObject children are out of scope for the debugger's flat view.
                if (attribute is { isUxmlObject: true } || attribute.type == null || attribute.serializedField == null)
                    continue;

                var field = new PropertyField { label = attribute.name, bindingPath = $"data.{attribute.serializedField.Name}" };

                // Align labels into a shared column (like the inspector), when an ancestor is marked as an
                // inspector element. Inert where there is no such ancestor.
                field.AddToClassList(BaseField<int>.alignedFieldUssClassName);

                if (m_ReadOnly)
                    field.SetEnabled(false);
                else
                    field.RegisterValueChangeCallback(_ => OnMirrorPropertyChanged(attribute));

                Add(field);
            }

            // The mirror lives only while the view is on a panel, so the host object never outlives its
            // window and survives a re-attach (e.g. docking) by being rebuilt.
            RegisterCallback<AttachToPanelEvent>(_ => CreateMirrorAndBind());
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseMirror());
        }

        /// <summary>Re-syncs the mirror from the current live object; the bound fields pick the values up.</summary>
        public void Refresh()
        {
            if (m_SerializedObject == null)
                return;

            // Don't overwrite a field mid-edit; the next periodic refresh catches up.
            if (focusController?.focusedElement is VisualElement focused && Contains(focused))
                return;

            SyncFromTarget();
            m_SerializedObject.Update();
        }

        void CreateMirrorAndBind()
        {
            if (m_Host != null)
                return;

            m_Host = ScriptableObject.CreateInstance<SerializedDataHost>();
            m_Host.hideFlags = HideFlags.HideAndDontSave;
            m_Host.data = m_Description.CreateDefaultSerializedData();
            SyncFromTarget();
            m_SerializedObject = new SerializedObject(m_Host);
            this.Bind(m_SerializedObject);
        }

        void ReleaseMirror()
        {
            this.Unbind();
            m_SerializedObject?.Dispose();
            m_SerializedObject = null;
            if (m_Host != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Host);
                m_Host = null;
            }
        }

        void SyncFromTarget()
        {
            var target = m_ReadTarget?.Invoke();
            if (target != null)
                m_Description.SyncSerializedData(target, m_Host.data);
        }

        // Fires for user edits but also for values echoed back by Refresh's sync (and once when the binding
        // activates), so only genuine edits — where the mirror differs from the live object — are committed.
        void OnMirrorPropertyChanged(UxmlSerializedAttributeDescription attribute)
        {
            if (m_Host == null)
                return;

            var target = m_ReadTarget?.Invoke();
            if (target == null)
                return;

            var value = attribute.GetSerializedValue(m_Host.data);
            if (attribute.TryGetValueFromObject(target, out var liveValue) && UxmlAttributeComparison.ObjectEquals(value, liveValue))
                return;

            m_Commit?.Invoke(attribute, value);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
