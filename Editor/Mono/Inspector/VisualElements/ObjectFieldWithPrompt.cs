// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class ObjectFieldWithPrompt : BaseField<Object>
    {
        static readonly BindingId titleProperty = nameof(title);
        static readonly BindingId messageProperty = nameof(message);
        static readonly BindingId objectTypeProperty = nameof(objectType);
        static readonly BindingId allowSceneObjectsProperty = nameof(allowSceneObjects);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Object>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool allowSceneObjects;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags allowSceneObjects_UxmlAttributeFlags;
            [UxmlAttribute("type"), UxmlTypeReference(typeof(Object))]
            [SerializeField] string objectType;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags objectType_UxmlAttributeFlags;
            [SerializeField] string title;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags title_UxmlAttributeFlags;
            [SerializeField] string message;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags message_UxmlAttributeFlags;
            #pragma warning restore 649
            public override object CreateInstance() => new ObjectFieldWithPrompt();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ObjectFieldWithPrompt)obj;
                if (ShouldWriteAttributeValue(allowSceneObjects_UxmlAttributeFlags))
                    e.allowSceneObjects = allowSceneObjects;
                if (ShouldWriteAttributeValue(objectType_UxmlAttributeFlags))
                    e.objectType = UxmlUtility.ParseType(objectType, typeof(Object));
                if (ShouldWriteAttributeValue(title_UxmlAttributeFlags))
                    e.title = title;
                if (ShouldWriteAttributeValue(message_UxmlAttributeFlags))
                    e.message = message;
            }
        }

        string m_Title;

        [CreateProperty]
        public string title
        {
            get => m_Title;
            set
            {
                if (string.Equals(m_Title, value, StringComparison.Ordinal))
                    return;
                m_Title = value;
                NotifyPropertyChanged(titleProperty);
            }
        }

        string m_Message;

        [CreateProperty]
        public string message
        {
            get => m_Message;
            set
            {
                if (m_Message == value) return;
                m_Message = value;
                NotifyPropertyChanged(messageProperty);
            }
        }

        Type m_objectType;

        [CreateProperty]
        public Type objectType
        {
            get { return m_objectType; }
            set
            {
                if (m_objectType == value) return;
                m_objectType = value;
                m_ObjectField.objectType = value;
                NotifyPropertyChanged(objectTypeProperty);
            }
        }

        bool m_AllowSceneObjects;

        [CreateProperty]
        public bool allowSceneObjects
        {
            get => m_AllowSceneObjects;
            set
            {
                if (m_AllowSceneObjects == value) return;
                m_AllowSceneObjects = value;
                m_ObjectField.allowSceneObjects = value;
                NotifyPropertyChanged(allowSceneObjectsProperty);
            }
        }

        readonly ObjectField m_ObjectField;

        public ObjectFieldWithPrompt() : this(null)
        {
        }

        public ObjectFieldWithPrompt(string label) : base(label, null)
        {
            visualInput.style.display = DisplayStyle.None;

            m_ObjectField = new ObjectField
            {
                allowSceneObjects = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginLeft = 0,
                    marginRight = 0
                }
            };
            m_ObjectField.SelectorClosed += (obj, canceled) =>
            {
                if (canceled)
                    return; // User pressed the Escape Key

                // Delay the change call to avoid calling DestroyImmediate from the ObjectSelector.
                EditorApplication.delayCall += () => ChangeObject(obj);
            };
            m_ObjectField.RegisterValueChangedCallback(evt =>
            {
                if (!ObjectSelector.isVisible)
                {
                   // If the Object Selector is NOT visible that means that the user performed a Drag&Drop into the field with a valid asset.
                    if (ChangeObject(evt.newValue))
                        return;
                }

                // The UI with the actual backend value might be out of sync as the ObjectSelector or the Drag&Drop might
                // have changed the display value. Keep them in sync.
                m_ObjectField.SetValueWithoutNotify(value);
            });
            Add(m_ObjectField);
        }

        private static StringBuilder s_MessageBuilder = new StringBuilder();

        private bool ChangeObject(Object newValue)
        {
            if (value != newValue)
            {
                s_MessageBuilder.Clear();
                s_MessageBuilder.AppendLine(message);
                s_MessageBuilder.AppendLine();
                s_MessageBuilder.AppendLine($"Current Value: {(value ? value.name : "None")}.");
                s_MessageBuilder.AppendLine($"New Value: {(newValue ? newValue.name : "None")}.");
                if (EditorUtility.DisplayDialog(title, s_MessageBuilder.ToString(), "Confirm", "Cancel"))
                {
                    value = newValue;
                    return true;
                }
            }

            return false;
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_ObjectField.SetValueWithoutNotify(newValue);
        }
    }
}
