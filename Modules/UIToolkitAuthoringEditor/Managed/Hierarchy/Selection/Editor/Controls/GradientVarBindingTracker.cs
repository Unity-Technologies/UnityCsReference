// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Tracks the var() names bound to each slot of a gradient being edited (angle, radial
    // extent/position, per-stop color/position) so each edit can clear exactly the slot it
    // overrides. Decoded from the style property on read (SetVarBindings) and re-encoded on
    // write (BuildVarBindings). Shared between the UI Builder gradient field and the
    // VisualElement inspector background field.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class GradientVarBindingTracker
    {
        readonly List<(string colorVarName, string positionVarName)> m_StopVarNames = new();

        // Bindings as read from the style property, kept so raw var() handles (with
        // fallback arguments) can be re-attached by name on the next write.
        StyleProperty.GradientVarBindings m_SourceBindings;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string angleVarName { get; set; }
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string extentVarName { get; set; }
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string positionXVarName { get; set; }
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string positionYVarName { get; set; }

        // Decodes the slot-indexed bindings read from the style property into named vars,
        // anchored on the gradient currently shown by the field.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetVarBindings(in StyleProperty.GradientVarBindings bindings, in BackgroundGradient effective)
        {
            m_SourceBindings = bindings;
            bool isLinear = effective.type == GradientType.Linear;

            angleVarName = isLinear ? bindings.GetName(StyleProperty.GradientVarBindings.AngleSlot()) : null;
            extentVarName = isLinear ? null : bindings.GetName(StyleProperty.GradientVarBindings.RadialExtentSlot());
            positionXVarName = isLinear ? null : bindings.GetName(StyleProperty.GradientVarBindings.RadialPositionXSlot());
            positionYVarName = isLinear ? null : bindings.GetName(StyleProperty.GradientVarBindings.RadialPositionYSlot());

            m_StopVarNames.Clear();
            var stopCount = effective.stops?.Length ?? 0;
            for (int i = 0; i < stopCount; ++i)
            {
                m_StopVarNames.Add((
                    bindings.GetName(StyleProperty.GradientVarBindings.StopColorSlot(i, isLinear)),
                    bindings.GetName(StyleProperty.GradientVarBindings.StopPositionSlot(i, isLinear))));
            }
        }

        // Re-encodes the named vars into the slot scheme for the write path.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal StyleProperty.GradientVarBindings BuildVarBindings(bool isLinear)
        {
            int stopSlotBase = isLinear ? 1 : 4;
            var slotNames = new string[stopSlotBase + 2 * m_StopVarNames.Count];

            if (isLinear)
            {
                slotNames[StyleProperty.GradientVarBindings.AngleSlot()] = angleVarName;
            }
            else
            {
                slotNames[StyleProperty.GradientVarBindings.RadialExtentSlot()] = extentVarName;
                slotNames[StyleProperty.GradientVarBindings.RadialPositionXSlot()] = positionXVarName;
                slotNames[StyleProperty.GradientVarBindings.RadialPositionYSlot()] = positionYVarName;
            }

            for (int i = 0; i < m_StopVarNames.Count; ++i)
            {
                slotNames[StyleProperty.GradientVarBindings.StopColorSlot(i, isLinear)] = m_StopVarNames[i].colorVarName;
                slotNames[StyleProperty.GradientVarBindings.StopPositionSlot(i, isLinear)] = m_StopVarNames[i].positionVarName;
            }

            return new StyleProperty.GradientVarBindings(slotNames).WithHandlesFrom(m_SourceBindings);
        }

        // Clears the names of exactly the slots whose resolved value differs between the two
        // gradients; a type change invalidates every slot mapping, so all vars go.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void DropOverridden(in BackgroundGradient previous, in BackgroundGradient current)
        {
            if (previous.type != current.type)
            {
                ClearAll();
                SyncStopCount(current.stops?.Length ?? 0);
                return;
            }

            if (!Mathf.Approximately(previous.angle, current.angle))
                angleVarName = null;
            if (previous.size != current.size)
                extentVarName = null;
            if (!Mathf.Approximately(previous.position.x, current.position.x))
                positionXVarName = null;
            if (!Mathf.Approximately(previous.position.y, current.position.y))
                positionYVarName = null;

            SyncStopCount(current.stops?.Length ?? 0);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void ClearAll()
        {
            angleVarName = null;
            extentVarName = null;
            positionXVarName = null;
            positionYVarName = null;
            ClearStops();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void ClearStops()
        {
            for (int i = 0; i < m_StopVarNames.Count; ++i)
                m_StopVarNames[i] = (null, null);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SyncStopCount(int count)
        {
            while (m_StopVarNames.Count < count)
                m_StopVarNames.Add((null, null));
            if (m_StopVarNames.Count > count)
                m_StopVarNames.RemoveRange(count, m_StopVarNames.Count - count);
        }

        // Pairs each resolved stop with its tracked var names for the stops popup.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal BackgroundGradientStopsEditor.StopData[] CreateStopData(BackgroundGradientStop[] stops)
        {
            var stopDatas = new BackgroundGradientStopsEditor.StopData[stops.Length];
            for (int i = 0; i < stops.Length; ++i)
            {
                stopDatas[i] = new BackgroundGradientStopsEditor.StopData
                {
                    stop = stops[i],
                    colorVarName = i < m_StopVarNames.Count ? m_StopVarNames[i].colorVarName : null,
                    positionVarName = i < m_StopVarNames.Count ? m_StopVarNames[i].positionVarName : null,
                };
            }
            return stopDatas;
        }

        // Replaces the tracked stop var names with the popup's; returns whether any changed,
        // so callers can force-notify a write when the resolved value is unchanged.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool SetStopNames(BackgroundGradientStopsEditor.StopData[] stopDatas)
        {
            bool changed = m_StopVarNames.Count != stopDatas.Length;
            for (int i = 0; i < stopDatas.Length && !changed; ++i)
            {
                if (m_StopVarNames[i] != (stopDatas[i].colorVarName, stopDatas[i].positionVarName))
                    changed = true;
            }

            m_StopVarNames.Clear();
            for (int i = 0; i < stopDatas.Length; ++i)
                m_StopVarNames.Add((stopDatas[i].colorVarName, stopDatas[i].positionVarName));

            return changed;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
