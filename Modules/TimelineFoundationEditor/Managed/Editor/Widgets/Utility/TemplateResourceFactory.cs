// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    readonly struct TemplateResource
    {
        readonly string m_TemplatePath;

        public TemplateResource(string templatePath)
        {
            m_TemplatePath = templatePath;
        }

        public void CloneInto(VisualElement visualElement)
        {
            visualElement.CloneTemplateInto(m_TemplatePath);
        }
    }

    class TemplateResourceFactory
    {
        const string k_TemplateExtension = ".uxml";
        readonly string m_TemplateDirectory;

        public TemplateResourceFactory(string templateDirectory)
        {
            m_TemplateDirectory = templateDirectory;
        }

        public TemplateResource Get(string templateName)
        {
            var templateFileName = $"{templateName}{k_TemplateExtension}";
            string templatePath = Path.Join(m_TemplateDirectory, templateFileName);
            return new TemplateResource(templatePath);
        }

        public TemplateResource Get<T>()
        {
            return Get(typeof(T).Name);
        }
    }
}
