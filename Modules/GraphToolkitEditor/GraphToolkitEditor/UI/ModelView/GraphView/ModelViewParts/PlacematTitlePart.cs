// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class PlacematTitlePart : EditableTitlePart
    {
        const float k_TitleToHeight = 4;
        const float k_MinHeightAt12pt = 57;

        /// <summary>
        /// Creates a new instance of the <see cref="PlacematTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">Options for the text.</param>
        /// <returns>A new instance of <see cref="EditableTitlePart"/>.</returns>
        public new static PlacematTitlePart Create(string name, Model model, ChildView ownerElement, string parentClassName, int options = Options.UseEllipsis)
        {
            if (model is PlacematModel)
            {
                return new PlacematTitlePart(name, model, ownerElement, parentClassName, options);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacematTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="options">Options for the text.</param>
        protected PlacematTitlePart(string name, Model model, ChildView ownerElement, string parentClassName, int options)
            : base(name, model, ownerElement, parentClassName, options) { }

        static readonly string k_PlatformPath = (Application.platform == RuntimePlatform.WindowsEditor) ? "Windows/" : "macOS/";
        static readonly Cursor k_MoveTitle = new() { texture = EditorGUIUtility.Load("Cursors/" + k_PlatformPath + "Grid.MoveTool.png") as Texture2D, hotspot = new Vector2(7, 6) };

        public static readonly string leftAlignClassName = ussClassName.WithUssModifier("left-align");
        public static readonly string rightAlignClassName = ussClassName.WithUssModifier("right-align");

        GraphViewZoomMode m_CurrentMode;

        protected override void BuildUI(VisualElement container)
        {
            base.BuildUI(container);

            TitleContainer.Q<Label>().style.cursor = TitleContainer.style.cursor = k_MoveTitle;
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (m_Model is PlacematModel placematModel)
            {
                WantedTextSize = placematModel.TitleFontSize;

                TitleLabel.EnableInClassList(leftAlignClassName, placematModel.TitleAlignment == TextAlignment.Left);
                TitleLabel.EnableInClassList(rightAlignClassName, placematModel.TitleAlignment == TextAlignment.Right);

                if (m_CurrentZoom != 0)
                    base.SetLevelOfDetail(m_CurrentZoom, m_CurrentMode, GraphViewZoomMode.Normal);

                if (visitor.ChangeHints.HasChange(ChangeHint.Data))
                {
                    TitleContainer.tooltip = placematModel.Title;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(k_LodMinTextSize, out var value))
                LodMinTextSize = value;
        }

        bool m_UpdateHeightRegistered;

        /// <inheritdoc />
        public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            m_CurrentMode = newZoomMode;
            base.SetLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            float height = TitleContainer.layout.height;

            if (float.IsFinite(height))
                TitleContainer.style.marginTop = -height;

            if (!m_UpdateHeightRegistered)
            {
                TitleContainer.RegisterCallback<GeometryChangedEvent>(UpdateHeight);
                m_UpdateHeightRegistered = true;
            }
        }

        void UpdateHeight(GeometryChangedEvent e)
        {
            float height = TitleContainer.layout.height;
            if (float.IsFinite(height) && TitleContainer.resolvedStyle.marginTop != height)
                TitleContainer.style.marginTop = -height;
        }
    }
}
