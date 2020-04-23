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
                switch (c)
                {
                    case ' ':
                        EatSpace();
                        AddValuePart();
                        break;
                    case ',':
                        EatSpace();
                        AddValuePart();
                        // comma is considered a literal value
                        m_ValueList.Add(",");
                        break;
                    case '(':
                        AppendFunction();
                        break;
                    default:
                        m_StringBuilder.Append(c);
                        break;
                }
                ++m_ParseIndex;
            }

            var lastPart = m_StringBuilder.ToString();
            if (!string.IsNullOrEmpty(lastPart))
                m_ValueList.Add(lastPart);

            return m_ValueList.ToArray();
        }

        private void AddValuePart()
        {
            var part = m_StringBuilder.ToString();
            m_StringBuilder.Remove(0, m_StringBuilder.Length);
            m_ValueList.Add(part);
        }

        private void AppendFunction()
        {
            while (m_ParseIndex < m_PropertyValue.Length && m_PropertyValue[m_ParseIndex] != ')')
            {
                m_StringBuilder.Append(m_PropertyValue[m_ParseIndex]);
                ++m_ParseIndex;
            }

            m_StringBuilder.Append(m_PropertyValue[m_ParseIndex]);
        }

        private void EatSpace()
        {
            while (m_ParseIndex + 1 < m_PropertyValue.Length && m_PropertyValue[m_ParseIndex + 1] == ' ')
            {
                ++m_ParseIndex;
            }
        }
    }
}
