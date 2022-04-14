// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal class UxmlAssetAttributeDescription<T> : TypedUxmlAttributeDescription<T> where T : Object
    {
        public UxmlAssetAttributeDescription()
        {
            type = "string"; // In uxml, this is referenced as a string.
            typeNamespace = xmlSchemaNamespace;
            defaultValue = default;
        }

        public override string defaultValueAsString => defaultValue?.ToString() ?? "null";

        public override T GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            string path = null;
            if (TryGetValueFromBag(bag, cc, (s, t) => s, null, ref path))
                return cc.visualTreeAsset?.GetAsset<T>(path);

            return null;
        }
    }
}
