using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public class CustomStyleResolvedEvent : EventBase<CustomStyleResolvedEvent>
    {
        public ICustomStyle customStyle
        {
            get { return (target as VisualElement)?.customStyle; }
        }
    }
}
