// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor.Metadata
{
    /// <summary>
    /// A container class that contains all provider setting objects used in build profile.
    /// This is to accommodate for addComponent API only takes a single object with a type,
    /// but we have multiple objects with the same type, and are thus aggregated under this container.
    /// </summary>
    public class BuildProfileProviderContainer : ScriptableObject
    {
        /// <summary>
        /// all provider setting objects used in build profile.
        /// </summary>
        public List<IAdaptivePerformanceSettings> adaptivePerformanceProviderSettings
        {
            get => m_AdaptivePerformanceProviderSettings;
            set => m_AdaptivePerformanceProviderSettings = value;
        }

        [SerializeReference] private List<IAdaptivePerformanceSettings> m_AdaptivePerformanceProviderSettings = new();
    }
}
