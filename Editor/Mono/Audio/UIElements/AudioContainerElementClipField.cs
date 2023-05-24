// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.Audio.UIElements;

class AudioContainerElementClipField : ObjectField
{
    [Preserve]
    public new class UxmlFactory : UxmlFactory<AudioContainerElementClipField, UxmlTraits> { }

    [Preserve]
    public new class UxmlTraits : ObjectField.UxmlTraits { }
    public int AssetElementInstanceID { get; set; }
    public double Progress
    {
        get => m_Progress;

        set
        {
            m_Progress = value;
            MarkDirtyRepaint();
        }
    }

    float m_ObjectFieldBorderWidth;
    float m_ObjectSelectorWidth;
    Color m_ProgressBarColor;
    Color m_ProgressBarBackgroundColor;
    double m_Progress;

    static readonly CustomStyleProperty<Color> s_ProgressBarColorProperty = new("--progress-bar-color");
    static readonly CustomStyleProperty<Color> s_ProgressBarBackgroundColorProperty = new("--progress-bar-background");

    public AudioContainerElementClipField()
    {
        generateVisualContent += GenerateVisualContent;
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);
    }

    static void CustomStylesResolved(CustomStyleResolvedEvent evt)
    {
        AudioContainerElementClipField element = (AudioContainerElementClipField)evt.currentTarget;
        element.UpdateCustomStyles();
    }

    void UpdateCustomStyles()
    {
        if (customStyle.TryGetValue(s_ProgressBarColorProperty, out var progressColor))
        {
            m_ProgressBarColor = progressColor;
        }

        if (customStyle.TryGetValue(s_ProgressBarBackgroundColorProperty, out var progressBackgroundColor))
        {
            m_ProgressBarBackgroundColor = progressBackgroundColor;
        }
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
            CacheChildSettings();
            MarkDirtyRepaint();
    }

    void CacheChildSettings()
    {
        var objField = UIToolkitUtilities.GetChildByClassName<VisualElement>(this, "unity-object-field__input");
        var col = objField.resolvedStyle.backgroundColor;
        col.a = 0;
        objField.style.backgroundColor = col;
        m_ObjectFieldBorderWidth = objField.resolvedStyle.borderBottomWidth;

        var selector = UIToolkitUtilities.GetChildByClassName<VisualElement>(this, "unity-object-field__selector");
        m_ObjectSelectorWidth = selector.boundingBox.width;
    }

    static void GenerateVisualContent(MeshGenerationContext context)
    {
        var element = context.visualElement as AudioContainerElementClipField;
        var painter2D = context.painter2D;
        var rect = context.visualElement.contentRect;
        rect.yMax -= element.m_ObjectFieldBorderWidth;
        rect.yMin += element.m_ObjectFieldBorderWidth;
        rect.xMin += element.m_ObjectFieldBorderWidth;

        var progress = element.m_Progress;
        var progressMaxX = (float)(((rect.xMax - element.m_ObjectSelectorWidth) - rect.xMin) * progress);
        painter2D.fillColor = element.m_ProgressBarColor;
        painter2D.BeginPath();
        painter2D.MoveTo(new Vector2(rect.xMin, rect.yMin));
        painter2D.LineTo(new Vector2(progressMaxX, rect.yMin));
        painter2D.LineTo(new Vector2(progressMaxX, rect.yMax));
        painter2D.LineTo(new Vector2(rect.xMin, rect.yMax));
        painter2D.ClosePath();
        painter2D.Fill();

        // redraw background
        painter2D.fillColor = element.m_ProgressBarBackgroundColor;
        painter2D.BeginPath();
        painter2D.MoveTo(new Vector2(progressMaxX, rect.yMin));
        painter2D.LineTo(new Vector2(rect.xMax, rect.yMin));
        painter2D.LineTo(new Vector2(rect.xMax, rect.yMax));
        painter2D.LineTo(new Vector2(progressMaxX, rect.yMax));
        painter2D.ClosePath();
        painter2D.Fill();

    }
}
