// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// Base class for labels with fold buttons
/// </summary>
internal abstract class FoldableLabel : VisualElement
{
    internal Foldout m_foldout;
    private Toggle m_toggle;

    protected FoldableLabel(string text, bool isBold = false)
    {
        style.position = Position.Absolute;
        usageHints = UsageHints.DynamicTransform;
        style.flexGrow = 1.0f;
        style.flexDirection = FlexDirection.Row;

        m_foldout = new Foldout();
        m_foldout.text = text;
        m_foldout.value = true; // Expanded by default
        m_foldout.style.fontSize = JobsProfilerSettings.BarTextFontSize;
        if (isBold)
            m_foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
        Add(m_foldout);

        // Cache the toggle element for showing/hiding
        m_toggle = m_foldout.Q<Toggle>();
        SetRowHeight(JobsProfilerSettings.BarHeight);
    }

    internal void SetRowHeight(float height)
    {
        m_foldout.style.height = height;
        m_foldout.style.marginTop = 0.0f;
        m_foldout.style.marginBottom = 0.0f;

        if (m_toggle == null)
            return;

        m_toggle.style.height = height;
        m_toggle.style.minHeight = 0.0f;
        m_toggle.style.marginTop = 0.0f;
        m_toggle.style.marginBottom = 0.0f;
        m_toggle.style.paddingTop = 0.0f;
        m_toggle.style.paddingBottom = 0.0f;
        m_toggle.style.alignItems = Align.Center;

        var input = m_toggle.Q(className: Toggle.inputUssClassName);
        if (input != null)
        {
            input.style.height = height;
            input.style.alignItems = Align.Center;
        }

        var textLabel = m_toggle.Q<Label>();
        if (textLabel != null)
        {
            textLabel.style.marginTop = 0.0f;
            textLabel.style.marginBottom = 0.0f;
            textLabel.style.paddingTop = 0.0f;
            textLabel.style.paddingBottom = 0.0f;
            textLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        }
    }

    internal string text
    {
        get => m_foldout.text;
        set => m_foldout.text = value;
    }

