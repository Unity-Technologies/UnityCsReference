// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEditorInternal;

namespace UnityEditor.PlatformSupport
{
    internal class PlatformIconFieldGroup
    {
        internal class IconFieldGroupInfo
        {
            public PlatformIconKind m_Kind;
            public string m_Label;
            public bool m_State;
            public int m_SetIconSlots;
            public int m_IconSlotCount;

            public override bool Equals(object obj)
            {
                return ((IconFieldGroupInfo)obj).m_Label == this.m_Label;
            }

            public override int GetHashCode()
            {
                return this.m_Label.GetHashCode();
            }
        }

        public BuildTargetGroup targetGroup { get; protected set; }
        internal Dictionary<IconFieldGroupInfo, Dictionary<IconFieldGroupInfo, PlatformIconField[]>> m_IconsFields =
            new Dictionary<IconFieldGroupInfo, Dictionary<IconFieldGroupInfo, PlatformIconField[]>>();

        internal Dictionary<PlatformIconKind, PlatformIcon[]> m_PlatformIconsByKind = new Dictionary<PlatformIconKind, PlatformIcon[]>();

        internal PlatformIconFieldGroup(BuildTargetGroup targetGroup)
        {
            this.targetGroup = targetGroup;
        }

        internal PlatformIconField CreatePlatformIconField(PlatformIcon icon)
        {
            if (icon.maxLayerCount > 1)
                return new PlatformIconFieldMultiLayer(icon, targetGroup);

            return new PlatformIconFieldSingleLayer(icon, targetGroup);
        }

        public bool IsEmpty()
        {
            return !(m_IconsFields.Count > 0);
        }

        public void AddPlatformIcons(PlatformIcon[] icons, PlatformIconKind kind)
        {
            m_PlatformIconsByKind[kind] = icons;

            IconFieldGroupInfo kindKey = new IconFieldGroupInfo();
            kindKey.m_Kind = kind;
            kindKey.m_Label = kind.ToString();

            Dictionary<IconFieldGroupInfo, PlatformIconField[]> kindDictionary;

            if (!m_IconsFields.ContainsKey(kindKey))
            {
                kindKey.m_State = false;
                kindDictionary = new Dictionary<IconFieldGroupInfo, PlatformIconField[]>();
            }
            else
            {
                kindDictionary = m_IconsFields[kindKey];
            }

            var groupedBySubKind = icons.GroupBy(i => i.iconSubKind);

            foreach (var subKindGroup in groupedBySubKind)
            {
                var subKindIcons = subKindGroup.ToArray().Select(i => CreatePlatformIconField(i)).ToArray();

                IconFieldGroupInfo subKindKey = new IconFieldGroupInfo();
                subKindKey.m_Kind = null;
                subKindKey.m_Label = subKindGroup.Key;
                subKindKey.m_IconSlotCount = subKindIcons.Length;
                subKindKey.m_SetIconSlots = PlayerSettings.GetNonEmptyPlatformIconCount(subKindGroup.ToArray());

                if (!kindDictionary.ContainsKey(subKindKey))
                    subKindKey.m_State = false;

                kindKey.m_IconSlotCount += subKindKey.m_IconSlotCount;
                kindKey.m_SetIconSlots += subKindKey.m_SetIconSlots;

                kindDictionary[subKindKey] = subKindIcons;
            }

            m_IconsFields[kindKey] = kindDictionary;
        }
    }

    abstract class PlatformIconField
    {
        protected const int kSlotSize = 64;
        protected const float kHeaderHeight = 18;
        protected const int kMaxElementHeight = 116;
        protected const int kMaxPreviewSize = 86;
        protected const int kIconSpacing = 8;

        public PlatformIcon platformIcon { get; protected set; }
        protected BuildTargetGroup m_TargetGroup;
        protected string m_HeaderString;
        protected string m_SizeLabel;

        // TODO move the styles to an uinified location for this & ReorderableList.
        internal ReorderableList.Defaults s_Defaults = new ReorderableList.Defaults();

        internal PlatformIconField(PlatformIcon platformIcon, BuildTargetGroup targetGroup)
        {
            this.platformIcon = platformIcon;
            m_TargetGroup = targetGroup;
            m_HeaderString = this.platformIcon.description;
            m_SizeLabel = string.Format("{0}x{1}px", platformIcon.width, platformIcon.height);
        }

        public static Rect GetContentRect(Rect rect, float paddingVertical = 0, float paddingHorizontal = 0)
        {
            Rect r = rect;

            r.yMin += paddingVertical;
            r.yMax -= paddingVertical;
            r.xMin += paddingHorizontal;
            r.xMax -= paddingHorizontal;
            return r;
        }

        internal abstract void DrawAt();
    }

    class PlatformIconFieldSingleLayer : PlatformIconField
    {
        private bool m_ShowSizeLabel = true;
        private string m_PlatformName;

        internal PlatformIconFieldSingleLayer(PlatformIcon platformIcon, BuildTargetGroup targetGroup) : base(platformIcon, targetGroup)
        {
            m_PlatformName = PlayerSettings.GetPlatformName(m_TargetGroup);
        }

