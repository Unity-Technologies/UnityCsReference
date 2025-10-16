// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An <see cref="OnboardingProvider"/> for the <see cref="SimpleGraphTool"/>.
    /// </summary>
    [UnityRestricted]
    internal class SimpleOnboardingProvider : OnboardingProvider
    {
        SimpleGraphTool m_GraphTool;

        /// <summary>
        /// Create a new instance of the <see cref="SimpleOnboardingProvider"/> class.
        /// </summary>
        /// <param name="tool">The <see cref="SimpleGraphTool"/>.</param>
        public SimpleOnboardingProvider(SimpleGraphTool tool)
        {
            m_GraphTool = tool;
        }

        /// <inheritdoc />
        public override IReadOnlyList<Type> GetAcceptedGraphObjectTypes()
        {
            return new[] { m_GraphTool.DefaultGraphObjectType };
        }

        /// <inheritdoc />
        protected override VisualElement CreateOnboardingElements(ICommandTarget commandTarget)
        {
            var defaultTemplate = m_GraphTool.DefaultGraphTemplate;
            if (defaultTemplate != null)
                ButtonContainer.Add(AddNewGraphButton(m_GraphTool.DefaultGraphObjectType, commandTarget, defaultTemplate));

            return ButtonContainer;
        }

        /// <inheritdoc />
        protected override string GetGraphName()
        {
            return m_GraphTool.Name;
        }
    }
}
