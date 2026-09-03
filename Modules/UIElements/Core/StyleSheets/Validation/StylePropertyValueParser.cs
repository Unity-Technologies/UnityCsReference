// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.UIElements.StyleSheets
{
    internal class StylePropertyValueParser
    {
        private string m_PropertyValue;
        private List<string> m_ValueList = new List<string>();
        private StringBuilder m_StringBuilder = new StringBuilder();
        private int m_ParseIndex = 0;

        public string[] Parse(string propertyValue)
        {
            m_PropertyValue = propertyValue;
            m_ValueList.Clear();

            m_StringBuilder.Remove(0, m_StringBuilder.Length);
            m_ParseIndex = 0;

            // Split the value into parts
            while (m_ParseIndex < m_PropertyValue.Length)
            {
                var c = m_PropertyValue[m_ParseIndex];
                if (char.IsWhiteSpace(c))
                {
                    EatSpace();
                    AddValuePart();
                }
                else
                {
                    switch (c)
                    {
                        case ',':
                            AddValuePart();
                            m_ValueList.Add(",");
                            break;
                        case '/':
                            AddValuePart();
                            m_ValueList.Add("/");
                            break;
                        case '(':
                            AppendFunction();
                            break;
                        default:
                            m_StringBuilder.Append(c);
                            break;
                    }
                }
                ++m_ParseIndex;
            }

            AddValuePart();

            return m_ValueList.ToArray();
        }

        private void AddValuePart()
        {
            var part = m_StringBuilder.ToString();
            m_StringBuilder.Remove(0, m_StringBuilder.Length);
            if (part.Length > 0)
                m_ValueList.Add(part);
        }

        private void AppendFunction()
        {
            // Depth-tracked so inner functions (e.g. rgb() inside linear-gradient()) don't close the outer one.
            int depth = 1;
            m_StringBuilder.Append(m_PropertyValue[m_ParseIndex]);
            ++m_ParseIndex;
            while (m_ParseIndex < m_PropertyValue.Length && depth > 0)
            {
                char c = m_PropertyValue[m_ParseIndex];
                if (c == '(') ++depth;
                else if (c == ')') --depth;
                m_StringBuilder.Append(c);
                if (depth > 0)
                    ++m_ParseIndex;
            }
        }

        private void EatSpace()
        {
            while (m_ParseIndex + 1 < m_PropertyValue.Length && char.IsWhiteSpace(m_PropertyValue[m_ParseIndex + 1]))
            {
                ++m_ParseIndex;
            }
        }
    }
}
