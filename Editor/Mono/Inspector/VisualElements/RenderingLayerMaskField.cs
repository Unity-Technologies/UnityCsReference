// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A RenderingLayerMaskField editor.
    /// </summary>
    public class RenderingLayerMaskField : BaseMaskField<uint>
    {
        internal override uint MaskToValue(int newMask) => unchecked( (uint) newMask);
        internal override int ValueToMask(uint value) => unchecked( (int) value);

        [ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseMaskField<uint>.UxmlSerializedData
        {
            #pragma warning disable 649
            [UxmlAttribute("value")] [SerializeField]
            RenderingLayerMask layerMask;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags layerMask_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new RenderingLayerMaskField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(layerMask_UxmlAttributeFlags))
                {
                    var e = (RenderingLayerMaskField)obj;
                    e.SetValueWithoutNotify(layerMask.value);
                }
            }
        }

        internal RenderingLayerMask layerMask
        {
            get => value;
            set => this.value = value.value;
        }

        SerializedObject m_TagManagerSerializedObject;

        HelpBox m_HelpBox;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rendering-layer-mask-field";

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Constructor of the field.
        /// </summary>
        /// <param name="defaultMask">The default mask to use for the initial selection.</param>
        public RenderingLayerMaskField(uint defaultMask)
            : this(null, defaultMask)
        {
        }

        /// <summary>
        /// Constructor of the field.
        /// </summary>
        /// <param name="label">The label to prefix the <see cref="RenderingLayerMaskField"/>.</param>
        /// <param name="defaultMask">The default mask to use for the initial selection.</param>
        public RenderingLayerMaskField(string label, uint defaultMask)
            : this(label)
        {
            SetValueWithoutNotify(defaultMask);
        }

        /// <summary>
        /// Constructor of the field.
        /// </summary>
        public RenderingLayerMaskField()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor of the field.
        /// </summary>
        /// <param name="label">The label to prefix the <see cref="RenderingLayerMaskField"/>.</param>
        public RenderingLayerMaskField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            m_HelpBox = new HelpBox("", HelpBoxMessageType.Warning)
            {
                style = { display = DisplayStyle.Flex }
            };

            UpdateLayersInfo();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.RegisterValueChangedCallback(evt => UpdateLayersInfo());
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RenderPipelineManager.activeRenderPipelineTypeChanged += OnActiveRenderPipelineTypeChanged;

            var tagManager = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
            m_TagManagerSerializedObject = new SerializedObject(tagManager);

            this.TrackSerializedObjectValue(m_TagManagerSerializedObject, so =>  UpdateLayersInfo());

            parent?.Add(m_HelpBox);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            RenderPipelineManager.activeRenderPipelineTypeChanged -= OnActiveRenderPipelineTypeChanged;

            m_TagManagerSerializedObject.Dispose();
            m_TagManagerSerializedObject = null;

            parent?.Remove(m_HelpBox);
        }

        void OnActiveRenderPipelineTypeChanged()
        {
            UpdateLayersInfo();
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            // We must update the choices and the values since we don't know if they changed...
            UpdateLayersInfo();

            // Create the menu the usual way...
            base.AddMenuItems(menu);
        }

        protected void UpdateLayersInfo()
        {
            // Create the appropriate lists...
            var (names, values) = RenderPipelineEditorUtility.GetRenderingLayerNamesAndValuesForMask(layerMask);

            choices = new List<string>(names);
            choicesMasks = new List<int>(values);

            var currentLimit = RenderPipelineEditorUtility.GetActiveMaxRenderingLayers();
            if (currentLimit != 32 && layerMask != uint.MaxValue && layerMask >= 1u << currentLimit)
            {
                m_HelpBox.style.display = DisplayStyle.Flex;
                m_HelpBox.text =
                    $"Current mask contains layers outside of a supported range by active Render Pipeline. The active Render Pipeline only supports up to {currentLimit} layers. Rendering Layers above {currentLimit} are ignored.";
            }
            else
            {
                m_HelpBox.style.display = DisplayStyle.None;
            }
        }
    }
}
