using System;

namespace UnityEngine.UIElements
{
    public interface IUxmlAttributes
    {
        bool TryGetAttributeValue(string attributeName, out string value);
    }
}
