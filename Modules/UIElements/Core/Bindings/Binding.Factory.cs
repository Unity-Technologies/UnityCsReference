// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for defining a binding.
    /// </summary>
    public abstract partial class Binding
    {
        internal const string k_UpdateTriggerTooltip = "This informs the binding system of whether the binding object " +
            "should be updated on every frame, when a change occurs in the source or on every frame if change detection " +
            "is impossible, and when explicitly marked as dirty.";

        [ExcludeFromDocs, Serializable]
        public abstract class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField, HideInInspector] internal string property;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags property_UxmlAttributeFlags;
            [Tooltip(k_UpdateTriggerTooltip)]
            [SerializeField, HideInInspector] BindingUpdateTrigger updateTrigger;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags updateTrigger_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                var e = (Binding)obj;
                if (ShouldWriteAttributeValue(property_UxmlAttributeFlags))
                    e.property = property;
                if (ShouldWriteAttributeValue(updateTrigger_UxmlAttributeFlags))
                    e.updateTrigger = updateTrigger;
            }
        }
    }
}
