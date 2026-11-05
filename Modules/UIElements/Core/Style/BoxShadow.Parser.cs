// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public partial struct BoxShadow
    {
        // Validates `none | [ inset? && <length>{2,4} && <color>? ]` from values added in order, so that USS and UXML
        // accept the same declarations. The lengths are offset-x, offset-y, blur-radius and spread-radius; the inset
        // keyword and the color can come before or after them.
        internal struct Parser
        {
            const int k_MaxLengthCount = 4;

            float m_OffsetX;
            float m_OffsetY;
            float m_BlurRadius;
            float m_SpreadRadius;
            int m_LengthCount;
            bool m_LengthsDone;
            bool m_HasColor;
            Color m_Color;
            bool m_Inset;
            bool m_Invalid;

            // Lengths must be in pixels or unitless
            public void AddLength(float value)
            {
                // The lengths must be contiguous
                if (m_LengthsDone || m_LengthCount == k_MaxLengthCount)
                {
                    m_Invalid = true;
                    return;
                }

                switch (m_LengthCount++)
                {
                    case 0: m_OffsetX = value; break;
                    case 1: m_OffsetY = value; break;
                    case 2: m_BlurRadius = value; break;
                    default: m_SpreadRadius = value; break;
                }
            }

            public void AddInset()
            {
                EndLengths();
                m_Invalid |= m_Inset;
                m_Inset = true;
            }

            public void AddColor(Color color)
            {
                EndLengths();
                m_Invalid |= m_HasColor;
                m_HasColor = true;
                m_Color = color;
            }

            public void AddInvalid()
            {
                m_Invalid = true;
            }

            // False when the declaration is invalid, in which case the shadow is none
            public bool TryGetValue(out BoxShadow boxShadow)
            {
                boxShadow = None();
                if (m_Invalid || m_LengthCount < 2 || m_BlurRadius < 0)
                    return false;

                boxShadow = new BoxShadow
                {
                    // CSS uses currentcolor when the color is omitted; black is used until currentcolor is supported.
                    color = m_HasColor ? m_Color : Color.black,
                    offset = new Vector2(m_OffsetX, m_OffsetY),
                    blurRadius = m_BlurRadius,
                    spreadRadius = m_SpreadRadius,
                    inset = m_Inset
                };
                return true;
            }

            void EndLengths()
            {
                if (m_LengthCount > 0)
                    m_LengthsDone = true;
            }
        }
    }
}
