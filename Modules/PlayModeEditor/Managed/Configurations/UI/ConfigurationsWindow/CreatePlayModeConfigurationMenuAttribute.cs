// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.PlayMode.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    class CreatePlayModeConfigurationMenuAttribute : Attribute
    {
        private string m_Label;
        private string m_NewItemName;

        public string Label => m_Label;
        public string NewItemName => m_NewItemName;

        public CreatePlayModeConfigurationMenuAttribute(string label, string newItemName = "NewPlayModeConfiguration")
        {
            m_Label = label;
            m_NewItemName = newItemName;
        }

        public static CreatePlayModeConfigurationMenuAttribute GetAttribute(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(CreatePlayModeConfigurationMenuAttribute), false);
            if (attributes.Length > 0)
            {
                return (CreatePlayModeConfigurationMenuAttribute)attributes[0];
            }
            return null;
        }
    }
}
