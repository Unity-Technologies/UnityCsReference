// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.UIAutomation
{
    class AutomatedWindow<T> : IElementFinder, IDisposable where T : EditorWindow
    {
        private readonly T m_EditorWindow;
        private readonly IMModel m_Model = new IMModel();

        //mouseVisualization
        private Vector2 m_LastMousePosition = new Vector2(-1, -1);
        private float m_MouseMoveSpeed = 100f; //points/s

        private float m_LastEventSent = -1f;

        private bool m_Disposed;
        private bool m_DeveloperBuild;

        public T window { get { return m_EditorWindow; } }

        static AutomatedWindow<T> GetAutomatedWindow()
        {
            T window = EditorWindow.GetWindow<T>();
            return new AutomatedWindow<T>(window);
        }

        //Make sure the events can be visualized
        public bool isAutomationVisible
        {
            set; get;
        }

        public AutomatedWindow(T windowToDrive)
        {
            m_EditorWindow = windowToDrive;
            //TODO: find a way to properly pass this data from the runner here.
            isAutomationVisible = EditorPrefs.GetBool("UnityEdior.PlaymodeTestsRunnerWindow" + ".visualizeUIAutomationEvents", false);

            //TODO: at the moment you can only debug one GUIView at a time, and nothing prevents from someone else to change who's being debugged.
            //figure out a way to handle this.
            GUIViewDebuggerHelper.DebugWindow(m_EditorWindow.m_Parent);
            GUIViewDebuggerHelper.onViewInstructionsChanged += m_Model.ViewContentsChanged;
            m_EditorWindow.RepaintImmediately(); //will create all the instructions, and will trigger onViewInstructionsChanged

            m_Disposed = false;
            m_DeveloperBuild = Unsupported.IsDeveloperMode();
        }

        ~AutomatedWindow()
        {
            if (!m_Disposed && m_DeveloperBuild)
                Debug.LogWarningFormat("{0} instance finalized without being disposed. Create it inside a using block or manually call Dispose () when done.", this.GetType());
            Dispose();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            GUIViewDebuggerHelper.onViewInstructionsChanged -= m_Model.ViewContentsChanged;
            GUIViewDebuggerHelper.StopDebugging();
            m_Disposed = true;
        }

        public void Click(Vector2 editorWindowPosition, EventModifiers modifiers = EventModifiers.None)
        {
            var GUIViewPosition = EventUtility.ConvertEditorWindowCoordsToGuiViewCoords(editorWindowPosition);
            HandleMouseAutomationVisibility(GUIViewPosition);
            if (EventUtility.Click(window, GUIViewPosition, modifiers))
                window.RepaintImmediately();
        }

        public void Click(IAutomatedUIElement element, EventModifiers modifiers = EventModifiers.None)
        {
            if (!element.hasRect)
            {
                Debug.LogWarning("Cannot click on an element that has no rect");
                return;
            }

            Click(EventUtility.ConvertGuiViewCoordsToEditorWindowCoords(element.rect.center), modifiers);
        }

        //TODO: this will lock up the Editor while 'moving the mouse'.
        //We need to decide if this is okay, or not.
        //This is responsible for showing the mouse moving between mouse events.
        private void HandleMouseAutomationVisibility(Vector2 desiredMousePosition, bool sendMouseDrag = false)
        {
            if (!isAutomationVisible)
                return;

            if (m_LastMousePosition != desiredMousePosition)
            {
                float startTime = (float)EditorApplication.timeSinceStartup;
                float endTime = startTime + ((m_LastMousePosition - desiredMousePosition).magnitude / m_MouseMoveSpeed);

                var mouseStart = m_LastMousePosition;

                while (m_LastMousePosition != desiredMousePosition)
                {
                    float curtime = (float)EditorApplication.timeSinceStartup;
                    float frac = Mathf.Clamp01((curtime - startTime) / (endTime - startTime));

                    frac = Easing.Quadratic.InOut(frac);
                    var mousePosition = Vector2.Lerp(mouseStart, desiredMousePosition, frac);

                    //We currently send mouse moves to the window when visualizing the interactions,
                    //But this might create different behaviour when visualizing or not.
                    //This is also true for the Repaint event from RepaintImmediately.
                    //TODO: Decide what to do about it.
                    EventUtility.UpdateMouseMove(window, mousePosition);

                    if (sendMouseDrag)
                        EventUtility.UpdateDragAndDrop(window, mousePosition);

                    m_LastMousePosition = mousePosition;

                    window.RepaintImmediately();

                    System.Threading.Thread.Sleep(16);
                }
            }

            HandleLastEventPauseVisibility();
        }

        private void HandleLastEventPauseVisibility(float minTimeBetweenEvents = .5f)
        {
            if (!isAutomationVisible)
                return;

            float now = (float)EditorApplication.timeSinceStartup;
            var deltaTime = now - m_LastEventSent;
            if (deltaTime < minTimeBetweenEvents)
            {
                int remainderTime = Mathf.CeilToInt((minTimeBetweenEvents - deltaTime) * 100f);
                System.Threading.Thread.Sleep(remainderTime);
            }
            m_LastEventSent = (float)EditorApplication.timeSinceStartup;
        }

        public void DragAndDrop(Vector2 start, Vector2 end, EventModifiers modifiers = EventModifiers.None)
        {
            BeginDrag(start, modifiers);

            UpdateDragAndDrop(Vector2.Lerp(start, end, 0.5f));

            EndDrop(end, modifiers);
        }

        public void BeginDrag(Vector2 start, EventModifiers modifiers = EventModifiers.None)
        {
            start = EventUtility.ConvertEditorWindowCoordsToGuiViewCoords(start);
            HandleMouseAutomationVisibility(start);
            if (EventUtility.BeginDragAndDrop(window, start, modifiers))
                window.RepaintImmediately();
        }

        public void UpdateDragAndDrop(Vector2 pos, EventModifiers modifiers = EventModifiers.None)
        {
            pos = EventUtility.ConvertEditorWindowCoordsToGuiViewCoords(pos);
            HandleMouseAutomationVisibility(pos, true);
            if (EventUtility.UpdateDragAndDrop(window, pos, modifiers))
                window.RepaintImmediately();
            window.RepaintImmediately();
        }

        public void EndDrop(Vector2 end, EventModifiers modifiers = EventModifiers.None)
        {
            end = EventUtility.ConvertEditorWindowCoordsToGuiViewCoords(end);
            HandleMouseAutomationVisibility(end, true);
            if (EventUtility.EndDragAndDrop(window, end, modifiers))
                window.RepaintImmediately();
        }

        public void KeyDownAndUp(KeyCode key, EventModifiers modifiers = EventModifiers.None)
        {
            HandleLastEventPauseVisibility();

            if (EventUtility.KeyDownAndUp(window, key, modifiers))
                window.RepaintImmediately();
        }

        public void SendKeyStrokesStream(IEnumerable<KeyCode> keys)
        {
            foreach (var key in keys)
            {
                KeyDownAndUp(key);
            }
        }

        /*
        public void PressKey(KeyCode key)
        {
            EventUtility.KeyDown(window, key);
        }

        public void ReleaseKey(KeyCode key)
        {
            EventUtility.KeyUp(window,key);
        }
        */
        public IEnumerable<IAutomatedUIElement> FindElementsByGUIStyle(GUIStyle style)
        {
            return m_Model.FindElementsByGUIStyle(style);
        }

        public IEnumerable<IAutomatedUIElement> FindElementsByGUIContent(GUIContent guiContent)
        {
            return m_Model.FindElementsByGUIContent(guiContent);
        }

        public IAutomatedUIElement nextSibling
        {
            get { return m_Model.nextSibling; }
        }
    }

    class IMModel : IElementFinder
    {
        private readonly List<IMGUIInstruction>       m_Instructions     = new List<IMGUIInstruction>();
        private readonly List<IMGUIClipInstruction>   m_ClipList         = new List<IMGUIClipInstruction>();
        private readonly List<IMGUILayoutInstruction> m_LayoutList       = new List<IMGUILayoutInstruction>();
        private readonly List<IMGUIDrawInstruction>   m_DrawInstructions = new List<IMGUIDrawInstruction>();

        private AutomatedIMElement[] m_Elements;
        private AutomatedIMElement m_Root;


        public void Update()
        {
            //TODO: right now we simbolicate the stacktrace of all elements, but for this scenario we are not super interested in it.
            GUIViewDebuggerHelper.GetUnifiedInstructions(m_Instructions);
            GUIViewDebuggerHelper.GetLayoutInstructions(m_LayoutList);
            GUIViewDebuggerHelper.GetClipInstructions(m_ClipList);
            GUIViewDebuggerHelper.GetDrawInstructions(m_DrawInstructions);

            GenerateDom();
        }

        private void GenerateDom()
        {
            int total = m_Instructions.Count;

            if (m_Elements == null || m_Elements.Length != total)
                m_Elements = new AutomatedIMElement[total];

            m_Root = new AutomatedIMElement(this, -1);
            m_Root.descendants = new ArraySegment<AutomatedIMElement>(m_Elements);

            Stack<AutomatedIMElement> ancestors = new Stack<AutomatedIMElement>();
            Stack<int> ancestorsIndex = new Stack<int>();
            ancestors.Push(m_Root);
            ancestorsIndex.Push(-1);

            for (int i = 0; i < total; ++i)
            {
                var instruction = m_Instructions[i];

                var parent = ancestors.Peek();

                AutomatedIMElement element = CreateAutomatedElement(instruction, i);
                m_Elements[i] = element;

                element.parent = parent;
                parent.AddChild(element);
                if (i + 1 < total)
                {
                    var nextInstruction = m_Instructions[i + 1];

                    if (nextInstruction.level > instruction.level)
                    {
                        ancestors.Push(element);
                        ancestorsIndex.Push(i);
                    }
                    if (nextInstruction.level < instruction.level)
                    {
                        for (int j = instruction.level - nextInstruction.level; j > 0; --j)
                        {
                            var closingParent = ancestors.Pop();
                            var closingParentIndex = ancestorsIndex.Pop();
                            closingParent.descendants = new ArraySegment<AutomatedIMElement>(m_Elements, closingParentIndex + 1, i - closingParentIndex);
                        }
                    }
                }
            }

            while (ancestors.Peek() != m_Root)
            {
                var closingParent = ancestors.Pop();
                var closingParentIndex = ancestorsIndex.Pop();
                closingParent.descendants = new ArraySegment<AutomatedIMElement>(m_Elements, closingParentIndex, (total - 1) - closingParentIndex);
            }
        }

        private AutomatedIMElement CreateAutomatedElement(IMGUIInstruction imguiInstruction, int index)
        {
            AutomatedIMElement element = new AutomatedIMElement(this, index);
            element.enabled = imguiInstruction.enabled;

            switch (imguiInstruction.type)
            {
                case InstructionType.kStyleDraw:
                {
                    var drawInstruction = m_DrawInstructions[imguiInstruction.typeInstructionIndex];
                    element.rect = drawInstruction.rect;
                    element.style = drawInstruction.usedGUIStyle;
                    element.guiContent = drawInstruction.usedGUIContent;
                    break;
                }
                case InstructionType.kLayoutBeginGroup:
                {
                    var layoutInstruction = m_LayoutList[imguiInstruction.typeInstructionIndex];
                    element.rect = layoutInstruction.unclippedRect;
                    element.style = layoutInstruction.style;
                    break;
                }
            }

            return element;
        }

        public IEnumerable<IAutomatedUIElement> FindElementsByGUIStyle(GUIStyle style)
        {
            return m_Root.FindElementsByGUIStyle(style);
        }

        public IEnumerable<IAutomatedUIElement> FindElementsByGUIContent(GUIContent guiContent)
        {
            return m_Root.FindElementsByGUIContent(guiContent);
        }

        public IAutomatedUIElement nextSibling
        {
            get { return m_Root.nextSibling; }
        }

        public void ViewContentsChanged()
        {
            //TODO: we dont need to update everytime the view actually change, we just need to "dirty" the current state
            //and update before we actually need the date to be synced.
            Update();
        }
    }

    interface IElementFinder
    {
        IEnumerable<IAutomatedUIElement> FindElementsByGUIStyle(GUIStyle style);
        IEnumerable<IAutomatedUIElement> FindElementsByGUIContent(GUIContent guiContent);
        IAutomatedUIElement nextSibling { get; }
    }

    interface IAutomatedUIElement : IElementFinder
    {
        string name { get; }

        IList<IAutomatedUIElement> children { get; }
        IAutomatedUIElement parent { get; }

        bool hasRect { get; }
        Rect rect { get; }

        bool enabled { get; }

        GUIStyle style { get; }
        GUIContent guiContent { get; }
    }

    class AutomatedIMElement : IAutomatedUIElement
    {
#pragma warning disable 414
        private IMModel m_ModelOwner;
        private int     m_instructionIndex;
#pragma warning restore 414

        private Rect?      m_Rect;
        private List<IAutomatedUIElement> m_Children = new List<IAutomatedUIElement>();

        public AutomatedIMElement(IMModel model, int index)
        {
            m_ModelOwner = model;
            m_instructionIndex = index;
        }

        public string name { get; }
        public IList<IAutomatedUIElement> children
        {
            get { return m_Children; }
        }

        public IAutomatedUIElement parent
        {
            get; set;
        }

        public bool enabled
        {
            get; set;
        }

        public IAutomatedUIElement nextSibling
        {
            get
            {
                if (parent == null)
                    return null;

                //TODO: this is a silly implementaiton. we can probably do better.
                var siblings = parent.children;
                for (int i = 0; i < siblings.Count; ++i)
                {
                    if (siblings[i] == this)
                    {
                        if (i + 1 < siblings.Count)
                            return siblings[i + 1];
                        break;
                    }
                }
                return null;
            }
        }

        public bool hasRect
        {
            get { return m_Rect.HasValue; }
        }

        public Rect rect
        {
            get
            {
                if (!hasRect)
                    throw new InvalidOperationException("Element does not contain rect info");
                return m_Rect.Value;
            }
            set
            {
                m_Rect = value;
            }
        }

        public GUIStyle style { get; set; }
        public GUIContent guiContent { get; set; }
        public ArraySegment<AutomatedIMElement> descendants { get; set; }

        public void AddChild(AutomatedIMElement element)
        {
            m_Children.Add(element);
        }

        protected virtual IEnumerable<IAutomatedUIElement> FindElements(Func<IAutomatedUIElement, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            // In the future, ArraySegment implements IEnumerable<T>...
            for (int i = 0; i < descendants.Count; ++i)
            {
                IAutomatedUIElement element = descendants.Array[descendants.Offset + i];
                if (predicate(element))
                    yield return element;
            }
            yield break;
        }

        public IEnumerable<IAutomatedUIElement> FindElementsByGUIStyle(GUIStyle style)
        {
            return FindElements(element => element.style == style);
        }

        private static string NullOrEmptyToNull(string input)
        {
            return string.IsNullOrEmpty(input) ? null : input;
        }

        private static bool GUIContentsAreEqual(GUIContent content1, GUIContent content2)
        {
            if (content1 == content2)
                return true;

            if (content1 == null || content2 == null)
                return false;

            // The native string type UTF16String does not differentiate between empty and null strings
            // Empty and null strings are considered equal here to work around this issue
            return NullOrEmptyToNull(content1.text) == NullOrEmptyToNull(content2.text) && content1.image == content2.image && NullOrEmptyToNull(content1.tooltip) == NullOrEmptyToNull(content2.tooltip);
        }

        public IEnumerable<IAutomatedUIElement> FindElementsByGUIContent(GUIContent guiContent)
        {
            return FindElements(element => GUIContentsAreEqual(element.guiContent, guiContent));
        }
    }
}
