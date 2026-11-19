// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// An element with a <see cref="GridViewComponent"/>: a grid of resizable cells whose track sizes persist in the
    /// view data when it has a view data key.
    /// </summary>
    /// <remarks>
    /// A component does not receive the view data of its owner, so the persistence needs this element.
    /// </remarks>
    [UxmlElement]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class GridViewControl : VisualElement
    {
        internal static readonly BindingId columnsProperty = nameof(columns);
        internal static readonly BindingId rowsProperty = nameof(rows);

        public GridViewControl()
            : this(null, null)
        {
        }

        public GridViewControl(string columns, string rows)
        {
            AddComponent(GridViewComponent.Create(columns, rows));
        }

        /// <summary>
        /// The initial column tracks, in the <c>grid-template-columns</c> syntax. Empty to use the style's.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public string columns
        {
            get => GetComponent<GridViewComponent>().columns;
            set
            {
                var previous = columns;
                GetComponent<GridViewComponent>().SetColumns(this, value);
                if (string.CompareOrdinal(previous, columns) != 0)
                    NotifyPropertyChanged(columnsProperty);
            }
        }

        /// <summary>
        /// The initial row tracks, in the <c>grid-template-rows</c> syntax. Empty to use the style's.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public string rows
        {
            get => GetComponent<GridViewComponent>().rows;
            set
            {
                var previous = rows;
                GetComponent<GridViewComponent>().SetRows(this, value);
                if (string.CompareOrdinal(previous, rows) != 0)
                    NotifyPropertyChanged(rowsProperty);
            }
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            GridViewComponent.OnViewDataReady(this);
        }
    }
}
