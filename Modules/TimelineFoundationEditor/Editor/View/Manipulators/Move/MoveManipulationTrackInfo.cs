// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class MoveManipulationTrackInfo
    {
        public Track trackUnderMouse { get; private set; }
        public bool trackUnderMouseHasChanged { get; private set; }
        public Rect trackUnderMouseRect { get; private set; }
        public float verticalDetachOffset { get; private set; }
        float detachOffsetRatio { get; }

        public MoveManipulationTrackInfo(Vector2 mousePosition, TrackElement trackElement)
        {
            if (trackElement == null)
                throw new ArgumentNullException(nameof(trackElement));

            detachOffsetRatio = CalculateDetachOffsetRatio(mousePosition, trackElement.worldBound);
            SetTrackUnderMouse(trackElement);
        }

        public void UpdateTrackUnderMouse(VisualElement target, Vector2 position)
        {
            bool isWithinTrackTarget = position.y >= trackUnderMouseRect.yMin && position.y <= trackUnderMouseRect.yMax;
            if (isWithinTrackTarget) //mouse did not leave target, no need to pick
            {
                trackUnderMouseHasChanged = false;
                return;
            }

            trackUnderMouseHasChanged = true;
            var targetTrackElement = MoveManipulatorUtils.PickElement<TrackElement>(target, position);

            if (targetTrackElement != null)
            {
                SetTrackUnderMouse(targetTrackElement);
            }
            else
            {
                trackUnderMouse = null;
                trackUnderMouseRect = Rect.zero;
            }
        }

        void SetTrackUnderMouse(TrackElement trackElement)
        {
            trackUnderMouse = trackElement.track;
            trackUnderMouseRect = trackElement.worldBound;
            verticalDetachOffset = detachOffsetRatio * trackUnderMouseRect.height;
        }

        static float CalculateDetachOffsetRatio(Vector2 mousePosition, Rect targetWorldRect)
        {
            float offset = mousePosition.y - targetWorldRect.yMin;
            return offset / targetWorldRect.height;
        }
    }
}
