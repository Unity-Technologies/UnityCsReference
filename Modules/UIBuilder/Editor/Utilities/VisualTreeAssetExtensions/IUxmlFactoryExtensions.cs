// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class IUXMLFactoryExtensions
    {
        internal static readonly string s_TraitsNotFoundMessage = "UI Builder: IUxmlFactory.m_Traits field has not been found! Update the reflection code!";
        internal static readonly string s_UxmlTypeNotFoundMessage = "UI Builder: IUxmlFactory.uxmlType property has not been found! Update the reflection code!";

        public static BaseUxmlTraits GetTraits(this IBaseUxmlFactory factory)
        {
            var traitsField = factory.GetType()
                .GetField("m_Traits", BindingFlags.Instance | BindingFlags.NonPublic);
            if (traitsField == null)
            {
                Debug.LogError(s_TraitsNotFoundMessage);
                return null;
            }

            return traitsField.GetValue(factory) as BaseUxmlTraits;
        }

        public static Type GetUxmlType(this IBaseUxmlFactory factory)
        {
            var uxmlTypeProperty = factory.GetType()
                .GetProperty("uxmlType");
            if (uxmlTypeProperty == null)
            {
                Debug.LogError(s_UxmlTypeNotFoundMessage);
                return null;
            }

            return uxmlTypeProperty.GetValue(factory) as Type;
        }
    }
}
