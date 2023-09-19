// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    [Serializable]
    struct EditorTypeAssociation : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_TargetContext, m_TargetBehaviour, m_EditorType;

        // Context and behaviour types can be null, and should be treated as universally applicable.
        public Type targetContext { get; private set; }
        public Type targetBehaviour { get; private set; }
        public Type editor { get; private set; }

        public EditorTypeAssociation(Type editor, Type targetBehaviour, Type targetContext)
        {
            this.targetContext = targetContext;
            this.targetBehaviour = targetBehaviour;
            this.editor = editor;
            m_EditorType = m_TargetBehaviour = m_TargetContext = null;
        }

        public void OnBeforeSerialize()
        {
            m_TargetContext = targetContext?.AssemblyQualifiedName;
            m_TargetBehaviour = targetBehaviour?.AssemblyQualifiedName;
            m_EditorType = editor?.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_TargetContext))
                targetContext = Type.GetType(m_TargetContext);
            if (!string.IsNullOrEmpty(m_TargetBehaviour))
                targetBehaviour = Type.GetType(m_TargetBehaviour);
            if (!string.IsNullOrEmpty(m_EditorType))
                editor = Type.GetType(m_EditorType);
            m_TargetContext = m_TargetBehaviour = m_EditorType = null;
        }
    }

    // Defines a selection driven instance of some type, either EditorTool or EditorToolContext
    [Serializable]
    class ComponentEditor
    {
        internal enum EditorToolScope
        {
            NotInitialized,
            ToolContext,
            ComponentTool,
            ManipulationToolOverride
        };

        [SerializeField]
        bool m_LockedInspector;

        [SerializeField]
        Editor m_Inspector;

        [SerializeField]
        ScriptableObject m_Editor;

        EditorTypeAssociation m_EditorTypeAssociation;
        public readonly ActiveEditorTracker tracker;
        public List<Editor> additionalEditors;
        public EditorToolScope m_EditorToolScope;

        public Editor inspector => m_Inspector;
        public Type editorType => m_EditorTypeAssociation.editor;
        public EditorTypeAssociation typeAssociation => m_EditorTypeAssociation;
        public bool lockedInspector => m_LockedInspector;
        public EditorToolScope editorToolScope => m_EditorToolScope;

        public UnityObject editor => m_Editor;

        public T GetEditor<T>() where T : ScriptableObject, IEditor => m_Editor as T;

        public ComponentEditor(EditorTypeAssociation typeAssociation, ActiveEditorTracker tracker, Editor inspector)
        {
            if (typeAssociation.editor == null)
                throw new ArgumentNullException("typeAssociation");

            if (!typeof(IEditor).IsAssignableFrom(typeAssociation.editor)
                || !typeof(UnityObject).IsAssignableFrom(typeAssociation.editor))
                throw new ArgumentException("Tool type must implement UnityEngine.ScriptableObject, IEditor.", "typeAssociation");

            this.tracker = tracker;

            m_Inspector = inspector;
            m_LockedInspector = tracker.isLocked;
            m_EditorTypeAssociation = typeAssociation;
            m_EditorToolScope = EditorToolScope.NotInitialized;
        }

        public UnityObject target => inspector != null ? inspector.target : null;

        public UnityObject[] targets
        {
            get
            {
                if (additionalEditors == null)
                    return inspector.targets;
                List<UnityObject> objects = new List<UnityObject>(inspector.targets);
                foreach (var insp in additionalEditors)
                    objects.AddRange(insp.targets);
                return objects.ToArray();
            }
        }

        public void AddInspector(Editor addlInspector)
        {
            if (additionalEditors == null)
                additionalEditors = new List<Editor>() { addlInspector };
            else
                additionalEditors.Add(addlInspector);
        }

        public UnityObject InstantiateEditor()
        {
            var toolType = editorType;

            m_Editor = ScriptableObject.CreateInstance(toolType, x =>
            {
                ((IEditor)x).SetTargets(targets);
                ((IEditor)x).SetTarget(target);
            });

            m_Editor.hideFlags = HideFlags.DontSave;

            if(m_Editor is EditorToolContext)
                m_EditorToolScope = EditorToolScope.ToolContext;
            else if(m_Editor is EditorTool tool &&
                    EditorToolUtility.GetEnumWithEditorTool(tool, EditorToolManager.activeToolContext) != Tool.Custom)
                m_EditorToolScope = EditorToolScope.ManipulationToolOverride;
            else
                m_EditorToolScope = EditorToolScope.ComponentTool;

            return m_Editor;
        }
    }

    // A lookup table of component (target) to editor (tool or context). Null targets are explicitly allowed. Null
    // target types are treated as "global" editors.
    class EditorToolCache
    {
        // Placeholder type for global editors to register as keys
        struct NullTargetKey {}

        Type m_AttributeType;
        EditorTypeAssociation[] s_AvailableEditorTypeAssociations = null;
        Dictionary<Type, List<EditorTypeAssociation>> s_EditorTargetCache = new Dictionary<Type, List<EditorTypeAssociation>>();

        // Static fields in generic classes result in multiple static field instances. In this case that's fine.
        // ReSharper disable once StaticMemberInGenericType
        static ActiveEditorTracker m_Tracker;

        static ActiveEditorTracker sharedTracker
        {
            get
            {
                if (m_Tracker == null)
                    m_Tracker = new ActiveEditorTracker();
                return m_Tracker;
            }
        }

        public EditorToolCache(Type attributeType)
        {
            if (!typeof(ToolAttribute).IsAssignableFrom(attributeType))
                throw new ArgumentException("Attribute type must inherit ToolAttribute.", "attributeType");
            m_AttributeType = attributeType;
        }

        public int Count => availableEditorTypeAssociations.Length;

        EditorTypeAssociation[] availableEditorTypeAssociations
        {
            get
            {
                if (s_AvailableEditorTypeAssociations == null)
                {
                    Type[] editorTools = TypeCache.GetTypesWithAttribute(m_AttributeType)
                        .Where(x => !x.IsAbstract)
                        .ToArray();
                    int len = editorTools.Length;
                    s_AvailableEditorTypeAssociations = new EditorTypeAssociation[len];

                    for (int i = 0; i < len; i++)
                    {
                        var customToolAttribute = editorTools[i].GetCustomAttributes(m_AttributeType, false)
                            .FirstOrDefault() as ToolAttribute;

                        if (customToolAttribute == null)
                            continue;

                        s_AvailableEditorTypeAssociations[i] = new EditorTypeAssociation(
                            editorTools[i], customToolAttribute.targetType ?? typeof(NullTargetKey), customToolAttribute.targetContext);
                    }
                }

                return s_AvailableEditorTypeAssociations;
            }
        }

        public Type GetTargetType(Type editorType)
        {
            for (int i = 0, c = availableEditorTypeAssociations.Length; i < c; i++)
                if (availableEditorTypeAssociations[i].editor == editorType)
                    return availableEditorTypeAssociations[i].targetBehaviour == typeof(NullTargetKey)
                        ? null
                        : availableEditorTypeAssociations[i].targetBehaviour;
            return null;
        }

        // Returns a reference to the cached target editor type list. If this is made public make sure to also return
        // a copy or modify to instead populate a pre-allocated list.
        internal IEnumerable<EditorTypeAssociation> GetEditorsForTargetType(Type target)
        {
            // Tools with 'null' target are considered to be Global tools
            if (target == null)
                target = typeof(NullTargetKey);

            if (s_EditorTargetCache.TryGetValue(target, out List<EditorTypeAssociation> res))
                return res;

            s_EditorTargetCache.Add(target, res = new List<EditorTypeAssociation>());

            for (int i = 0, c = availableEditorTypeAssociations.Length; i < c; i++)
            {
                if (availableEditorTypeAssociations[i].targetBehaviour != null
                    && (availableEditorTypeAssociations[i].targetBehaviour.IsAssignableFrom(target)
                        || target.IsAssignableFrom(availableEditorTypeAssociations[i].targetBehaviour)))
                    res.Add(availableEditorTypeAssociations[i]);
            }

            return res;
        }

        void CollectEditorsForTracker(EditorToolContext ctx, ActiveEditorTracker tracker, List<ComponentEditor> editors)
        {
            var trackerEditors = tracker.activeEditors;

            for (int i = 0, c = trackerEditors.Length; i < c; i++)
            {
                var editor = trackerEditors[i];
                var target = editor != null ? editor.target : null;

                if (target == null || EditorUtility.IsPersistent(target))
                    return;

                var eligible = GetEditorsForTargetType(editor.target.GetType());
                var activeContextType = ctx == null ? typeof(GameObjectToolContext) : ctx.GetType();

                foreach (var association in eligible)
                {
                    if (association.targetContext != null && association.targetContext != activeContextType)
                        continue;

                    // Shared trackers should create one tool per-type, regardless of how many targets are present.
                    // The exception is locked inspectors, which can create a distinct tool instance for each target
                    // not already represented. That means that if the shared tracker and locked inspector are inspecting
                    // the same selection, there should only be one tool per tool type. However if the locked inspector
                    // and selection are different, there may be duplicate tool types with different targets.
                    var existing = editors.Find(x =>
                        x.editorType == association.editor
                        && (x.target == target || ReferenceEquals(x.tracker, tracker))
                    );

                    if (existing != null)
                        existing.AddInspector(editor);
                    else
                        editors.Add(new ComponentEditor(association, tracker, editor));
                }
            }
        }

        public void InstantiateEditors(EditorToolContext ctx, List<ComponentEditor> editors)
        {
            editors.Clear();

            // If the shared tracker is locked, use our own tracker instance so that the current selection is always
            // represented. Addresses case where a single locked inspector is open.
            var shared = ActiveEditorTracker.sharedTracker;
            var activeTracker = shared.isLocked ? sharedTracker : shared;
            var propertyEditors = PropertyEditor.GetPropertyEditors();

            // Collect editor tools for the shared tracker first, then any locked inspectors or open properties editors
            CollectEditorsForTracker(ctx, activeTracker, editors);

            foreach (var propertyEditor in propertyEditors)
            {
                if (propertyEditor is InspectorWindow)
                {
                    if ((propertyEditor as InspectorWindow).isLocked)
                        CollectEditorsForTracker(ctx, propertyEditor.tracker, editors);
                }
                else
                {
                    CollectEditorsForTracker(ctx, propertyEditor.tracker, editors);
                }
            }

            foreach (var editor in editors)
                editor.InstantiateEditor();
        }
    }
}
