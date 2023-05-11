// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a UXML <c>Object</c> attribute referencing an asset in the project. In UXML, this is referenced as a string URI.
    /// </summary>
    public class UxmlAssetAttributeDescription<T> : TypedUxmlAttributeDescription<T>, IUxmlAssetAttributeDescription where T : Object
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlAssetAttributeDescription()
        {
            type = "string"; // In uxml, this is referenced as a string.
            typeNamespace = xmlSchemaNamespace;
            defaultValue = default;
        }

        /// <summary>
        /// The default value for the attribute, as a string.
        /// </summary>
        public override string defaultValueAsString => defaultValue?.ToString() ?? "null";

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns it if it is found, otherwise return null.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public override T GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            if (TryGetValueFromBagAsString(bag, cc, out var path, out var sourceAsset) && sourceAsset != null)
                return sourceAsset.GetAsset<T>(path);

            return null;
        }

        /// <summary>
        /// Attempts to retrieve the value of this attribute from the attribute bag and returns true if found, otherwise false.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>True if the value is found, false otherwise.</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, out T value)
        {
            if (TryGetValueFromBagAsString(bag, cc, out var path, out var sourceAsset) && sourceAsset != null)
            {
                value = sourceAsset.GetAsset<T>(path);
                return true;
            }

            value = default;
            return false;
        }

        Type IUxmlAssetAttributeDescription.assetType => typeof(T);
    }

    // The sole purpose of this interface is to easily access the generic type without using reflection
    interface IUxmlAssetAttributeDescription
    {
        Type assetType { get; }
    }
}
