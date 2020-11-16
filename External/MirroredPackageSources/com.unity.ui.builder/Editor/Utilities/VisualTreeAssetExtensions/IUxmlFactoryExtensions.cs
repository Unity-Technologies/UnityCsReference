using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class IUXMLFactoryExtensions
    {
        internal static readonly string s_TraitsNotFoundMessage = "UI Builder: IUxmlFactory.m_Traits field has not been found! Update the reflection code!";

        public static UxmlTraits GetTraits(this IUxmlFactory factory)
        {
            var traitsField = factory.GetType()
                .GetField("m_Traits", BindingFlags.Instance | BindingFlags.NonPublic);
            if (traitsField == null)
            {
                Debug.LogError(s_TraitsNotFoundMessage);
                return null;
            }

            return traitsField.GetValue(factory) as UxmlTraits;
        }
    }
}
