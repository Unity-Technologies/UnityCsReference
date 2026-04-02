// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Interface for the field search completer. It is used to abstract the implementation of the field search completer
/// and provide a way to refresh the results from the outside, for example from a custom property drawer that uses the
/// field search completer.
/// </summary>
interface IFieldSearchCompleter
{
    /// <summary>
    /// The popup used to display the search results. It can be used to customize the popup, for example by adding a
    /// details view or a footer.
    /// </summary>
    public FieldSearchCompleterPopup Popup { get; }

    /// <summary>
    /// Refreshes the result.
    /// </summary>
    public void Refresh();
}

/// <summary>
/// This class is responsible for providing auto-completion functionality for a TextField.
/// It listens to the events of the TextField and shows a popup with the matching results based on the text input.
/// The data source, filtering and matching logic can be customized through callbacks.
/// </summary>
/// <typeparam name="TData">The data type of the result of the completion</typeparam>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class FieldSearchCompleter<TData> : IFieldSearchCompleter
{
    const int k_ItemHeight = 20;
    internal const int k_PauseDelay = 250;

    static readonly List<TData> k_SEmptyMatchingDataList = [];

    // Properties
    bool m_Enabled = true;
    IEnumerable<TData> m_DataSource;
    List<TData> m_Results;

    // Internal state
    bool m_DataSourceDirty;
    IVisualElementScheduledItem m_ScheduledFilterUpdate;

    // Callbacks
    public delegate IEnumerable<TData> GetDataSourceDelegate();
    public delegate bool MatcherDelegate(string filter, TData data);
    public delegate string GetFilterFromTextDelegate(string text);
    public delegate string GetTextFromDataDelegate(TData data);
    public delegate string GetFormattedResultCountDelegate(int count);

    // List view properties
    int m_ItemHeight;
    Func<VisualElement> m_MakeItem;
    Action<VisualElement> m_DestroyItem;
    Action<VisualElement, int> m_BindItem;

    MatcherDelegate m_MatcherCallback;

    // UI parts
    FieldSearchCompleterPopup m_Popup;
    VisualElement m_DetailsContent;
    VisualElement m_Footer;

    private Rect m_PreviousTextFieldWorldPosition;

    private bool m_TemporarilyDontShowPopup;

    private bool m_IsEvaluatingFocus;
    private bool m_HasPendingValueChanged;
    private string m_PendingPreviousValue;

    /// <summary>
    /// Indicates whether the popup is a native popup window rather than a overlay VisualElement.
    /// The default value is false.
    /// </summary>
    public bool UsesRealPopupWindow { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field search completer is enabled. When disabled, the completer will
    /// not show any results and will not listen to the events of the attached TextField.
    /// </summary>
    public bool Enabled
    {
        get => m_Enabled;
        set
        {
            if (m_Enabled == value)
                return;

            m_Enabled = value;

            if (!m_Enabled)
            {
                SetResults(k_SEmptyMatchingDataList);
                DisconnectFromField();
                m_Popup?.Hide();
            }
            else
            {
                ConnectToField();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the popup should always be visible when the TextField is focused,
    /// even if there is no text input or if the input does not match any result.
    /// The default value is false, which means that the popup will only be visible when there are matching results for
    /// the current text input.
    /// </summary>
    public bool AlwaysVisible { get; set; }

    /// <summary>
    /// The list of results that match the current filter. This is set internally by the FieldSearchCompleter when the
    /// text changes and is used as the items source for the popup list view.
    /// </summary>
    public ReadOnlyCollection<TData> Results => m_Results?.AsReadOnly();

    /// <summary>
    /// The data source for the field search completer.
    /// </summary>
    public IEnumerable<TData> DataSource
    {
        get
        {
            if (!m_DataSourceDirty)
                return m_DataSource;

            m_DataSource = DataSourceCallback?.Invoke();
            m_DataSourceDirty = false;
            return m_DataSource;
        }
    }

    /// <summary>
    /// The callback used to retrieve the data source for the field search completer. This allows the data source to be
    /// dynamic and updated every time the popup is shown. If not set, the data source will be null. Setting this
    /// callback will mark the data source as dirty, which means that it will be refreshed the next time it is accessed.
    /// </summary>
    public GetDataSourceDelegate DataSourceCallback { get; set; }

    /// <summary>
    /// The callback used to match the current filter with the data. It takes the current filter and a data item as
    /// parameters and returns true if the data item matches the filter and should be included in the results, or false otherwise. If not set, the default matcher will be used, which checks if the string representation of the data item contains the filter as a substring, ignoring case. Setting this callback will cause the field search completer to use it for matching the filter with the data items in the data source.
    /// </summary>
    public MatcherDelegate MatcherCallback
    {
        get
        {
            m_MatcherCallback ??= DefaultMatcher;

            return m_MatcherCallback;
        }
        set => m_MatcherCallback = value;
    }

    /// <summary>
    /// Gets the currently selected data item in the popup list view.
    /// If there is no selection then this will return the default value of TData.
    /// </summary>
    public TData SelectedData
    {
        get
        {
            if (m_Popup == null)
                return default;

            var index = m_Popup.ListView.selectedIndex;

            return index != -1 ? m_Results[index] : default;

        }
    }

    /// <summary>
    /// The height of each item in the popup list view.
    /// Setting this property will update the fixed item height of the list view if the popup is already created.
    /// The default value is 20.
    /// </summary>
    public int ItemHeight
    {
        get => m_ItemHeight;
        set
        {
            m_ItemHeight = value;

            if (m_Popup == null)
                return;

            m_Popup.ListView.fixedItemHeight = m_ItemHeight;
        }
    }

    /// <summary>
    /// Callback used to create a new visual element for each item in the popup list view. This is used by the list
    /// view as the makeItem callback. Setting this property will update the makeItem callback of the list view if the
    /// popup is already created. If not set, the default makeItem will be used, which creates a new Label for each
    /// item.
    /// </summary>
    public Func<VisualElement> MakeItem
    {
        get => m_MakeItem;
        set
        {
            m_MakeItem = value;
            if (m_Popup != null)
                m_Popup.ListView.makeItem = m_MakeItem;
        }
    }

    /// <summary>
    /// Callback used to destroy a visual element for each item in the popup list view. This is used by the list view
    /// as the destroyItem callback. Setting this property will update the destroyItem callback of the list view if the
    /// popup is already created. If not set, no callback will be used for destroying items.
    /// </summary>
    public Action<VisualElement> DestroyItem
    {
        get => m_DestroyItem;
        set
        {
            m_DestroyItem = value;
            if (m_Popup != null)
                m_Popup.ListView.destroyItem = m_DestroyItem;
        }
    }

    /// <summary>
    /// Callback used to bind a visual element to an item in the popup list view. This is used by the list view as the
    /// bindItem callback. Setting this property will update the bindItem callback of the list view if the popup is
    /// already created. If not set, the default bindItem will be used, which sets the text of a Label to the string
    /// representation of the data item.
    /// </summary>
    public Action<VisualElement, int> BindItem
    {
        get => m_BindItem;
        set
        {
            m_BindItem = value;
            if (m_Popup != null)
                m_Popup.ListView.bindItem = m_BindItem;
        }
    }

    /// <summary>
    /// Callback used to get the filter string from the text input of the attached TextField.
    /// This allows to have a different filter string than the text input, for example by removing some characters or
    /// by using a specific format. Setting this property will cause the field search completer to use it for getting
    /// the filter string from the text input. If not set, the default implementation will return the text input as is.
    /// </summary>
    public GetFilterFromTextDelegate GetFilterFromTextCallback { get; set; }

    /// <summary>
    /// Callback used to get the text to display for each data item in the popup list view. This is used by the default
    /// bindItem callback to set the text of a Label to the string representation of the data item.
    /// Setting this property will cause the default bindItem callback to use it for getting the text to display for
    /// each data item. If not set, the default implementation will return the string representation of the data item
    /// by calling ToString() on it.
    /// </summary>
    public GetTextFromDataDelegate GetTextFromDataCallback { get; set; }

    /// <summary>
    /// Callback used to get the text used to display the number of results found in the popup.
    /// </summary>
    public GetFormattedResultCountDelegate GetFormattedResultCountCallback { get; set; }

    /// <summary>
    /// The popup used to display the search results. This can be used to customize the popup, for example by adding a
    /// details view or a footer.
    /// </summary>
    public FieldSearchCompleterPopup Popup => m_Popup;

    /// <summary>
    /// The TextField to which the field search completer is attached. The field search completer listens to the events
    /// of this TextField and shows the popup with the matching results based on the text input of this TextField.
    /// Setting up the attached TextField must be done through the SetupCompleterField method to ensure that the
    /// callbacks are properly registered and unregistered.
    /// </summary>
    public TextField AttachedTextField { get; private set; }

    /// <summary>
    /// Event triggered when the hovered item in the popup list view changes. The event parameter is the data item
    /// corresponding to the currently hovered item, or the default value of TData if there is no hovered item.
    /// This event can be used to update a details view in the popup based on the currently hovered item.
    /// </summary>
    public event Action<TData> HoveredItemChanged;

    /// <summary>
    /// Event triggered when the selected item in the popup list view changes. The event parameter is the data item
    /// corresponding to the currently selected item, or the default value of TData if there is no selected item.
    /// </summary>
    public event Action<TData> SelectionChanged;

    /// <summary>
    /// Event triggered when an item is chosen from the popup list view, either by clicking on it or by pressing Enter.
    /// </summary>
    public event Action<int> ItemChosen;

    /// <summary>
    /// Constructor for the FieldSearchCompleter with defaults values. The default makeItem callback creates a new Label
    /// for each item, and the default bindItem callback sets the text of the Label to the string representation of the
    /// data item using the GetTextFromData method. The default item height is set to 20.
    /// </summary>
    public FieldSearchCompleter()
    {
        ItemHeight = k_ItemHeight;
        MakeItem = () =>
        {
            var item = new Label();
            return item;
        };
        BindItem = (e, i) =>
        {
            if (e is Label label)
                label.text = GetTextFromData(m_Results[i]);
        };
        SetResults(k_SEmptyMatchingDataList);
    }

    void SetResults(List<TData> results)
    {
        // Refresh every time set is called even when it has not changed
        m_Results = results;
        if (m_Popup == null)
            return;
        Popup.ListView.itemsSource = results;
        Popup.ResultLabel.text = GetResultCountText(m_Results?.Count ?? 0);
        Popup.Refresh();
    }

    /// <summary>
    /// Sets up the field search completer to work with the given TextField. This method registers the necessary
    /// callbacks to listen to the events of the TextField and show the popup with the matching results based on the
    /// text input of the TextField. The useRealWindow parameter indicates whether to use a native popup window for the completer or an overlay VisualElement. This method should be called to attach the field search completer to a TextField, and it can be called again with a different TextField to change the attachment. If the field search completer is already attached to a TextField, it will be detached from it before attaching to the new one.
    /// </summary>
    /// <param name="field">The attached text field</param>
    /// <param name="useRealWindow">Indicates whether the completer popup should use a real window</param>
    public void SetupCompleterField(TextField field, bool useRealWindow)
    {
        UsesRealPopupWindow = useRealWindow;

        if (AttachedTextField == field)
            return;

        DisconnectFromField();

        AttachedTextField = field;

        ConnectToField();
        if (m_Popup != null)
            m_Popup.AnchoredElement = AttachedTextField.visualInput;
    }

    /// <summary>
    /// Ensures that the popup is created and properly initialized. This method is called when the TextField receives
    /// focus to make sure that the popup is ready to be shown with the matching results. If the popup is already
    /// created, this method does nothing. If not, it creates a new popup using the CreatePopup method, sets up the
    /// necessary callbacks for item selection and hover, and adds the details content and footer content if they are
    /// defined. Finally, it calls UpdatePopup to apply the current settings to the popup.
    /// </summary>
    public void EnsurePopupIsCreated()
    {
        if (m_Popup == null)
        {
            m_Popup = CreatePopup();
            m_Popup.UsesRealWindow = UsesRealPopupWindow;

            if (m_Footer == null)
            {
                CreateFooterContent();
            }

            if (m_Footer != null)
            {
                m_Popup.Add(m_Footer);
            }

            if (m_DetailsContent == null)
            {
                CreateDetailsContent();
            }

            if (m_DetailsContent != null)
            {
                m_Popup.Add(m_DetailsContent);
            }

            m_Popup.ElementChosen += (index) =>
            {
                CancelEvaluateFocus();
                AttachedTextField.SetValueWithoutNotify(GetTextFromData(m_Results[index]));
                m_TemporarilyDontShowPopup = true;
                AttachedTextField.schedule.Execute((e) =>
                {
                    AttachedTextField.Blur();
                    m_TemporarilyDontShowPopup = false;
                }).ExecuteLater(k_PauseDelay);
                if (UsesRealPopupWindow)
                {
                    m_Popup?.Hide();
                }

                ItemChosen?.Invoke(index);
            };
            m_Popup.HoveredItemChanged += (index) =>
                HoveredItemChanged?.Invoke(index != -1 ? m_Results[index] : default);
            m_Popup.SelectionChanged += (index) =>
                SelectionChanged?.Invoke(index != -1 ? m_Results[index] : default);
            m_Popup.OnHide += OnPopupHide;
            UpdatePopup();
        }
    }

    void OnPopupHide()
    {
        ScheduleEvaluateFocus();
    }

    /// <summary>
    /// Makes the details content to be added to the popup. This method can be overridden to provide a custom details
    /// view in the popup, for example to show more information about the currently hovered item. If this method returns
    /// null, no details view will be added to the popup.
    /// </summary>
    /// <returns>The created details content</returns>
    protected virtual VisualElement MakeDetailsContent() => null;

    void CreateDetailsContent()
    {
        m_DetailsContent?.RemoveFromClassList(FieldSearchCompleterPopup.DetailsViewUssClassName);
        m_DetailsContent = MakeDetailsContent();
        m_DetailsContent?.AddToClassList(FieldSearchCompleterPopup.DetailsViewUssClassName);
    }

    /// <summary>
    /// Makes the footer content to be added to the popup. This method can be overridden to provide a custom footer in
    /// the popup, for example to show the number of results found or to add buttons for additional actions.
    /// If this method returns null, no footer will be added to the popup.
    /// </summary>
    /// <returns></returns>
    protected virtual VisualElement MakeFooterContent()
    {
        return null;
    }

    void CreateFooterContent()
    {
        m_Footer?.RemoveFromClassList(FieldSearchCompleterPopup.FooterUssClassName);
        m_Footer = MakeFooterContent();
        m_Footer?.AddToClassList(FieldSearchCompleterPopup.FooterUssClassName);
    }

    void ConnectToField()
    {
        if (AttachedTextField != null && Enabled)
        {
            AttachedTextField.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            AttachedTextField.RegisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
            AttachedTextField.RegisterValueChangedCallback(OnTextValueChange);
            AttachedTextField.Q(TextField.textInputUssName)
                .RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            AttachedTextField.RegisterCallback<DetachFromPanelEvent>(OnTextFieldDetached);
            AttachedTextField.RegisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
            AttachedTextField.RegisterCallback<FocusInEvent>(OnTextFieldFocusInPrepareTracking);
            AttachedTextField.RegisterCallback<GeometryChangedEvent>(OnTextFieldGeometryChangedEvent);
            m_PreviousTextFieldWorldPosition = GUIUtility.GUIToScreenRect(AttachedTextField.visualInput.worldBound);
        }
    }

    void OnTextFieldFocusInPrepareTracking(FocusInEvent _)
    {
        PrepareWindowTracking();
    }

    void PrepareWindowTracking()
    {
        if (!UsesRealPopupWindow)
            return;

        var window = GetEditorWindow();
        if (window == null)
            return;

        UpdateAnchoredControlScreenPosition();
        window.OnWindowGeometryChanged += OnWindowGeometryChanged;
    }

    void OnWindowGeometryChanged(object obj, EventArgs args)
    {
        // Wait for the new layout to be performed after the window geometry change
        AttachedTextField?.schedule.Execute(HandlePopupWindowPositionChanged).ExecuteLater(k_PauseDelay);
    }

    // If the window which hosts the textfield is moved/resized, popup must be forcefully hidden.
    void HandlePopupWindowPositionChanged()
    {
        if (AttachedTextField?.elementPanel?.ownerObject == null)
        {
            return;
        }

        // Use the element panel to find which window "owns" field
        var window = GetEditorWindow();

        if (window == null || window.rootVisualElement == null)
            return;

        UpdateAnchoredControlScreenPosition();

        // If the window is moved, we need to hide the popup
        var textFieldPositionPostUpdate = (Rect)AttachedTextField.visualInput
            .GetProperty(Unity.UIToolkit.Editor.Popup.k_AnchoredElementCachedScreenRectVEPropertyName);

        if (m_PreviousTextFieldWorldPosition.Equals(textFieldPositionPostUpdate))
            return;

        if (window.baseRootVisualElement.focusController.focusedElement == AttachedTextField)
        {
            AttachedTextField.Blur();
        }

        if (window.baseRootVisualElement.focusController.m_LastPendingFocusedElement == AttachedTextField)
        {
            window.baseRootVisualElement.focusController.BlurLastFocusedElement();
        }

        m_PreviousTextFieldWorldPosition = textFieldPositionPostUpdate;
        m_Popup?.Hide();
    }

    void DisconnectFromField()
    {
        if (AttachedTextField != null)
        {
            AttachedTextField.UnregisterCallback<DetachFromPanelEvent>(OnTextFieldDetached);
            AttachedTextField.UnregisterCallback<FocusInEvent>(OnTextFieldFocusInPrepareTracking);
            AttachedTextField.UnregisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
            AttachedTextField.UnregisterCallback<GeometryChangedEvent>(OnTextFieldGeometryChangedEvent);
            AttachedTextField.UnregisterValueChangedCallback(OnTextValueChange);
            AttachedTextField.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            AttachedTextField.UnregisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
            AttachedTextField.Q(TextField.textInputUssName)
                .UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }
    }

    void OnTextFieldDetached(DetachFromPanelEvent evt)
    {
        if (m_Popup != null && m_Popup.IsOpened)
        {
            m_Popup.Hide();
        }
    }

    void OnTextFieldFocusOut(FocusOutEvent evt)
    {
        var window = GetEditorWindow();
        if (window != null)
        {
            window.OnWindowGeometryChanged -= OnWindowGeometryChanged;
            if (evt.relatedTarget != null &&
                ((VisualElement)evt.relatedTarget).GetFirstAncestorWhere((element => element == m_Popup?.GetRoot())) ==
                null)
            {
                m_Popup?.Hide();
            }
        }
    }

    void OnTextFieldGeometryChangedEvent(GeometryChangedEvent evt)
    {
        if (m_Popup != null && m_Popup.IsOpened)
        {
            m_Popup.Hide();
        }
    }

    void UpdatePopup()
    {
        if (m_Popup != null)
        {
            m_Popup.ListView.fixedItemHeight = m_ItemHeight;
            m_Popup.ListView.makeItem = m_MakeItem;
            m_Popup.ListView.destroyItem = m_DestroyItem;
            m_Popup.ListView.bindItem = m_BindItem;
            m_Popup.AnchoredElement = AttachedTextField.visualInput;
        }
    }

    // This has to be called during an event to ensure that GUIUtility.GUIToScreenRect() uses the window containing the attached text field.
    void UpdateAnchoredControlScreenPosition()
    {
        var field = AttachedTextField.visualInput;

        if (field != null)
        {
            field.SetProperty(Unity.UIToolkit.Editor.Popup.k_AnchoredElementCachedScreenRectVEPropertyName, GUIUtility.GUIToScreenRect(field.worldBound));
        }
    }

    protected virtual FieldSearchCompleterPopup CreatePopup() => new FieldSearchCompleterPopup();

    static bool DefaultMatcher(string filter, TData data)
    {
        var text = data.ToString();

        return !string.IsNullOrEmpty(text) && FuzzySearch.FuzzyMatch(filter, text);
    }

    void OnTextChanged()
    {
        UpdateFilter(AttachedTextField.text);
    }

    void ScheduleTextChange()
    {
        if (m_ScheduledFilterUpdate == null)
        {
            m_ScheduledFilterUpdate = AttachedTextField?.schedule.Execute(a => UpdateFilter(AttachedTextField.text));
        }
        else
        {
            m_ScheduledFilterUpdate.ExecuteLater(0);
        }
    }

    protected virtual bool IsValidText(string text)
    {
        return true;
    }

    /// <summary>
    /// Refreshes the result.
    /// </summary>
    public void Refresh()
    {
        m_DataSourceDirty = true;
        if (m_Popup != null)
        {
            OnTextChanged();
        }
    }

    void UpdateFilter(string text)
    {
        if (!IsValidText(text))
        {
            SetResults(k_SEmptyMatchingDataList);
            Popup?.Hide();
            return;
        }

        SetFilter(GetFilterFromText(text));
    }

    private string GetFilterFromText(string text)
    {
        return GetFilterFromTextCallback != null ? GetFilterFromTextCallback(text) : text;
    }

    protected virtual string GetTextFromData(TData data)
    {
        return GetTextFromDataCallback != null ? GetTextFromDataCallback(data) : data.ToString();
    }

    protected virtual string GetResultCountText(int count)
    {
        return GetFormattedResultCountCallback?.Invoke(count) ?? $"{count} found";
    }

    protected virtual bool MatchFilter(string filter, in TData data)
    {
        return string.IsNullOrEmpty(filter) || (MatcherCallback?.Invoke(filter, data) ?? false);
    }

    private void SetFilter(string filter)
    {
        if (DataSource == null || MatcherCallback == null)
            return;

        var matchingDataList = new List<TData>();

        foreach (var data in DataSource)
        {
            if (MatchFilter(filter, in data))
                matchingDataList.Add(data);
        }

        SetResults(matchingDataList);

        if (!m_Popup.IsOpened && !m_TemporarilyDontShowPopup)
        {
            m_Popup.Show();
            Popup.ListView.Rebuild();

            int selectedIndex = -1;
            int i = 0;

            // Select the element with the exact match
            foreach (var data in matchingDataList)
            {
                if (!string.IsNullOrEmpty(filter) && filter == GetFilterFromText(GetTextFromData(data)))
                {
                    selectedIndex = i;
                    break;
                }

                i++;
            }

            if (selectedIndex != -1)
            {
                Popup.ListView.selectedIndex = selectedIndex;
                Popup.ListView.ScrollToItem(selectedIndex);
            }
        }
        else
        {
            m_Popup.AdjustGeometry();
        }
    }

    void OnFocusIn(FocusInEvent e)
    {
        if (m_TemporarilyDontShowPopup)
            return;

        m_DataSourceDirty = true;
        EnsurePopupIsCreated();

        if (AlwaysVisible)
        {
            if (AttachedTextField.isReadOnly)
                return;
            UpdateAnchoredControlScreenPosition();
            OnTextChanged();
        }
    }

    void OnFocusOut(FocusOutEvent e)
    {
        m_ScheduledFilterUpdate?.Pause();
        if (UsesRealPopupWindow)
        {
            ScheduleEvaluateFocus();
        }
        else
        {
            m_Popup?.Hide();
        }
    }

    void OnTextValueChange(ChangeEvent<string> e)
    {
        // Do not submit the value change until we evaluate the current focus. See EvaluateFocus.
        if (m_IsEvaluatingFocus)
        {
            PendValueChange(e);
        }
        else
        {
            ClearPendingValueChange();
        }
    }

    /*
     * Schedule the check of the current focus on the next frame
     */
    void ScheduleEvaluateFocus()
    {
        if (m_IsEvaluatingFocus)
            return;

        m_IsEvaluatingFocus = true;
        AttachedTextField.schedule.Execute(EvaluateFocus);
    }

    void CancelEvaluateFocus()
    {
        ClearPendingValueChange();
        m_IsEvaluatingFocus = false;
    }

    /*
     * When the target text field loses focus, we do not submit (by blocking ChangeEvent notification) the new value right away because we want to ensure that
     * the text field did not lose focus because the user click on the completer popup window (e.g user scrolls the results using the scrollbar).
     * If this is what happened then we will submit the pending change only when the popup window will also lose focus unless the focus is given back to the text field or if the user choses a result.
     */
    void EvaluateFocus()
    {
        if (!m_IsEvaluatingFocus)
            return;

        m_IsEvaluatingFocus = false;

        if (AttachedTextField.hasFocus ||
            (m_Popup?.Window != null && m_Popup?.Window == EditorWindow.focusedWindow))
            return;

        SubmitPendingValueChange();
        m_Popup?.Hide();
    }

    void PendValueChange(ChangeEvent<string> e)
    {
        m_HasPendingValueChanged = true;
        m_PendingPreviousValue = e.previousValue;
        e.StopImmediatePropagation();
    }

    void ClearPendingValueChange()
    {
        m_HasPendingValueChanged = false;
        m_PendingPreviousValue = null;
    }

    void SubmitPendingValueChange()
    {
        if (!m_HasPendingValueChanged)
            return;

        try
        {
            using var evt = ChangeEvent<string>.GetPooled(m_PendingPreviousValue, AttachedTextField.value);
            evt.elementTarget = AttachedTextField;
            AttachedTextField.SendEvent(evt);
        }
        finally
        {
            ClearPendingValueChange();
        }
    }

    bool IsNavigationEvent(KeyDownEvent evt)
    {
        switch (evt.keyCode)
        {
            case KeyCode.Tab:
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.Home:
            case KeyCode.End:
                return true;
            default:
                return false;
        }
    }

    void OnKeyDown(KeyDownEvent e)
    {
        if (AttachedTextField.isReadOnly)
            return;

        EnsurePopupIsCreated();
        UpdateAnchoredControlScreenPosition();

        // If the users presses DownArrow key but the popup is not visible then search using the current text
        if (e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Return || e.character == 3 || e.character == '\n')
        {
            var selectedIndex = Popup.ListView.selectedIndex;

            if (selectedIndex != -1)
            {
                AttachedTextField.SetValueWithoutNotify(GetTextFromData(m_Results[selectedIndex]));
                AttachedTextField.Blur();
                m_TemporarilyDontShowPopup = true;
                AttachedTextField.schedule.Execute((e) => m_TemporarilyDontShowPopup = false).ExecuteLater(k_PauseDelay);
                if (UsesRealPopupWindow)
                {
                    m_Popup?.Hide();
                }

                ItemChosen?.Invoke(selectedIndex);
            }
        }

        if (e.keyCode == KeyCode.DownArrow && m_Popup is not { IsOpened: true })
        {
            OnTextChanged();
        }
        // Forward the navigation key event to the list view as it does not have focus
        else if (m_Results != null && m_Results.Count > 0 &&
                 (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow))
        {
            if (e.keyCode == KeyCode.UpArrow && Popup.ListView.selectedIndex == 0)
            {
                Popup.ListView.ClearSelection();
            }
            else if (e.keyCode == KeyCode.DownArrow && Popup.ListView.selectedIndex == -1)
            {
                Popup.ListView.selectedIndex = 0;
            }
            else
            {
                using (var evt = KeyDownEvent.GetPooled(e.character, e.keyCode, e.modifiers))
                {
                    evt.elementTarget = Popup.ListView.scrollView.contentContainer;
                    Popup.ListView.SendEvent(evt);
                }
            }

            e.StopImmediatePropagation();
        }
        else if (!IsNavigationEvent(e))
        {
            ScheduleTextChange();
        }
    }

    EditorWindow GetEditorWindow()
    {
        if (AttachedTextField.elementPanel == null)
        {
            return null;
        }

        return AttachedTextField.elementPanel.ownerObject switch
        {
            EditorWindow editorWindow => editorWindow,
            HostView hostView => hostView.actualView,
            IEditorWindowModel ewm => ewm.window,
            _ => null
        };
    }
}
