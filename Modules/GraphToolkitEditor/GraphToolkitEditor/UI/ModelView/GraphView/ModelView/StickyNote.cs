// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UI for a <see cref="StickyNoteModel"/>.
    /// </summary>
    [UxmlElement]
    [UnityRestricted]
    [UsedImplicitly]
    internal partial class StickyNote : GraphElement
    {
        [Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            public override object CreateInstance() => new StickyNote();
        }

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        /// <summary>
        /// The USS class name added to a <see cref="StickyNote"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-sticky-note";

        /// <summary>
        /// The USS class name prefix used for the theme of the sticky note.
        /// </summary>
        public static readonly string themeClassNamePrefix = ussClassName.WithUssModifier("theme-");

        /// <summary>
        /// The USS class name prefix used for the size of the sticky note.
        /// </summary>
        public static readonly string sizeClassNamePrefix = ussClassName.WithUssModifier("size-");

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the title container.
        /// </summary>
        public static readonly string titleContainerPartName = GraphElementHelper.titleContainerName;

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the text container.
        /// </summary>
        public static readonly string contentContainerPartName = "text-container";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the resizer.
        /// </summary>
        public static readonly string resizerPartName = "resizer";

        string m_CurrentThemeClassName;
        string m_CurrentSizeClassName;

        public StickyNoteModel StickyNoteModel => Model as StickyNoteModel;

        /// <summary>
        /// Creates an instance of the <see cref="StickyNote"/> class.
        /// </summary>
        public StickyNote() : base(true)
        { }

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
            this.AddPackageStylesheet("StickyNote.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                SetPositionAndSize(StickyNoteModel.PositionAndSize);
            }

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                this.ReplaceAndCacheClassName(themeClassNamePrefix + StickyNoteModel.Theme.ToKebabCase(), ref m_CurrentThemeClassName);
                this.ReplaceAndCacheClassName(sizeClassNamePrefix + StickyNoteModel.TextSize.ToKebabCase(), ref m_CurrentSizeClassName);
            }
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

        public static IEnumerable<string> GetThemes() => Enum.GetNames(typeof(StickyNoteColorTheme));

        public static IEnumerable<string> GetSizes() => Enum.GetNames(typeof(StickyNoteTextSize));

        /// <inheritdoc />
        public override void ActivateRename()
        {
            GraphView.Window.Focus();
            (PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.BeginEditing();
        }
    }
}
