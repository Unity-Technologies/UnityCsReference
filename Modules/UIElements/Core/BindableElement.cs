// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Element that can be bound to a property. For more information, refer to [[wiki:UIE-uxml-element-BindableElement|UXML element BindableElement]].
    /// </summary>
    public class BindableElement : VisualElement, IBindable
    {
        internal const string k_BindingPathTooltip = "Default method to define a path to a serialized property. Most often used for Editor extensions and inspectors.";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(bindingPath), "binding-path")
                });
            }

            #pragma warning disable 649
            [Tooltip(k_BindingPathTooltip)]
            [SerializeField, BindingPathDrawer] string bindingPath;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindingPath_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new BindableElement();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(bindingPath_UxmlAttributeFlags))
                {
                    var e = (BindableElement)obj;
                    e.bindingPath = bindingPath;
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="BindableElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<BindableElement, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BindableElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath;

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };
            }

            /// <summary>
            /// Initialize <see cref="BindableElement"/> properties using values from the attribute bag.
            /// </summary>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                string propPath = m_PropertyPath.GetValueFromBag(bag, cc);

                if (!string.IsNullOrEmpty(propPath))
                {
                    var field = ve as IBindable;
                    if (field != null)
                    {
                        field.bindingPath = propPath;
                    }
                }
            }
        }

        /// <summary>
        /// Binding object that will be updated.
        /// </summary>
        public IBinding binding { get; set; }
        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        public string bindingPath { get; set; }
    }
}
