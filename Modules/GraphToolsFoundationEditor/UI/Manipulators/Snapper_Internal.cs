// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class Snapper_Internal
    {
        List<SnapStrategy> m_SnappingStrategies = new List<SnapStrategy>();
        internal bool IsActive_Internal => m_SnappingStrategies.Any(s => s.Enabled);

        public Snapper_Internal()
        {
            InitSnappingStrategies();
        }

        void InitSnappingStrategies()
        {
            foreach (var snappingStrategy in GraphViewSettings.UserSettings_Internal.SnappingStrategiesStates.Keys.Where(snappingStrategy => typeof(SnapStrategy).IsAssignableFrom(snappingStrategy)))
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
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
            {
                snapStrategy.BeginSnap(selectedElement);
            }
        }

        public Vector2 GetSnappedPosition(Rect sourceRect, GraphElement selectedElement)
        {
            Vector2 snappedPosition = sourceRect.position;

            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
            {
                AdjustSnappedPosition(ref snappedPosition, sourceRect, selectedElement, snapStrategy);
            }

            return snappedPosition;
        }

        public void EndSnap()
        {
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
            {
                snapStrategy.EndSnap();
            }
        }

        public void PauseSnap(bool isPaused)
        {
            foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled))
            {
                snapStrategy.PauseSnap(isPaused);
            }
        }

        void UpdateSnappingStrategies()
        {
            foreach (var snappingStrategy in GraphViewSettings.UserSettings_Internal.SnappingStrategiesStates)
            {
                EnableStrategy(snappingStrategy.Key, snappingStrategy.Value);
            }
        }

        void EnableStrategy(Type strategyType, bool isEnabled)
        {
            m_SnappingStrategies.First(s => s.GetType() == strategyType).Enabled = isEnabled;
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
