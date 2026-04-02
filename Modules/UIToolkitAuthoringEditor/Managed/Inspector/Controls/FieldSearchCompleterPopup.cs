// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Popup used by the field search completer. It contains a ListView to display the results, and a Label to display
/// the number of results found. It also handles the hover state of the items in the ListView, and provides events for
/// when an item is chosen or when the selection changes.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class FieldSearchCompleterPopup : Popup
{
    public new const string UssClassName = "unity-field-search-completer-popup";
    public const string DetailsViewUssClassName = UssClassName + "__details-view";
    public const string FooterUssClassName = UssClassName + "__footer";
    public const string ResultLabelUssClassName = UssClassName + "__result-label";

    private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/Controls/Completers/FieldSearchCompleterPopup.uss";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/Controls/Completers/FieldSearchCompleterPopupDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/Controls/Completers/FieldSearchCompleterPopupLight.uss";

    int m_HoveredIndex = -1;

    /// <summary>
    /// Event invoked when an item in the ListView is chosen. The index of the chosen item is passed as a parameter.
    /// </summary>
    public Action<int> ElementChosen;

    /// <summary>
    /// Event invoked when the selection in the ListView changes. The index of the newly selected item is passed as a
    /// parameter.
    /// </summary>
    public Action<int> SelectionChanged;

    /// <summary>
    /// Event invoked when the hovered item in the ListView changes. The index of the newly hovered item is passed as a
    /// parameter. If no item is hovered, the index will be -1.
    /// </summary>
    public event Action<int> HoveredItemChanged;

    /// <summary>
    /// The index of the currently hovered item in the ListView. This property is updated when the user moves the mouse
    /// over the ListView, and it triggers the HoveredItemChanged event when it changes. If no item is hovered,
    /// the index will be -1.
    /// </summary>
    public int HoveredIndex
    {
        get => m_HoveredIndex;
        set
        {
            if (m_HoveredIndex == value)
                return;
            m_HoveredIndex = value;
            HoveredItemChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// The listView used to display the search results. The itemsSource of the ListView can be set to display the
    /// results, and the ElementChosen and SelectionChanged events can be used to handle user interactions with the
    /// ListView.
    /// </summary>
    public ListView ListView { get; }

    /// <summary>
    /// The label element used to display the number of results found. The text of the label can be set to provide
    /// feedback to the user about the search results. The label is styled with the ResultLabelUssClassName class for
    /// consistent styling within the popup.
    /// </summary>
    public Label ResultLabel { get; }

    /// <summary>
    /// Constructor for the FieldSearchCompleterPopup. It initializes the ListView and ResultLabel, sets up the
    /// necessary event handlers for user interactions, and configures the styling and behavior of the popup to ensure
    /// a smooth user experience when displaying search results. The ListView is configured to allow single click choice
    /// and to prevent focus changes when interacting with the popup, while the ResultLabel is styled to provide clear
    /// feedback on the search results. The popup's geometry is also adjusted to fit the content appropriately.
    /// </summary>
    public FieldSearchCompleterPopup()
    {
        AddToClassList(UssClassName);

        ListView = new ListView();

        var sv = ListView.Q<ScrollView>();
        sv.style.flexGrow = 0;
        sv.style.flexShrink = 1;
        sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        ListView.itemsChosen += (obj) =>
        {
            ElementChosen?.Invoke(ListView.selectedIndex);
        };

        ListView.selectionChanged += (obj) =>
        {
            SelectionChanged?.Invoke(ListView.selectedIndex);
        };

        ListView.allowSingleClickChoice = true;

        Add(ListView);
        Add(ResultLabel = new Label());
        ResultLabel.AddToClassList(ResultLabelUssClassName);

        // Avoid focus change when clicking on the popup
        ListView.Q<ScrollView>().contentContainer.focusable = true;
        ListView.Query<Scroller>().ForEach(s =>
        {
            s.focusable = false;
        });
        ListView.focusable = false;
        ListView.delegatesFocus = false;
        RegisterCallback<PointerDownEvent>(e =>
        {
            e.StopImmediatePropagation();
        });
        ListView.scrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        ListView.scrollView.contentContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
        style.minHeight = 0;

        // Load assets.
        var mainUSS = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
        var themeUSSPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var themeUSS = EditorGUIUtility.Load(themeUSSPath) as StyleSheet;

        styleSheets.Add(mainUSS);
        styleSheets.Add(themeUSS);
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        var index = ListView.virtualizationController.GetIndexFromPosition(evt.localPosition);
        var count = ListView.viewController.itemsSource?.Count ?? 0;

        if (index > count - 1)
        {
            HoveredIndex = -1;
        }
        else
        {
            HoveredIndex = index;
        }
    }

    void OnPointerLeaveEvent(PointerLeaveEvent evt)
    {
        HoveredIndex = -1;
    }

    public override void AdjustGeometry()
    {
        const int minListViewHeight = 160;

        base.AdjustGeometry();
        ListView.style.minHeight = Math.Min(minListViewHeight,
            ListView.fixedItemHeight * (ListView.itemsSource?.Count ?? 0));
    }

    public void Refresh()
    {
        ListView.RefreshItems();
        ListView.ClearSelection();
    }
}
