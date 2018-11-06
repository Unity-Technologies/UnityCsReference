// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    internal class VisualTreeLayoutUpdater : BaseVisualTreeUpdater
    {
        const int kMaxValidateLayoutCount = 5;

        public override string description
        {
            get { return "Update Layout"; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Layout | VersionChangeType.Hierarchy)) == 0)
                return;

            var yogaNode = ve.yogaNode;
            if (yogaNode != null && yogaNode.IsMeasureDefined)
            {
                yogaNode.MarkDirty();
            }
        }

        public override void Update()
        {
            // update flex once
            int validateLayoutCount = 0;
            while (visualTree.yogaNode.IsDirty)
            {
                // Doing multiples layout pass require to update the styles or else the
                // elements may not be initialized properly and the resulting layout will be invalid.
                if (validateLayoutCount > 0)
                    panel.ApplyStyles();

                visualTree.yogaNode.CalculateLayout();
                using (new EventDispatcher.Gate(visualTree.panel.dispatcher))
                {
                    UpdateSubTree(visualTree);
                }

                if (validateLayoutCount++ >= kMaxValidateLayoutCount)
                {
                    Debug.LogError("Layout update is struggling to process current layout (consider simplifying to avoid recursive layout): " + visualTree);
                    break;
                }
            }
        }

        private void UpdateSubTree(VisualElement root)
        {
            Rect yogaRect = new Rect(root.yogaNode.LayoutX, root.yogaNode.LayoutY, root.yogaNode.LayoutWidth, root.yogaNode.LayoutHeight);
            Rect lastRect = root.renderData.lastLayout;
            bool rectChanged = lastRect != yogaRect;

            // If the last layout rect is different than the current one we must dirty transform on children
            if (rectChanged)
            {
                if (yogaRect.position != lastRect.position)
                {
                    root.IncrementVersion(VersionChangeType.Transform);
                }
                root.IncrementVersion(VersionChangeType.Clip);
                root.renderData.lastLayout = yogaRect;
            }

            // ignore clean sub trees
            bool hasNewLayout = root.yogaNode.HasNewLayout;
            if (hasNewLayout)
            {
                for (int i = 0; i < root.shadow.childCount; ++i)
                {
                    UpdateSubTree(root.shadow[i]);
                }
            }

            if (rectChanged)
            {
                using (var evt = GeometryChangedEvent.GetPooled(lastRect, yogaRect))
                {
                    evt.target = root;
                    root.SendEvent(evt);
                }
            }

            if (hasNewLayout)
            {
                root.yogaNode.MarkLayoutSeen();
                // Layout change require a repaint
                root.IncrementVersion(VersionChangeType.Repaint);
            }
        }
    }
}
