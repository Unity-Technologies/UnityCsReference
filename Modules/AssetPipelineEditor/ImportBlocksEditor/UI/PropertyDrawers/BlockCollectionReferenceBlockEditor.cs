// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Renders a <see cref="BlockCollectionReference"/> as a read-only, fully-expanded tree; nested
    /// references recurse into their own providers. Edits happen on the referenced asset's own inspector.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    [CustomPropertyDrawer(typeof(BlockCollectionReference), useForChildren: true)]
    public class BlockCollectionReferenceBlockEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty blockProperty)
        {
            var root = new VisualElement();

            var blockRef = blockProperty?.managedReferenceValue as BlockCollectionReference;
            if (blockRef == null)
                return root;

            var refProp = blockProperty.FindPropertyRelative(blockRef.ReferenceFieldName);
            if (refProp == null)
                return root;

            // Reference field: shown only for references whose target is a user pick (a Block Asset).
            PropertyField refField = null;
            if (blockRef.ShowReferenceField)
            {
                refField = new PropertyField(refProp, "Reference");
                refField.Bind(blockProperty.serializedObject);
                root.Add(refField);
            }

            var blocksContainer = new VisualElement();
            blocksContainer.style.marginTop = 6;
            root.Add(blocksContainer);

            // One SerializedObject per expanded provider, kept alive for the panel's lifetime and
            // disposed on refresh or detach.
            var serializedObjects = new System.Collections.Generic.List<SerializedObject>();

            void DisposeSerializedObjects()
            {
                foreach (var so in serializedObjects)
                    so?.Dispose();
                serializedObjects.Clear();
            }

            // Deferred and coalesced: a tracker fires from the binding update, where rebuilding (and
            // disposing the tracked SerializedObjects) synchronously is not safe.
            bool refreshQueued = false;
            void RequestRefresh()
            {
                if (refreshQueued)
                    return;
                refreshQueued = true;
                blocksContainer.schedule.Execute(() =>
                {
                    refreshQueued = false;
                    RefreshList();
                });
            }

            void RefreshList()
            {
                DisposeSerializedObjects();
                blocksContainer.Clear();

                UpdateParentBlockHeader(root, blockRef);

                var referenced = blockRef.GetReferencedObject();
                if (referenced == null)
                    return;

                var so = new SerializedObject(referenced);
                serializedObjects.Add(so);

                var guard = new BlockCollectionReference.ProviderPathGuard();
                guard.TryEnter(referenced);
                blocksContainer.Add(ExpandLevel(so, blockRef.ProviderBlocksPath, guard, serializedObjects,
                    RequestRefresh));
            }

            // Rebuild the inline tree when the user repoints the reference (only when it's shown).
            refField?.RegisterValueChangeCallback(_ => RefreshList());

            // Detach disposes the SerializedObjects, so every attach (initial or a redock) must rebuild.
            root.RegisterCallback<AttachToPanelEvent>(_ => RefreshList());
            root.RegisterCallback<DetachFromPanelEvent>(_ => DisposeSerializedObjects());

            return root;
        }

        // Reference blocks recurse into their own provider; leaf fields render disabled — the
        // SerializedObjects wrap the real shared assets, so nothing here may write to them.
        static VisualElement ExpandLevel(SerializedObject providerSO, string blocksPath,
            BlockCollectionReference.ProviderPathGuard guard,
            System.Collections.Generic.List<SerializedObject> serializedObjects,
            System.Action requestRefresh)
        {
            var container = new VisualElement();

            var blocks = providerSO.FindProperty(blocksPath);
            if (blocks == null)
                return container;

            // Rebuild when the referenced asset changes under us (edited in another Inspector, undo,
            // revert). The tracker lives in the rebuilt content, so a refresh drops it with its
            // now-disposed SerializedObject.
            container.TrackSerializedObjectValue(providerSO, _ => requestRefresh());

            var styleSheet = UiHelpers.GetBlockItemStyleSheet();

            for (int i = 0; i < blocks.arraySize; i++)
            {
                var blockElem = blocks.GetArrayElementAtIndex(i);
                var item = new BlockItemElement(blockElem, blocks, i, typeof(IBlock), null, providerSO,
                    readOnly: true);
                if (styleSheet != null)
                    item.styleSheets.Add(styleSheet);

                if (blockElem.managedReferenceValue is BlockCollectionReference nestedRef)
                {
                    VisualElement childContent;

                    var referenced = nestedRef.GetReferencedObject();
                    if (referenced == null)
                    {
                        childContent = new VisualElement();
                    }
                    else
                    {
                        if (!guard.TryEnter(referenced))
                        {
                            childContent = new HelpBox(
                                $"'{referenced.name}' is referenced from within its own expansion, so it is " +
                                "not expanded here and importing will fail. Remove the reference cycle " +
                                "between the referenced block collections.",
                                HelpBoxMessageType.Warning);
                        }
                        else
                        {
                            var nestedSO = new SerializedObject(referenced);
                            serializedObjects.Add(nestedSO);
                            childContent = ExpandLevel(nestedSO, nestedRef.ProviderBlocksPath, guard,
                                serializedObjects, requestRefresh);
                            guard.Exit(referenced);
                        }
                    }

                    item.SetContent(childContent);
                }
                else if (blockElem.managedReferenceValue is IBlock)
                {
                    // Leaf field, disabled. Explicit Bind is required — PropertyFields created outside an
                    // inspector never self-bind.
                    var field = new PropertyField(blockElem);
                    field.Bind(blockElem.serializedObject);
                    field.SetEnabled(false);
                    item.SetContent(field);
                }
                else
                {
                    item.SetContent(null);
                }

                container.Add(item);
            }

            return container;
        }

        static void UpdateParentBlockHeader(VisualElement root, BlockCollectionReference blockRef)
        {
            if (root.panel != null)
            {
                ApplyHeader();
                return;
            }

            root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            void OnAttach(AttachToPanelEvent _)
            {
                root.UnregisterCallback<AttachToPanelEvent>(OnAttach);
                ApplyHeader();
            }

            void ApplyHeader()
            {
                var item = root.GetFirstAncestorOfType<BlockItemElement>();
                if (item == null) return;
                // Deliberately short — the reference color hint already signals the type.
                var refObj = blockRef.GetReferencedObject();
                item.OverrideHeaderLabel(refObj != null ? refObj.name : "Unassigned");
            }
        }
    }
}
