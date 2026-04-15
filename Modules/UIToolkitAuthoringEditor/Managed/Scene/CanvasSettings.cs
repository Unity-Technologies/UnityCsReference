// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace Unity.UIToolkit.Editor;

sealed class CanvasSettings : UISelectionObject
{
    public delegate void CanvasSettingsChangedDelegate(CanvasSettings settings, Vector2 canvasSize, Vector2 offset, float zoomFactor);
    public delegate void CanvasBackgroundSettingsChangedDelegate(CanvasSettings settings);

    public static readonly Vector2 DefaultSize = new(480, 640);
    public static readonly Vector2 DefaultOffset = new(0, 0);
    public static readonly float DefaultZoomFactor = 1.0f;
    public static readonly CanvasBackgroundType DefaultBackgroundType = CanvasBackgroundType.Checkerboard;
    public static readonly Color DefaultBackgroundColor = Color.black;
    public static readonly float DefaultBackgroundOpacity = 1.0f;
    public static readonly Texture2D DefaultBackgroundImage = null;
    public static readonly ScaleMode DefaultScaleMode = ScaleMode.StretchToFill;

    [SerializeField] Vector2 m_CanvasSize = DefaultSize;
    [SerializeField] Vector2 m_Offset = DefaultOffset;
    [SerializeField] float m_ZoomFactor = DefaultZoomFactor;
    [SerializeField] CanvasBackgroundType m_BackgroundType = DefaultBackgroundType;
    [SerializeField] Color m_BackgroundColor = DefaultBackgroundColor;
    [SerializeField] float m_BackgroundColorOpacity = DefaultBackgroundOpacity;
    [SerializeField] Texture2D m_BackgroundImage = DefaultBackgroundImage;
    [SerializeField] float m_BackgroundImageOpacity = DefaultBackgroundOpacity;
    [SerializeField] ScaleMode m_BackgroundImageScaleMode = DefaultScaleMode;

    public event CanvasSettingsChangedDelegate CanvasSettingsChanged;
    public event CanvasBackgroundSettingsChangedDelegate CanvasBackgroundChanged;

    public Vector2 CanvasSize
    {
        get => m_CanvasSize;
        set
        {
            if (m_CanvasSize == value)
                return;

            m_CanvasSize = value;
            EnsureMinCanvasSize();
            SendCanvasChangedEvent();
        }
    }

    public Vector2 ScaledSize => CanvasSize * ZoomFactor;

    public Vector2 Offset
    {
        get => m_Offset;
        set
        {
            if (m_Offset == value)
                return;
            m_Offset = value;
            SendCanvasChangedEvent();
        }
    }

    public float ZoomFactor
    {
        get => m_ZoomFactor;
        set
        {
            if (Mathf.Approximately(m_ZoomFactor, value))
                return;
            m_ZoomFactor = value;
            EnsureMinZoomFactor();
            SendCanvasChangedEvent();
        }
    }

    public CanvasBackgroundType BackgroundType
    {
        get => m_BackgroundType;
        set
        {
            if (m_BackgroundType == value)
                return;
            m_BackgroundType = value;
            SendCanvasBackgroundTypeChangedEvent();
        }
    }

    public Color BackgroundColor
    {
        get => m_BackgroundColor;
        set
        {
            if (m_BackgroundColor == value)
                return;
            m_BackgroundColor = value;
            SendCanvasBackgroundTypeChangedEvent();
        }
    }

    public float BackgroundColorOpacity
    {
        get => m_BackgroundColorOpacity;
        set
        {
            if (Mathf.Approximately(m_BackgroundColorOpacity, value))
                return;
            m_BackgroundColorOpacity = value;
            SendCanvasBackgroundTypeChangedEvent();
        }
    }

    public Texture2D BackgroundImage
    {
        get => m_BackgroundImage;
        set
        {
            if (m_BackgroundImage == value)
                return;
            m_BackgroundImage = value;
            SendCanvasBackgroundTypeChangedEvent();
        }
    }

    public float BackgroundImageOpacity
    {
        get => m_BackgroundImageOpacity;
        set
        {
            if (Mathf.Approximately(m_BackgroundImageOpacity, value))
                return;
            m_BackgroundImageOpacity = value;
            SendCanvasBackgroundTypeChangedEvent();
        }
    }

    public ScaleMode BackgroundImageScaleMode
    {
        get => m_BackgroundImageScaleMode;
        set
        {
            if (m_BackgroundImageScaleMode == value)
                return;
            m_BackgroundImageScaleMode = value;
            SendCanvasBackgroundTypeChangedEvent();
        }
    }

    public void LoadStorage(string storageKey)
    {
        var storage = EditorUserSettings.GetConfigValue(storageKey);
        if (!string.IsNullOrEmpty(storage))
            JsonUtility.FromJsonOverwrite(storage, this);
        else
            ResetToDefaultValues();
    }

    public void SerializeToStorage(string storageKey)
    {
        EditorUserSettings.SetConfigValue(storageKey, JsonUtility.ToJson(this));
    }

    void ResetToDefaultValues()
    {
        CanvasSize = DefaultSize;
        Offset = DefaultOffset;
        ZoomFactor = DefaultZoomFactor;
        BackgroundType = DefaultBackgroundType;
        BackgroundColor = DefaultBackgroundColor;
        BackgroundColorOpacity = DefaultBackgroundOpacity;
        BackgroundImage = DefaultBackgroundImage;
        BackgroundImageOpacity = DefaultBackgroundOpacity;
        BackgroundImageScaleMode = DefaultScaleMode;
    }

    internal void SendCanvasChangedEvent()
    {
        CanvasSettingsChanged?.Invoke(this, CanvasSize, Offset, ZoomFactor);
    }

    internal void SendCanvasBackgroundTypeChangedEvent()
    {
        CanvasBackgroundChanged?.Invoke(this);
    }

    void OnValidate()
    {
        EnsureMinCanvasSize();
        EnsureMinZoomFactor();
        SendCanvasChangedEvent();
        SendCanvasBackgroundTypeChangedEvent();
    }

    void EnsureMinCanvasSize()
    {
        if (m_CanvasSize.x < 1.0f)
            m_CanvasSize.x = 1.0f;
        if (m_CanvasSize.y < 1.0f)
            m_CanvasSize.y = 1.0f;
    }

    void EnsureMinZoomFactor()
    {
        if (m_ZoomFactor <= 0.0f)
            m_ZoomFactor = 0.01f;
    }
}
