// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A RenderingLayerMaskField editor.
    /// </summary>
    public class RenderingLayerMaskField : BaseMaskField<uint>
    {
        [ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<uint>.UxmlSerializedData
        {
#pragma warning disable 649
            [UxmlAttribute("value")] [SerializeField]
            RenderingLayerMask layerMask;

            [SerializeField, UxmlIgnore, HideInInspector]
            UxmlAttributeFlags layerMask_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new RenderingLayerMaskField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(layerMask_UxmlAttributeFlags))
                {
                    var e = (RenderingLayerMaskField)obj;
                    e.layerMask = layerMask;
                }
            }
        }

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

        internal RenderingLayerMask layerMask
        {
            get => value;
            set => this.value = value;
        }

        readonly HelpBox m_HelpBox;

        readonly List<string> m_RenderingLayersChoices = new();
        readonly List<int> m_RenderingLayersChoicesMasks = new();

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
        public RenderingLayerMaskField(string label)
            : this(label, RenderingLayerMask.defaultRenderingLayerMask)
        {
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
        /// <param name="defaultMask">The default mask to use for the initial selection.</param>
        public RenderingLayerMaskField(string label, uint defaultMask)
            : base(label)
        {
            style.flexWrap = Wrap.Wrap;
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            m_HelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            Add(m_HelpBox);

            UpdateChoices(defaultMask);
            SetValueWithoutNotify(defaultMask);

            RegisterCallback<AttachToPanelEvent>(_ => TagManager.onRenderingLayersChanged += OnRenderingLayersChanged);
            RegisterCallback<DetachFromPanelEvent>(_ => TagManager.onRenderingLayersChanged -= OnRenderingLayersChanged);
            RegisterCallback<GeometryChangedEvent>(RecalculateHelpBoxSize);
        }

        internal override uint MaskToValue(int newMask) => unchecked((uint)newMask);

        internal override int ValueToMask(uint value) => unchecked((int)value);

        private protected override int UpdateMaskIfEverything(int currentMask)
        {
            var limit = RenderPipelineEditorUtility.GetActiveMaxRenderingLayers();
            return RenderPipelineEditorUtility.DoesMaskContainRenderingLayersOutsideOfMaxBitCount(unchecked((uint)currentMask), limit) ? currentMask : base.UpdateMaskIfEverything(currentMask);
        }

        public override uint value
        {
            get => rawValue;
            set
            {
                if (CheckIfOnlyOneLayerWasDeselectedFromEverything(value))
                {
                    var diffValue = ~value;
                    var counter = BitOperationUtils.CountBits(diffValue);
                    if (counter == 1)
                    {
                        var currentMax = RenderPipelineEditorUtility.GetActiveMaxRenderingLayers();
                        var definedRenderingLayerValues = RenderingLayerMask.GetDefinedRenderingLayerValues();
                        value = BitOperationUtils.ModifyMaskByValuesArrayAndBitCount(value, definedRenderingLayerValues, currentMax);
                    }
                }

                UpdateChoices(value);
                base.value = value;
            }
        }

        protected void UpdateLayersInfo()
        {
            UpdateChoices(value);
        }

        bool CheckIfOnlyOneLayerWasDeselectedFromEverything(uint nextValue)
        {
            return rawValue == uint.MaxValue && nextValue != uint.MaxValue;
        }

        void UpdateChoices(uint mask)
        {
            // Create the appropriate choices for the mask
            var (names, values) = RenderPipelineEditorUtility.GetRenderingLayerNamesAndValuesForMask(mask);

            m_RenderingLayersChoices.Clear();
            m_RenderingLayersChoices.AddRange(names);
            choices = m_RenderingLayersChoices;

            m_RenderingLayersChoicesMasks.Clear();
            m_RenderingLayersChoicesMasks.AddRange(values);
            choicesMasks = m_RenderingLayersChoicesMasks;

            UpdateHelpBoxVisibility(mask);
        }

        void OnRenderingLayersChanged()
        {
            value = rawValue;
        }

        //We manually recalculate the HelpBox width to imitate the behavior of flex column for our mask control and HelpBox
        void RecalculateHelpBoxSize(GeometryChangedEvent evt)
        {
            m_HelpBox.style.width = evt.newRect.width;
        }

        void UpdateHelpBoxVisibility(uint mask)
        {
            var maxBitCount = RenderPipelineEditorUtility.GetActiveMaxRenderingLayers();
            if (RenderPipelineEditorUtility.DoesMaskContainRenderingLayersOutsideOfMaxBitCount(mask, maxBitCount))
            {
                m_HelpBox.style.display = DisplayStyle.Flex;
                m_HelpBox.text = RenderPipelineEditorUtility.GetOutsideOfMaxBitCountWarningMessage(maxBitCount);
            }
            else
            {
                m_HelpBox.style.display = DisplayStyle.None;
            }
        }
    }
}
