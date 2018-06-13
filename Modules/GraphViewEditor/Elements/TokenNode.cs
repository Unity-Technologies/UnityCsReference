// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class TokenNode : Node
    {
        private Pill m_Pill;

        public Texture icon
        {
            get { return m_Pill.icon; }
            set { m_Pill.icon = value; }
        }

        public Port input
        {
            get
            {
                return m_Pill.left as Port;
            }
        }

        public Port output
        {
            get
            {
                return m_Pill.right as Port;
            }
        }

        public TokenNode(Port input, Port output) : base("UXML/GraphView/TokenNode.uxml")
        {
            AddStyleSheetPath("StyleSheets/GraphView/TokenNode.uss");

            m_Pill = this.Q<Pill>(name: "pill");

            if (input != null)
            {
                m_Pill.left = input;
            }

            if (output != null)
            {
                m_Pill.right = output;
            }

            ClearClassList();
            AddToClassList("token-node");
        }

        public bool highlighted
        {
            get { return m_Pill.highlighted; }
            set { m_Pill.highlighted = value; }
        }
    }
}
