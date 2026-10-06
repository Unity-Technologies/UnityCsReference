// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Common
{
    /// <summary>
    /// Used internally to find all implementations of <see cref="OnboardingGUIProvider{T}"/>.
    /// </summary>
    /// <remarks>
    /// Do not implement this interface directly, see <see cref="OnboardingGUIProvider{T}"/>.
    /// </remarks>
    interface IOnboardingGUIProvider
    {
        (OnboardingSectionCategory, int)[] Categories { get; }

        VisualElement CreateGUI();

        (OnboardingSectionCategory Category, int Priority) this[OnboardingSectionCategory a] { get; }
    }

    /// <summary>
    /// Implement this class to provide a visual element for a new section
    /// in the chosen <see cref="OnboardingSectionCategory"/> in the Multiplayer Center window.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the <see cref="OnboardingGUIProvider{T}"/> for the section.
    /// Must be a value of <see cref="OnboardingSectionCategory"/>.
    /// </typeparam>
    internal abstract class OnboardingGUIProvider<T> : IOnboardingGUIProvider where T : OnboardingGUIProvider<T>, new()
    {
        /// <summary>
        /// Returns the category the section will be displayed into.
        /// </summary>
        public abstract (OnboardingSectionCategory, int)[] Categories { get; }

        /// <summary>
        /// Creates the visual element for the section.
        /// </summary>
        /// <returns>The VisualElement root of the content to be displayed for this onboarding section.</returns>
        public abstract VisualElement CreateGUI();

        /// <summary>
        /// Gets the category-priority pair for the specified onboarding section.
        /// </summary>
        /// <param name="section">The onboarding section category to look up.</param>
        /// <value>
        /// A tuple containing the matching category and its priority;
        /// otherwise, the default tuple value if no match is found.
        /// </value>
        public virtual (OnboardingSectionCategory Category, int Priority) this[OnboardingSectionCategory section]
        {
            get
            {
                foreach (var category in Categories)
                {
                    if (category.Item1 == section)
                    {
                        return category;
                    }
                }
                return default;
            }
        }
    }
}
