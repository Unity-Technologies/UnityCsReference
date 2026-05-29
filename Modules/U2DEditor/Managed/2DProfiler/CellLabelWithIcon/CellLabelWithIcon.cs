// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    [UxmlElement]
    partial class CellLabelWithIcon:VisualElement
    {
        const string k_Uxml = "U2DEditor/SpriteAtlasProfiler/CellLabelWithIcon/CellLabelWithIcon.uxml";

        Label m_Label;
        VisualElement m_Icon;
        public CellLabelWithIcon()
        {
            (EditorGUIUtility.Load(k_Uxml) as VisualTreeAsset).CloneTree(this);
            m_Label = this.Q<Label>("Label");
            m_Icon = this.Q<VisualElement>("Icon");
        }

        public void BindLabel(DataBinding binding)
        {
            if (binding != null)
                m_Label.SetBinding("text", binding);
        }

        public void SetLabelDataSoruce(object source)
        {
            m_Label.dataSource  = source;
        }

        public void SetIconClassName(string iconClassName)
        {
            m_Icon.AddToClassList(iconClassName);
        }

        public void RemoveIconClassName(string iconClassName)
        {
            m_Icon.RemoveFromClassList(iconClassName);
        }
    }
}