        void DrawHeader(Rect headerRect)
        {
            // draw the background on repaint
            if (Event.current.type == EventType.Repaint)
                s_Defaults.DrawHeaderBackground(headerRect);

            // apply the padding to get the internal rect
            headerRect.xMin += ReorderableList.Defaults.padding;
            headerRect.xMax -= ReorderableList.Defaults.padding;
            headerRect.height -= 2;
            headerRect.y += 1;

            m_ShowSizeLabel = headerRect.width > (EditorGUIUtility.labelWidth  + kSlotSize + kIconSpacing + 24);


            string sizeLabelString = m_HeaderString;
            if (!m_ShowSizeLabel)
                sizeLabelString += string.Format("({0})", m_SizeLabel);

            GUI.Label(headerRect, LocalizationDatabase.GetLocalizedString(sizeLabelString), EditorStyles.label);
        }

        void DrawElement(Rect elementRect)
        {
            int slotWidth = kMaxPreviewSize;
            int slotHeight = (int)((float)platformIcon.height / platformIcon.height * slotWidth);  // take into account the aspect ratio
            int previewWidth = Mathf.Min(slotWidth, platformIcon.width);
            int previewHeight = (int)((float)platformIcon.height * previewWidth / platformIcon.width);  // take into account the aspect ratio

            if (Event.current.type == EventType.Repaint)
                s_Defaults.boxBackground.Draw(elementRect, false, false, false, false);

            float width = Mathf.Min(elementRect.width, EditorGUIUtility.labelWidth + 4 + kSlotSize + kIconSpacing + kMaxPreviewSize);
            Rect elementContentRect = GetContentRect(elementRect, 6f, 12f);
            Rect textureRect =
                new Rect(elementContentRect.x + elementContentRect.width - kMaxPreviewSize - slotWidth - kIconSpacing,
                    elementContentRect.y, slotWidth, slotHeight);

            Texture2D texture = (Texture2D)EditorGUI.ObjectField(
                    textureRect,
                    platformIcon.GetTexture(0),
                    typeof(Texture2D),
                    false);

            platformIcon.SetTexture(texture, 0);

            // Preview
            Rect previewRect = new Rect(elementContentRect.x + elementContentRect.width - kMaxPreviewSize, elementContentRect.y, previewWidth, previewHeight);

            GUI.Box(previewRect, "");
            Texture2D closestIcon = PlayerSettings.GetPlatformIconAtSize(m_PlatformName, platformIcon.width, platformIcon.height, platformIcon.kind.kind, platformIcon.iconSubKind);
            if (closestIcon != null)
                GUI.DrawTexture(GetContentRect(previewRect, 1, 1), closestIcon);

            //Do not show size label if there is not enough space for it in the body.
            if (m_ShowSizeLabel)
            {
                GUI.Label(new Rect(elementContentRect.x, elementContentRect.y, width - kSlotSize - kIconSpacing, 20),
                    m_SizeLabel);
            }
        }

        internal override void DrawAt()
        {
            Rect rect = GUILayoutUtility.GetRect(0, kMaxElementHeight + kIconSpacing, GUILayout.ExpandWidth(true));

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, kHeaderHeight);
            Rect elementRect = new Rect(rect.x, rect.y + kHeaderHeight, rect.width, rect.height - kHeaderHeight - kIconSpacing);

            DrawHeader(headerRect);
            DrawElement(elementRect);
        }
    }

    class PlatformIconFieldMultiLayer : PlatformIconField
    {
        ReorderableIconLayerList m_IconLayers;

        void EnsureMinimumNumberOfTextures()
        {
            while (m_IconLayers.textures.Count < m_IconLayers.minItems)
                m_IconLayers.textures.Add(null);
        }

        internal PlatformIconFieldMultiLayer(PlatformIcon platformIcon, BuildTargetGroup targetGroup) : base(platformIcon, targetGroup)
        {
            bool showControls = this.platformIcon.minLayerCount != this.platformIcon.maxLayerCount;

            m_IconLayers = new ReorderableIconLayerList(showControls: showControls);
            m_IconLayers.headerString = string.Format("{0} ({1})", this.platformIcon.description, m_SizeLabel);
            m_IconLayers.minItems = this.platformIcon.minLayerCount;
            m_IconLayers.maxItems = this.platformIcon.maxLayerCount;

            string[] customLayerLabels = platformIcon.kind.customLayerLabels;
            if (customLayerLabels != null && customLayerLabels.Length > 0)
                m_IconLayers.SetElementLabels(platformIcon.kind.customLayerLabels);

            int slotWidth = kMaxPreviewSize;
            int slotHeight = (int)((float)platformIcon.height / platformIcon.height * slotWidth);
            m_IconLayers.SetImageSize(slotWidth, slotHeight);

            m_IconLayers.textures = platformIcon.GetTextures().ToList();
            EnsureMinimumNumberOfTextures();
        }

        internal override void DrawAt()
        {
            m_IconLayers.textures = platformIcon.GetTextures().ToList();
            m_IconLayers.previewTextures =  platformIcon.GetPreviewTextures().ToList();
            EnsureMinimumNumberOfTextures();

            m_IconLayers.DoLayoutList();

            platformIcon.SetTextures(m_IconLayers.textures.ToArray());
        }
    }
}
