// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    // Setup animation, tracks animation, fires callback when done (fully expanded/collapsed)
    //

    internal class TreeViewItemExpansionAnimator
    {
        TreeViewAnimationInput m_Setup; // when null we are not animating
        bool m_InsideGUIClip;
        Rect m_CurrentClipRect;
        static bool s_Debug = false;

        public void BeginAnimating(TreeViewAnimationInput setup)
        {
            if (m_Setup != null)
            {
                if (m_Setup.item.id == setup.item.id && m_Setup.expanding != setup.expanding)
                {
                    // If same item (changed expand/collapse while animating) then just change direction, but skip the time that already passed
                    if (m_Setup.elapsedTime >= 0)
                    {
                        setup.elapsedTime = m_Setup.animationDuration - m_Setup.elapsedTime;
                    }
                    else
                        Debug.LogError("Invalid duration " + m_Setup.elapsedTime);

                    m_Setup = setup;
                }
                else
                {
                    // Ensure current animation ends before starting a new (just finish it immediately)
                    SkipAnimating();
                    m_Setup = setup;
                }

                m_Setup.expanding = setup.expanding;
            }

            m_Setup = setup;
            if (m_Setup == null)
                Debug.LogError("Setup is null");

            if (printDebug)
                Console.WriteLine("Begin animating: " + m_Setup);

            m_CurrentClipRect = GetCurrentClippingRect();
        }

        public void SkipAnimating()
        {
            if (m_Setup != null)
            {
                m_Setup.FireAnimationEndedEvent();
                m_Setup = null;
            }
        }

        // Returns true if row should be culled
        public bool CullRow(int row, ITreeViewGUI gui)
        {
            if (!isAnimating)
            {
                return false;
            }

            if (printDebug && row == 0)
                Console.WriteLine("--------");

            // Check rows that are inside animation clip rect if they can be culled
            if (row > m_Setup.startRow && row <= m_Setup.endRow)
            {
                Rect rowRect = gui.GetRowRect(row, 1); // we do not care about the width

                // Check row Y local to clipRect
                float rowY = rowRect.y - m_Setup.startRowRect.y;

                if (rowY > m_CurrentClipRect.height)
                {
                    // Ensure to end animation clip since items after
                    // culling should be rendered normally
                    if (m_InsideGUIClip)
                    {
                        EndClip();
                    }

                    return true;
                }
            }

            // Row is not culled
            return false;
        }

        public void OnRowGUI(int row)
        {
            if (printDebug)
                Console.WriteLine(row + " Do item " + DebugItemName(row));
        }

        // Call before of TreeViewGUI's OnRowGUI (Needs to be called for all items (changes rects for rows comming after the animating rows)
        public Rect OnBeginRowGUI(int row, Rect rowRect)
        {
            if (!isAnimating)
                return rowRect;

            if (row == m_Setup.startRow)
            {
                BeginClip();
            }

            // Make row rect local to guiclip if animating
            if (row >= m_Setup.startRow && row <= m_Setup.endRow)
            {
                rowRect.y -= m_Setup.startRowRect.y;
            }
            // rows following the animation snap to cliprect bottom
            else if (row > m_Setup.endRow)
            {
                rowRect.y -= m_Setup.rowsRect.height - m_CurrentClipRect.height;
            }

            return rowRect;
        }

        // Call at the after TreeViewGUI's OnRowGUI
        public void OnEndRowGUI(int row)
        {
            if (!isAnimating)
                return;

            if (m_InsideGUIClip && row == m_Setup.endRow)
            {
                EndClip();
            }
        }

        // Call before all items are being handling


        private void BeginClip()
        {
            GUI.BeginClip(m_CurrentClipRect);
            m_InsideGUIClip = true;
            if (printDebug)
                Console.WriteLine("BeginClip startRow: " + m_Setup.startRow);
        }

        private void EndClip()
        {
            GUI.EndClip();
            m_InsideGUIClip = false;
            if (printDebug)
                Console.WriteLine("EndClip endRow: " + m_Setup.endRow);
        }

        public void OnBeforeAllRowsGUI()
        {
            if (!isAnimating)
                return;

            // Cache to ensure consistent across all rows (it is dependant on time)
            m_CurrentClipRect = GetCurrentClippingRect();

            // Stop animation when duration has passed
            if (m_Setup.elapsedTime > m_Setup.animationDuration)
            {
                m_Setup.FireAnimationEndedEvent();
                m_Setup = null;

                if (printDebug)
                    Debug.Log("Animation ended");
            }
        }

        public void OnAfterAllRowsGUI()
        {
            // Ensure to end clip if not done in CullRow (while iterating rows)
            if (m_InsideGUIClip)
            {
                EndClip();
            }

            if (isAnimating)
                HandleUtility.Repaint();

            // Capture time at intervals to ensure that expansion value is consistent across layout and repaint.
            // This fixes that the scroll view showed its scrollbars during expansion since using realtime
            // would give higher values on repaint than on layout event.
            if (isAnimating && Event.current.type == EventType.Repaint)
                m_Setup.CaptureTime();
        }

        public bool IsAnimating(int itemID)
        {
            if (!isAnimating)
                return false;

            return m_Setup.item.id == itemID;
        }

        // 1 fully expanded, 0 fully collapsed
        public float expandedValueNormalized
        {
            get
            {
                float frac = m_Setup.elapsedTimeNormalized;
                return m_Setup.expanding ? frac : (1.0f - frac);
            }
        }

        public int startRow
        {
            get { return m_Setup.startRow; }
        }

        public int endRow
        {
            get { return m_Setup.endRow; }
        }

        public float deltaHeight
        {
            get { return Mathf.Floor(m_Setup.rowsRect.height - m_Setup.rowsRect.height * expandedValueNormalized); }
        }

        public bool isAnimating
        {
            get { return m_Setup != null; }
        }

        public bool isExpanding
        {
            get { return m_Setup.expanding; }
        }

        Rect GetCurrentClippingRect()
        {
            Rect rect = m_Setup.rowsRect;
            rect.height *= expandedValueNormalized;
            return rect;
        }

        bool printDebug
        {
            get { return s_Debug && (m_Setup != null) && (m_Setup.treeView != null) && (Event.current.type == EventType.Repaint); }
        }

        string DebugItemName(int row)
        {
            return m_Setup.treeView.data.GetRows()[row].displayName;
        }
    }

    internal class TreeViewAnimationInput
    {
        public TreeViewAnimationInput()
        {
            startTime = timeCaptured = EditorApplication.timeSinceStartup;
        }

        public void CaptureTime()
        {
            timeCaptured = EditorApplication.timeSinceStartup;
        }

        public float elapsedTimeNormalized
        {
            get
            {
                return Mathf.Clamp01((float)elapsedTime / (float)animationDuration);
            }
        }

        public double elapsedTime
        {
            get
            {
                return timeCaptured - startTime;
            }

            set
            {
                startTime = timeCaptured - value;
            }
        }

        public int startRow { get; set; }
        public int endRow { get; set; }
        public Rect rowsRect {get; set; } // the rect encapsulating startrow and endrow

        public Rect startRowRect {get; set; }
        public double startTime { get; set; }
        public double timeCaptured { get; set; }
        public double animationDuration { get; set; }
        public bool expanding { get; set; }
        public bool includeChildren { get; set; }
        public TreeViewItem item { get; set; }
        public TreeViewController treeView { get; set; }

        public System.Action<TreeViewAnimationInput> animationEnded; // set to get a callback when animation ends

        public void FireAnimationEndedEvent()
        {
            if (animationEnded != null)
                animationEnded(this);
        }

        public override string ToString()
        {
            return "Input: startRow " + startRow + " endRow " + endRow + " rowsRect " + rowsRect + " startTime " + startTime + " anitmationDuration" + animationDuration + " " + expanding + " " + item.displayName;
        }
    }
} // UnityEditor
