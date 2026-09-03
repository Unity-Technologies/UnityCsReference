// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace UnityEngine.UI
{
    // This is the required interface to UIElementsUtility for Runtime game components.
    [NativeHeader("Modules/UI/Insights/UIInsights.bindings.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class Insights
    {
        /// <summary>
        /// Method which validates if UI insights are enabled.
        /// Validate before triggering costly processing related to insights.
        /// </summary>
        /// <returns>True if UI insights are enabled, false otherwise.</returns>
        public static bool Enabled()
        {
            return Internal_Enabled();
        }

        /// <summary>
        /// Registers an event along with the name of the element which is the source of the event.
        /// </summary>
        /// <param name="id">The type of event.</param>
        /// <param name="name">The name which identifies the source element.</param>
        [FreeFunction("UIInsightsBindings::LogEvent")]
        public static extern void LogEvent(InsightID id, string name);

        /// <summary>
        /// Registers an event with payload data, along with the name of the element which is the source of the event.
        /// </summary>
        /// <param name="id">The type of event.</param>
        /// <param name="name">The name which identifies the source element.</param>
        /// <param name="data">Payload data.</param>
        [FreeFunction("UIInsightsBindings::LogEventWithData")]
        public static extern void LogEventWithData(InsightID id, string name, string data);

        /// <summary>
        /// Registers an event associated with a Component.
        /// Equivalent to <see cref="LogEvent(InsightID, string)"/>, but the path is calculated between the nearest Canvas and the provided component.
        /// </summary>
        /// <param name="id">The unique identifier of the event to be registered.</param>
        /// <param name="component"><see cref="Component"/> which triggered the event. Cannot be <see langword="null"/>.</param>
        [FreeFunction("UIInsightsBindings::LogEventWithComponent")]
        public static extern void LogEventWithComponent(InsightID id, Component component);

        /// <summary>
        /// Registers an event associated with a Component, along with payload data
        /// Equivalent to <see cref="LogEventWithData(InsightID, string, string)"/>, but the path is calculated between the nearest Canvas and the provided component.
        /// </summary>
        /// <param name="id">The unique identifier of the event to be registered.</param>
        /// <param name="component"><see cref="Component"/> which triggered the event. Cannot be <see langword="null"/>.</param>
        /// <param name="data">Payload data.</param>
        [FreeFunction("UIInsightsBindings::LogEventWithComponentAndData")]
        public static extern void LogEventWithComponentAndData(InsightID id, Component component, string data);

        [FreeFunction("UIInsightsBindings::Enabled")]
        private extern static bool Internal_Enabled();
    }

    /// <summary>
    /// Event identifiers for UI Insights. Must be synchronized with native enum "UI::Insights::InsightID".
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal enum InsightID
    {
        Unknown = 0, // Default value; events with this id were not fully defined and are not logged
        Button_Clicked,
        Dropdown_Selection
    }
}
