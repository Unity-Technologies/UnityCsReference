// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The view to show the available <see cref="OnboardingProviders"/>. Displayed in a new window, when no asset is selected.
    /// </summary>
    class BlankPage : VisualElement
    {
        public static readonly string ussClassName = "ge-blank-page";

        readonly ICommandTarget m_CommandTarget;

        public IEnumerable<OnboardingProvider> OnboardingProviders { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlankPage"/> class.
        /// </summary>
        /// <param name="commandTarget">The command dispatcher.</param>
        /// <param name="onboardingProviders">The list of <see cref="OnboardingProviders"/> to display.</param>
        public BlankPage(ICommandTarget commandTarget, IEnumerable<OnboardingProvider> onboardingProviders)
        {
            m_CommandTarget = commandTarget;
            OnboardingProviders = onboardingProviders;
        }

        public virtual void CreateUI()
        {
            Clear();

            AddToClassList(ussClassName);

            if (m_CommandTarget != null)
            {
                foreach (var provider in OnboardingProviders)
                {
                    Add(provider.CreateOnboardingElements(m_CommandTarget));
                }
            }
        }

        public virtual void UpdateUI()
        {
        }
    }
}
