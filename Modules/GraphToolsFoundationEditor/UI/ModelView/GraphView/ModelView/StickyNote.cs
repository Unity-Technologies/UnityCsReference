// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for a <see cref="StickyNoteModel"/>.
    /// </summary>
    class StickyNote : GraphElement
    {
        public new class UxmlFactory : UxmlFactory<StickyNote> {}

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public new static readonly string ussClassName = "ge-sticky-note";
        public static readonly string themeClassNamePrefix = ussClassName.WithUssModifier("theme-");
        public static readonly string sizeClassNamePrefix = ussClassName.WithUssModifier("size-");

        public static readonly string titleContainerPartName = "title-container";
        public static readonly string contentContainerPartName = "text-container";
        public static readonly string resizerPartName = "resizer";

        string m_CurrentThemeClassName;
        string m_CurrentSizeClassName;

        public StickyNoteModel StickyNoteModel => Model as StickyNoteModel;

        /// <summary>
        /// Creates an instance of the <see cref="StickyNote"/> class.
        /// </summary>
        public StickyNote():base(true)
        {}

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName, EditableTitlePart.Options.Multiline | EditableTitlePart.Options.UseEllipsis));
            PartList.AppendPart(StickyNoteContentPart.Create(contentContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("StickyNote.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            SetPositionAndSize(StickyNoteModel.PositionAndSize);

            this.ReplaceAndCacheClassName(themeClassNamePrefix + StickyNoteModel.Theme.ToKebabCase_Internal(), ref m_CurrentThemeClassName);
            this.ReplaceAndCacheClassName(sizeClassNamePrefix + StickyNoteModel.TextSize.ToKebabCase_Internal(), ref m_CurrentSizeClassName);
        }

        /// <summary>
        /// Sets the position and the size of the placemat.
        /// </summary>
        /// <param name="positionAndSize">The position and size.</param>
        public void SetPositionAndSize(Rect positionAndSize)
        {
            SetPosition(positionAndSize.position);
            if (!PositionIsOverriddenByManipulator)
            {
                style.height = positionAndSize.height;
                style.width = positionAndSize.width;
            }
        }

        public static IEnumerable<string> GetThemes()
        {
            return Enum.GetNames(typeof(StickyNoteColorTheme));
        }

        public static IEnumerable<string> GetSizes()
        {
            return Enum.GetNames(typeof(StickyNoteTextSize));
        }

        /// <inheritdoc />
        public override void ActivateRename()
        {
            GraphView.Window.Focus();
            (PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.BeginEditing();
        }
    }
}
