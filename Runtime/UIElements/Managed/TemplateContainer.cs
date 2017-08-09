// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class TemplateContainer : VisualContainer
    {
        public readonly string templateId;

        public TemplateContainer(string templateId)
        {
            this.templateId = templateId;
        }
    }
}
