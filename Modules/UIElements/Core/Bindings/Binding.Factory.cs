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
        [ExcludeFromDocs, Serializable]
        public abstract class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField, HideInInspector] internal string property;
            [SerializeField, HideInInspector] private BindingUpdateTrigger updateTrigger;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                var binding = (Binding) obj;
                binding.property = property;
                binding.updateTrigger = updateTrigger;
            }
        }
    }
}
