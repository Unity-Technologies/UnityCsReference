// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="GroupModelBase"/>.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardGroup : BlackboardElement
    {
        /// <summary>
        /// The USS class for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-variable-group";

        /// <summary>
        /// The USS class for the title container.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleContainerName);

        /// <summary>
        /// The USS class for the folder icon.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// The USS class for the drag indicator.
        /// </summary>
        public static readonly string dragIndicatorUssClassName = ussClassName.WithUssElement("drag-indicator");

        /// <summary>
        /// The Label displaying the title.
        /// </summary>
        protected VisualElement m_TitleLabel;

        /// <summary>
        /// The toggle button for collapsing the group.
        /// </summary>
        protected Toggle m_TitleToggle;

        /// <summary>
        /// The title element.
        /// </summary>
        protected VisualElement m_Title;

        /// <summary>
        /// The name of the title part.
        /// </summary>
        protected static readonly string k_TitlePartName = "title-part";

        /// <summary>
        /// The icon element.
        /// </summary>
        protected Image m_Icon;

        GroupModelBase GroupModelBase => Model as GroupModelBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardGroup"/> class.
        /// </summary>
        public BlackboardGroup()
        {
            RegisterCallback<PromptItemLibraryEvent>(OnPromptItemLibrary);
        }

        static Texture s_FolderIcon;

        /// <summary>
        /// The icon of the group.
        /// </summary>
        public virtual Texture Icon
        {
            get
            {
                if (s_FolderIcon == null)
                    s_FolderIcon = EditorGUIUtility.FindTexture("Folder Icon");
                return s_FolderIcon;
            }
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            AddToClassList(ussClassName);

            base.BuildUI();

            m_Title = new VisualElement();
            m_Title.AddToClassList(titleContainerUssClassName);

            m_Icon = new Image();
            m_Icon.AddToClassList(iconUssClassName);
            m_Title.Add(m_Icon);

            var iconTexture = Icon;
            if (iconTexture != null)
            {
                m_Icon.image = iconTexture;
            }

            Add(m_Title);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(EditableTitlePart.Create(k_TitlePartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TitleLabel = PartList.GetPart(k_TitlePartName).Root;

            m_Title.Insert(m_Icon == null ? 0 : 1, m_TitleLabel);
        }

        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);
            BlackboardView.Blackboard.RefreshGroup(GroupModelBase);
        }

        void OnPromptItemLibrary(PromptItemLibraryEvent e)
        {
            BlackboardView.Blackboard.ShowCreateVariableLibrary(e.MenuPosition, Model as GroupModel);
        }

        public override void ActivateRename()
        {
            (PartList.GetPart(k_TitlePartName) as EditableTitlePart)?.BeginEditing();
        }

        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (copyPasteData.HasVariableContent() && GroupModelBase is GroupModel gm)
            {
                BlackboardView.Dispatch(new PasteDataCommand(operation, operationName, delta, copyPasteData, gm));
                return true;
            }

            return false;
        }
    }
}
