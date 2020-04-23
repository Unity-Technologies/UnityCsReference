using System;

namespace UnityEngine.UIElements
{
    public class UxmlChildElementDescription
    {
        public UxmlChildElementDescription(Type t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            elementName = t.Name;
            elementNamespace = t.Namespace;
        }

        public string elementName { get; protected set; }
        public string elementNamespace { get; protected set; }
    }
}
