// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
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

        void UpdateLayersInfo()
        {
            // Get the layers : names and values
            TagManager.GetDefinedRenderingLayers(out var layerNames, out var layerValues);

            // Create the appropriate lists...
            choices = new List<string>(layerNames);
            choicesMasks = new List<int>(layerValues);
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

            UpdateLayersInfo();
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            // We must update the choices and the values since we don't know if they changed...
            UpdateLayersInfo();

            // Create the menu the usual way...
            base.AddMenuItems(menu);
        }
    }
}