    /// <summary>
    /// Shows or hides the fold toggle. When hidden, only the label text is displayed.
    /// </summary>
    internal void SetFoldable(bool foldable)
    {
        if (m_toggle != null)
        {
            m_toggle.style.display = foldable ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

internal class GroupLabel : FoldableLabel
{
    internal FixedString128Bytes m_groupName;
    internal System.Action<FixedString128Bytes> m_onFoldToggled;
    internal System.Action<FixedString128Bytes, bool> m_onExpandAllRequested;
    bool m_hasMultipleThreads;
    readonly Button m_kebab;

    internal bool hasMultipleThreads
    {
        get => m_hasMultipleThreads;
        set
        {
            m_hasMultipleThreads = value;
            EnableInClassList(k_HasMenuClass, value);
        }
    }

    internal GroupLabel(string text) : base(text, isBold: true)
    {
        // Register callback once - it will use the current m_groupName value
        m_foldout.RegisterValueChangedCallback(evt => m_onFoldToggled?.Invoke(m_groupName));
        m_foldout.style.flexGrow = 1.0f;

        m_kebab = new Button(OnKebabClicked) { tooltip = "Group actions" };
        m_kebab.AddToClassList(k_KebabClass);
        m_kebab.RegisterCallback<MouseUpEvent>(evt => evt.StopImmediatePropagation());

        var icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.AddToClassList(k_KebabIconClass);
        icon.style.backgroundImage = new StyleBackground(EditorGUIUtility.IconContent("_Menu").image as Texture2D);
        m_kebab.Add(icon);
        Add(m_kebab);
    }

    void OnKebabClicked()
    {
        if (!m_hasMultipleThreads)
            return;

        var menu = new GenericDropdownMenu();
        menu.AddItem(k_ExpandAllText, false, ExpandAllWorkers);
        menu.AddItem(k_CollapseAllText, false, CollapseAllWorkers);
        menu.DropDown(m_kebab.worldBound, m_kebab, DropdownMenuSizeMode.Auto);
    }

    void ExpandAllWorkers() => m_onExpandAllRequested?.Invoke(m_groupName, true);

    void CollapseAllWorkers() => m_onExpandAllRequested?.Invoke(m_groupName, false);

    const string k_ExpandAllText = "Expand All Workers";
    const string k_CollapseAllText = "Collapse All Workers";
    const string k_HasMenuClass = "jobs-profiler-group-label--has-menu";
    const string k_KebabClass = "jobs-profiler-kebab";
    const string k_KebabIconClass = "jobs-profiler-kebab__icon";
}

internal class ThreadLabel : FoldableLabel
{
    internal ulong m_threadId;
    internal System.Action<ulong> m_onFoldToggled;

    internal ThreadLabel(string text) : base(text, isBold: false)
    {
        // Register callback once - it will use the current m_threadId value
        m_foldout.RegisterValueChangedCallback(evt => m_onFoldToggled?.Invoke(m_threadId));
    }
}

internal class ThreadLabels : VisualElement
{
    List<ThreadLabel> m_threadLabels;
    List<GroupLabel> m_groupLabels;
    bool m_visible = true;
    float m_rowHeight = JobsProfilerSettings.BarHeight;
    System.Action<ulong> m_onThreadFoldToggled;
    System.Action<FixedString128Bytes> m_onGroupFoldToggled;
    System.Action<FixedString128Bytes, bool> m_onGroupExpandAllRequested;

    internal ThreadLabels()
    {
        m_threadLabels = new List<ThreadLabel>(128);
        m_groupLabels = new List<GroupLabel>(16);

        style.overflow = Overflow.Hidden;
        style.flexGrow = 1.0f;

        generateVisualContent += OnGenerateVisualContent;
    }

    internal void SetThreadFoldCallback(System.Action<ulong> callback)
    {
        m_onThreadFoldToggled = callback;
    }

    internal void SetGroupFoldCallback(System.Action<FixedString128Bytes> callback)
    {
        m_onGroupFoldToggled = callback;
    }

    internal void SetGroupExpandAllCallback(System.Action<FixedString128Bytes, bool> callback)
    {
        m_onGroupExpandAllRequested = callback;
    }

    void OnGroupExpandAllRequested(FixedString128Bytes groupName, bool expand)
    {
        m_onGroupExpandAllRequested?.Invoke(groupName, expand);
    }

    static bool HasMultipleThreads(in FrameData frameData, in FixedString128Bytes groupName)
    {
        foreach (var group in frameData.threadGroups)
        {
            if (group.name == groupName)
                return group.arrayEnd - group.arrayIndex > 1;
        }

        return false;
    }

    /// <summary>
    /// Converts internal group names to display names.
    /// </summary>
    static string GetGroupDisplayName(in FixedString128Bytes name)
    {
        // Convert singular to plural for better readability
        if (name == "Job")
            return "Jobs";
        if (name == "Background Job")
            return "Background Jobs";
        return name.ToString();
    }

    void OnThreadFoldButtonClicked(ulong threadId)
    {
        m_onThreadFoldToggled?.Invoke(threadId);
    }

    void OnGroupFoldButtonClicked(FixedString128Bytes groupName)
    {
        m_onGroupFoldToggled?.Invoke(groupName);
    }

    internal void Hide()
    {
        foreach (var label in m_threadLabels)
            label.style.visibility = Visibility.Hidden;

        foreach (var label in m_groupLabels)
            label.style.visibility = Visibility.Hidden;

        m_visible = false;
    }

    internal void Show()
    {
        m_visible = true;
    }

    internal void DrawSeparators(MeshGenerationContext ctx, float width, float height)
    {
        if (!m_visible)
            return;

        ctx.painter2D.strokeColor = JobsProfilerSettings.ThreadSeparatorLine;
        ctx.painter2D.lineWidth = 1.0f;

        // Draw separator above each group
        for (int i = 0; i < m_groupLabels.Count; i++)
        {
            var label = m_groupLabels[i];
            if (label.style.visibility == Visibility.Hidden)
                continue;

            var pos = label.resolvedStyle.translate;

            if (pos.y < 0.0f || pos.y > height)
                continue;

            float separatorY = pos.y;

            ctx.painter2D.BeginPath();
            ctx.painter2D.MoveTo(new Vector2(0.0f, separatorY));
            ctx.painter2D.LineTo(new Vector2(width, separatorY));
            ctx.painter2D.Stroke();
        }

        // Draw separator above each thread
        foreach (var label in m_threadLabels)
        {
            if (label.style.visibility == Visibility.Hidden)
                continue;

            var pos = label.resolvedStyle.translate;

            if (pos.y < 0.0f || pos.y > height)
                continue;

            ctx.painter2D.BeginPath();
            ctx.painter2D.MoveTo(new Vector2(0.0f, pos.y));
            ctx.painter2D.LineTo(new Vector2(width, pos.y));
            ctx.painter2D.Stroke();
        }
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        DrawSeparators(ctx, resolvedStyle.width, resolvedStyle.height);
    }

    internal void Update(in NativeHashMap<ulong, ThreadPosition> threadOffsets, in FrameData frameData, in NativeArray<ThreadGroupInfo> threadGroupOrder, float4x4 mat, float rowHeight)
    {
        if (!m_visible)
            return;

        bool rowHeightChanged = !Mathf.Approximately(m_rowHeight, rowHeight);
        m_rowHeight = rowHeight;

        float y = resolvedStyle.transformOrigin.y;

        // Build set of thread IDs that are alone in their group (hide their labels)
        HashSet<ulong> singleThreadGroupIds = new HashSet<ulong>();
        foreach (var group in frameData.threadGroups)
        {
            int threadCount = group.arrayEnd - group.arrayIndex;
            if (threadCount == 1)
            {
                singleThreadGroupIds.Add(frameData.threads[group.arrayIndex].threadId);
            }
        }

        // Ensure we have enough group labels
        for (int i = m_groupLabels.Count; i < threadGroupOrder.Length; ++i)
        {
            var label = new GroupLabel("");
            label.SetRowHeight(m_rowHeight);
            label.m_onFoldToggled = OnGroupFoldButtonClicked;
            label.m_onExpandAllRequested = OnGroupExpandAllRequested;
            m_groupLabels.Add(label);
            Add(label);
        }

        // Ensure we have enough thread labels
        for (int i = m_threadLabels.Count; i < frameData.threads.Length; ++i)
        {
            var label = new ThreadLabel("");
            label.SetRowHeight(m_rowHeight);
            label.m_onFoldToggled = OnThreadFoldButtonClicked;
            m_threadLabels.Add(label);
            Add(label);
        }

        if (rowHeightChanged)
        {
            foreach (var label in m_threadLabels)
                label.SetRowHeight(m_rowHeight);
            foreach (var label in m_groupLabels)
                label.SetRowHeight(m_rowHeight);
        }

        int threadLabelIndex = 0;

        // Render thread labels
        foreach (var thread in frameData.threads)
        {
            ThreadPosition threadPos;

            if (threadOffsets.TryGetValue(thread.threadId, out threadPos))
            {
                ThreadLabel label = m_threadLabels[threadLabelIndex];

                // Hide labels for threads in preview mode (folded group compact view)
                // or for single-thread groups (group label is shown instead)
                if (threadPos.visibility == ThreadVisibility.Preview ||
                    singleThreadGroupIds.Contains(thread.threadId))
                {
                    label.style.visibility = Visibility.Hidden;
                    threadLabelIndex++;
                    continue;
                }

                string threadName = thread.name.ToString();
                if (label.text != threadName)
                    label.text = threadName;

                // Update thread ID (callback will use current value)
                label.m_threadId = thread.threadId;

                // Show fold toggle for threads in multi-thread groups
                label.SetFoldable(true);

                // Update fold state (Foldout.value: true = expanded, false = collapsed)
                label.m_foldout.SetValueWithoutNotify(!threadPos.isFolded);

                float3 posTemp = new float3(0.0f, threadPos.offset, 0.0f);
                float3 pos = transform(mat, posTemp);
                // Indent thread labels under their group (tree view style)
                label.style.translate = new StyleTranslate(new Translate(new Length(20.0f), new Length(pos.y), 0.0f));
                label.style.visibility = Visibility.Visible;

                threadLabelIndex++;
            }
        }

        // Render group labels using the pre-calculated group offset
        int groupLabelIndex = 0;
        foreach (var groupInfo in threadGroupOrder)
        {
            GroupLabel groupLabel = m_groupLabels[groupLabelIndex];

            // Update label text (use display name for UI)
            string displayName = GetGroupDisplayName(groupInfo.name);
            if (groupLabel.text != displayName)
                groupLabel.text = displayName;

            // Update group name (callback will use current value)
            groupLabel.m_groupName = groupInfo.name;
            groupLabel.hasMultipleThreads = HasMultipleThreads(frameData, groupInfo.name);

            // Update fold state (Foldout.value: true = expanded, false = collapsed)
            groupLabel.m_foldout.SetValueWithoutNotify(!groupInfo.isFolded);

            // Use the group's stored offset for positioning
            float3 posTemp = new float3(0.0f, groupInfo.offset, 0.0f);
            float3 pos = transform(mat, posTemp);
            groupLabel.style.translate = new StyleTranslate(new Translate(new Length(10.0f), new Length(pos.y), 0.0f));
            groupLabel.style.visibility = Visibility.Visible;

            groupLabelIndex++;
        }

        // Hide unused thread labels
        for (int i = threadLabelIndex; i < m_threadLabels.Count; ++i)
        {
            m_threadLabels[i].style.visibility = Visibility.Hidden;
        }

        // Hide unused group labels
        for (int i = groupLabelIndex; i < m_groupLabels.Count; ++i)
        {
            m_groupLabels[i].style.visibility = Visibility.Hidden;
        }

        MarkDirtyRepaint();
    }

    ThreadGroup FindGroup(in FrameData frame, in FixedString128Bytes name)
    {
        foreach (var group in frame.threadGroups)
        {
            if (group.name == name)
                return group;
        }

        throw new System.ArgumentException("Group not found");
    }
}
