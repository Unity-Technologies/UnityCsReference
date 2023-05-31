// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes an XML <c>Object</c> attribute referencing an asset of a chosen type in the project. In UXML, this is
    /// referenced as a string URI.
    /// </summary>
    internal class UxmlImageAttributeDescription : UxmlAttributeDescription, IUxmlAssetAttributeDescription
    {
        // Stores the value of the asset type being interacted with or saved. Defaults to an empty Texture if none is provided
        private Type m_AssetType;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UxmlImageAttributeDescription()
        {
            type = "string"; // In uxml, this is referenced as a string.
            typeNamespace = xmlSchemaNamespace;
            defaultValue = default;
        }

        /// <summary>
        /// The default value to be used for that specific attribute.
        /// </summary>
        public Background defaultValue { get; set; }

        /// <summary>
        /// The string representation of the default value of the UXML attribute.
        /// </summary>
        public override string defaultValueAsString => defaultValue.IsEmpty() ? "null" : defaultValue.ToString();

        /// <summary>
        /// Retrieves the value of this attribute from the attribute bag. Returns it if it is found, otherwise return null.
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>The value of the attribute.</returns>
        public Background GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            if (TryGetValueFromBagAsString(bag, cc, out var path, out var sourceAsset) && sourceAsset != null)
            {
                if (path == null)
                    return default;

                if (m_AssetType == null)
                    m_AssetType = sourceAsset.GetAssetType(path);

                return Background.FromObject(sourceAsset.GetAsset(path, m_AssetType));
            }

            return default;
        }

        // Override to keep a reference of the asset type being selected
        Type IUxmlAssetAttributeDescription.assetType
        {
            get => m_AssetType ?? typeof(Texture);
        }
    }
}
