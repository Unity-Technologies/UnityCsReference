// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class HelpBox : BindableElement
    {
        [Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(text), "text")
                });
            }

            #pragma warning disable 649
            [SerializeField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new HelpBox();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                {
                    var e = (HelpBox)obj;
                    e.text = text;
                }
            }
        }

        public string text { get; set; }

        public HelpBox()
        {
            Add(new IMGUIContainer(() =>
            {
                EditorGUILayout.HelpBox(text, MessageType.Info, true);
            }));
        }
    }
}
