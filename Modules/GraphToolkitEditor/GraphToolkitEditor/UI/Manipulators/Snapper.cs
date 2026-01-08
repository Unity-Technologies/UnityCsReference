// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    class Snapper
    {
        List<SnapStrategy> m_SnappingStrategies = new List<SnapStrategy>();
        public bool IsActive => m_SnappingStrategies.HasAny(s => s.Enabled);

        public Snapper()
        {
            InitSnappingStrategies();
        }

        void InitSnappingStrategies()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var snappingStrategy in GraphViewSettings.UserSettings.SnappingStrategiesStates.Keys.Where(snappingStrategy => typeof(SnapStrategy).IsAssignableFrom(snappingStrategy)))
#pragma warning restore RS0030
            {
                m_SnappingStrategies.Add((SnapStrategy)Activator.CreateInstance(snappingStrategy));
            }
        }

        public void AddSnapStrategy(SnapStrategy strategy)
        {
            m_SnappingStrategies.Add(strategy);
        }

        public void RemoveSnapStrategy(SnapStrategy strategy)
        {
            m_SnappingStrategies.Remove(strategy);
        }

        public void BeginSnap(GraphElement selectedElement)
        {
            UpdateSnappingStrategies();
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
#pragma warning restore RS0030
            {
                snapStrategy.BeginSnap(selectedElement);
            }
        }

        public Vector2 GetSnappedPosition(Rect sourceRect, GraphElement selectedElement)
        {
            Vector2 snappedPosition = sourceRect.position;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
#pragma warning restore RS0030
            {
                AdjustSnappedPosition(ref snappedPosition, sourceRect, selectedElement, snapStrategy);
            }

            return snappedPosition;
        }

        public void EndSnap()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
#pragma warning restore RS0030
            {
                snapStrategy.EndSnap();
            }
        }

        public void PauseSnap(bool isPaused)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
#pragma warning restore RS0030
            {
                snapStrategy.PauseSnap(isPaused);
            }
        }

        void UpdateSnappingStrategies()
        {
            foreach (var snappingStrategy in GraphViewSettings.UserSettings.SnappingStrategiesStates)
            {
                EnableStrategy(snappingStrategy.Key, snappingStrategy.Value);
            }
        }

        void EnableStrategy(Type strategyType, bool isEnabled)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SnappingStrategies.First(s => s.GetType() == strategyType).Enabled = isEnabled;
#pragma warning restore RS0030
        }

        static void AdjustSnappedPosition(ref Vector2 snappedPosition, Rect sourceRect, GraphElement selectedElement, SnapStrategy snapStrategy)
        {
            // Retrieve the snapping strategy's suggested snapped position and its snapping offset
            var suggestedSnappedPosition = snapStrategy.GetSnappedPosition(out var snapDirection, sourceRect, selectedElement);

            // Set snapped position using the suggested position relevant coordinates
            SetSnappedRect(ref snappedPosition, suggestedSnappedPosition, snapDirection);
        }

        static void SetSnappedRect(ref Vector2 snappedPosition, Vector2 suggestedSnappedPosition, SnapDirection snapDirection)
        {
            if ((snapDirection & SnapDirection.SnapY) == SnapDirection.SnapY)
            {
                snappedPosition.y = suggestedSnappedPosition.y;
            }
            if ((snapDirection & SnapDirection.SnapX) == SnapDirection.SnapX)
            {
                snappedPosition.x = suggestedSnappedPosition.x;
            }
        }
    }
}
