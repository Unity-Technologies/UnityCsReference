// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UIElementButton = UnityEngine.Experimental.UIElements.Button;

namespace UnityEditor
{
    internal partial class SpritePolygonModeModule : SpriteFrameModuleBase
    {
        private static class SpritePolygonModeStyles
        {
            public static readonly GUIContent changeShapeLabel = EditorGUIUtility.TrTextContent("Change Shape");
        }

        private VisualElement m_PolygonShapeView;
        private UIElementButton m_ChangeButton;
        private VisualElement m_WarningMessage;

        // overrides for SpriteFrameModuleViewBase
        private void AddMainUI(VisualElement element)
        {
            var visualTree = EditorGUIUtility.Load("UXML/SpriteEditor/PolygonChangeShapeWindow.uxml") as VisualTreeAsset;
            m_PolygonShapeView = visualTree.CloneTree(null).Q<VisualElement>("polygonShapeWindow");
            m_PolygonShapeView.RegisterCallback<MouseDownEvent>((e) => { e.StopPropagation(); });
            m_PolygonShapeView.RegisterCallback<MouseUpEvent>((e) => { e.StopPropagation(); });
            SetupPolygonChangeShapeWindowElements(m_PolygonShapeView);
            element.Add(m_PolygonShapeView);
        }

        public override void DoMainGUI()
        {
            base.DoMainGUI();
            DrawGizmos();

            HandleGizmoMode();

            HandleBorderCornerScalingHandles();
            HandleBorderSidePointScalingSliders();

            HandleBorderSideScalingHandles();
            HandlePivotHandle();

            if (!MouseOnTopOfInspector())
                spriteEditor.HandleSpriteSelection();
        }

        public override void DoToolbarGUI(Rect toolbarRect)
        {
            using (new EditorGUI.DisabledScope(spriteEditor.editingDisabled))
            {
                GUIStyle skin = EditorStyles.toolbarPopup;
                Rect drawArea = toolbarRect;
                drawArea.width = skin.CalcSize(SpritePolygonModeStyles.changeShapeLabel).x;
                SpriteUtilityWindow.DrawToolBarWidget(ref drawArea, ref toolbarRect, (adjustedDrawArea) =>
                {
                    showChangeShapeWindow = GUI.Toggle(adjustedDrawArea, showChangeShapeWindow, SpritePolygonModeStyles.changeShapeLabel, EditorStyles.toolbarButton);
                });
            }
        }

        private void DrawGizmos()
        {
            if (eventSystem.current.type != EventType.Repaint)
                return;
            for (int i = 0; i < spriteCount; i++)
            {
                List<Vector2[]> outline = GetSpriteOutlineAt(i);
                Vector2 offset = GetSpriteRectAt(i).size * 0.5f;
                if (outline.Count > 0)
                {
                    SpriteEditorUtility.BeginLines(new Color(0.75f, 0.75f, 0.75f, 0.75f));
                    for (int j = 0; j < outline.Count; ++j)
                    {
                        for (int k = 0, last = outline[j].Length - 1; k < outline[j].Length; last = k, ++k)
                            SpriteEditorUtility.DrawLine(outline[j][last] + offset, outline[j][k] + offset);
                    }
                    SpriteEditorUtility.EndLines();
                }
            }
            DrawSpriteRectGizmos();
        }

        private void ViewUpdateSideCountField()
        {
            var sidesField = m_PolygonShapeView.Q<PropertyControl<long>>("labelIntegerField");
            sidesField.value = polygonSides;
        }

        private void SetupPolygonChangeShapeWindowElements(VisualElement moduleView)
        {
            var sidesField = moduleView.Q<PropertyControl<long>>("labelIntegerField");
            sidesField.SetValueWithoutNotify(polygonSides);
            sidesField.OnValueChanged((evt) =>
            {
                polygonSides = (int)evt.newValue;
                ShowHideWarningMessage();
            });
            m_ChangeButton = moduleView.Q<UIElementButton>("changeButton");
            m_ChangeButton.RegisterCallback<MouseUpEvent>((e) =>
            {
                if (isSidesValid)
                {
                    GeneratePolygonOutline();
                    showChangeShapeWindow = false;
                }
            });
            m_WarningMessage = moduleView.Q("warning");
            ShowHideWarningMessage();
        }

        void ShowHideWarningMessage()
        {
            m_WarningMessage.visible = !isSidesValid;
            m_WarningMessage.style.positionType = m_WarningMessage.visible ? PositionType.Relative : PositionType.Absolute;

            m_ChangeButton.visible = isSidesValid;
            m_ChangeButton.style.positionType = m_ChangeButton.visible ? PositionType.Relative : PositionType.Absolute;
        }

        private bool isSidesValid
        {
            get
            {
                return polygonSides == 0 || (polygonSides >= 3 && polygonSides <= 128);
            }
        }

        public bool showChangeShapeWindow
        {
            get { return m_PolygonShapeView.visible; }
            set
            {
                if (m_PolygonShapeView.visible == value)
                    return;
                m_PolygonShapeView.visible = value;
            }
        }
    }
}
