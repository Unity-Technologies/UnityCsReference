// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class ObjectFieldWithPrompt : BaseField<Object>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Object>.UxmlSerializedData
        {
#pragma warning disable 649
            [SerializeField] bool allowSceneObjects;
            [UxmlAttribute("type"), UxmlTypeReference(typeof(Object))]
            [SerializeField] string objectType;

            [SerializeField] string title;
            [SerializeField] string message;
#pragma warning restore 649
            public override object CreateInstance() => new ObjectFieldWithPrompt();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ObjectFieldWithPrompt)obj;
                e.allowSceneObjects = allowSceneObjects;
                e.objectType = UxmlUtility.ParseType(objectType, typeof(Object));

                e.title = title;
                e.message = message;
            }
        }

        public new class UxmlFactory : UxmlFactory<ObjectFieldWithPrompt, UxmlTraits>
        {
        }

        public new class UxmlTraits : BaseField<Object>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_AllowSceneObjects = new() { name = "allow-scene-objects", defaultValue = true };
            UxmlTypeAttributeDescription<Object> m_ObjectType = new() { name = "type", defaultValue = typeof(Object) };
            UxmlStringAttributeDescription m_Title = new() { name = "title" };
            UxmlStringAttributeDescription m_Message = new() { name = "message" };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ObjectFieldWithPrompt)ve).allowSceneObjects = m_AllowSceneObjects.GetValueFromBag(bag, cc);
                ((ObjectFieldWithPrompt)ve).objectType = m_ObjectType.GetValueFromBag(bag, cc);

                ((ObjectFieldWithPrompt)ve).title = m_Title.GetValueFromBag(bag, cc);
                ((ObjectFieldWithPrompt)ve).message = m_Message.GetValueFromBag(bag, cc);
            }
        }

        string m_Title;

        public string title
        {
            get => m_Title;
            set => m_Title = value;
        }

        string m_Message;

        public string message
        {
            get => m_Message;
            set => m_Message = value;
        }

        Type m_objectType;

        public Type objectType
        {
            get { return m_objectType; }
            set
            {
                if (m_objectType == value) return;
                m_objectType = value;
                m_ObjectField.objectType = value;
            }
        }

        bool m_AllowSceneObjects;

        public bool allowSceneObjects
        {
            get => m_AllowSceneObjects;
            set
            {
                if (m_AllowSceneObjects == value) return;
                m_AllowSceneObjects = value;
                m_ObjectField.allowSceneObjects = value;
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
            m_ObjectField.RegisterValueChangedCallback(evt =>
            {
                if (EditorUtility.DisplayDialog(title, message, "Confirm", "Cancel"))
                    value = evt.newValue;
            });
            Add(m_ObjectField);
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            base.SetValueWithoutNotify(newValue);

            m_ObjectField.SetValueWithoutNotify(newValue);
        }
    }
}
