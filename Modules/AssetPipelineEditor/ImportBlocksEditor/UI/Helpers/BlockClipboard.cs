// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Handles copy/paste operations for blocks and block collections.
    /// </summary>
    internal static class BlockClipboard
    {
        // Distinct prefixes let HasBlockInClipboard / HasCollectionInClipboard tell single-block and collection
        // payloads apart with a cheap string check, without deserializing.
        const string k_BlockPrefix = "ImportBlocks_Block_";
        const string k_CollectionPrefix = "ImportBlocks_Collection_";

        // Transient host whose [SerializeReference] list lets EditorJsonUtility serialize each block's concrete type
        // and fields natively — and reconstruct them (as deep clones with correct types) on read. This is what lets
        // the clipboard avoid tracking assembly-qualified type names or instantiating types by hand.
        class ClipboardHost : ScriptableObject
        {
            [SerializeReference] public List<IBlock> blocks = new List<IBlock>();
        }

        static void WriteToClipboard(string prefix, IEnumerable<IBlock> blocks)
        {
            var host = ScriptableObject.CreateInstance<ClipboardHost>();
            try
            {
                foreach (var block in blocks)
                {
                    if (block != null)
                        host.blocks.Add(block);
                }

                EditorGUIUtility.systemCopyBuffer = prefix + EditorJsonUtility.ToJson(host);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // Reads and deep-clones the blocks stored under <paramref name="prefix"/>, or null when the system clipboard
        // holds no matching payload. Entries whose type can no longer be resolved come back null (Unity's
        // [SerializeReference] missing-type handling) and are dropped.
        static List<IBlock> ReadFromClipboard(string prefix)
        {
            var clipboardData = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboardData) || !clipboardData.StartsWith(prefix))
                return null;

            var host = ScriptableObject.CreateInstance<ClipboardHost>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(clipboardData.Substring(prefix.Length), host);

                var result = new List<IBlock>(host.blocks.Count);
                foreach (var block in host.blocks)
                {
                    if (block != null)
                        result.Add(block);
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read blocks from clipboard: {e.Message}");
                return null;
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        public static void CopyBlock(SerializedProperty blockProperty)
        {
            if (blockProperty?.managedReferenceValue is not IBlock block) return;
            WriteToClipboard(k_BlockPrefix, new[] { block });
        }

        public static bool PasteBlock(SerializedProperty blocksArrayProperty, int index, Type requiredInterface = null)
        {
            var blocks = ReadFromClipboard(k_BlockPrefix);
            if (blocks == null || blocks.Count == 0) return false;

            var block = blocks[0];
            if (requiredInterface != null && !requiredInterface.IsInstanceOfType(block))
            {
                Debug.LogWarning(
                    $"Cannot paste block '{block.GetType().Name}': it does not implement '{requiredInterface.Name}'");
                return false;
            }

            // A pasted block is a new block: give it (and any nested children) fresh identity so it does not collide
            // with the block it was copied from. The override system addresses blocks by BlockInstanceID.
            if (block is Block pastedBlock)
                Block.AssignFreshBlockInstanceIds(new[] { pastedBlock });

            blocksArrayProperty.serializedObject.Update();
            blocksArrayProperty.InsertArrayElementAtIndex(index);
            blocksArrayProperty.GetArrayElementAtIndex(index).managedReferenceValue = block;
            blocksArrayProperty.serializedObject.ApplyModifiedProperties();

            return true;
        }

        public static bool PasteBlockValues(SerializedProperty blockProperty)
        {
            if (blockProperty?.managedReferenceValue is not IBlock currentBlock) return false;

            var blocks = ReadFromClipboard(k_BlockPrefix);
            if (blocks == null || blocks.Count == 0) return false;

            var newBlock = blocks[0];

            // Only allow pasting values if the types match exactly.
            if (currentBlock.GetType() != newBlock.GetType())
            {
                Debug.LogWarning(
                    $"Cannot paste values: clipboard contains '{newBlock.GetType().Name}' but target is '{currentBlock.GetType().Name}'");
                return false;
            }

            // "Paste values" intentionally keeps the target block's BlockInstanceID: its identity is unchanged, only
            // its field values are overwritten.
            if (currentBlock is Block currentBase && newBlock is Block newBase)
                newBase.SetBlockInstanceID(currentBase.BlockInstanceID);

            blockProperty.serializedObject.Update();
            blockProperty.managedReferenceValue = newBlock;
            blockProperty.serializedObject.ApplyModifiedProperties();

            if (blockProperty.serializedObject.targetObject != null)
                EditorUtility.SetDirty(blockProperty.serializedObject.targetObject);

            return true;
        }

        public static bool HasBlockOfType(Type targetType)
        {
            var blocks = ReadFromClipboard(k_BlockPrefix);
            return blocks != null && blocks.Count > 0 && blocks[0].GetType() == targetType;
        }

        public static bool DuplicateBlock(SerializedProperty blocksArrayProperty, int index,
            Type requiredInterface = null)
        {
            if (index < 0 || index >= blocksArrayProperty.arraySize) return false;

            var blockProperty = blocksArrayProperty.GetArrayElementAtIndex(index);
            if (blockProperty?.managedReferenceValue is not IBlock source) return false;

            var type = source.GetType();
            if (requiredInterface != null && !requiredInterface.IsInstanceOfType(source))
            {
                Debug.LogWarning(
                    $"Cannot duplicate block '{type.Name}': it does not implement '{requiredInterface.Name}'");
                return false;
            }

            var clone = CloneBlock(source);
            if (clone == null) return false;

            // A duplicate is a new block: give it (and any nested children) fresh identity.
            if (clone is Block cloneBlock)
                Block.AssignFreshBlockInstanceIds(new[] { cloneBlock });

            blocksArrayProperty.serializedObject.Update();
            blocksArrayProperty.InsertArrayElementAtIndex(index + 1);
            blocksArrayProperty.GetArrayElementAtIndex(index + 1).managedReferenceValue = clone;
            blocksArrayProperty.serializedObject.ApplyModifiedProperties();

            return true;
        }

        public static void CopyCollection(SerializedProperty blocksArrayProperty)
        {
            var blocks = new List<IBlock>(blocksArrayProperty.arraySize);
            for (int i = 0; i < blocksArrayProperty.arraySize; i++)
            {
                if (blocksArrayProperty.GetArrayElementAtIndex(i).managedReferenceValue is IBlock block)
                    blocks.Add(block);
            }

            WriteToClipboard(k_CollectionPrefix, blocks);
        }

        public static bool PasteCollection(SerializedProperty blocksArrayProperty, bool append = false,
            Type requiredInterface = null)
        {
            var blocks = ReadFromClipboard(k_CollectionPrefix);
            if (blocks == null || blocks.Count == 0) return false;

            // Resolve the compatible paste set BEFORE touching the destination. A "replace" paste must not clear the
            // existing collection unless there is at least one compatible block to replace it with.
            var compatible = new List<IBlock>();
            var skippedBlocks = new List<string>();

            foreach (var block in blocks)
            {
                if (requiredInterface != null && !requiredInterface.IsInstanceOfType(block))
                {
                    skippedBlocks.Add(block.GetType().Name);
                    continue;
                }

                compatible.Add(block);
            }

            if (compatible.Count == 0)
            {
                // Nothing valid to paste — leave the destination untouched.
                if (skippedBlocks.Count > 0)
                    Debug.LogWarning($"Pasted 0 compatible block(s). {DescribeSkipped(skippedBlocks, requiredInterface)}");

                return false;
            }

            blocksArrayProperty.serializedObject.Update();

            if (!append)
                blocksArrayProperty.ClearArray();

            foreach (var block in compatible)
            {
                // Each pasted block is a new block: fresh identity so it does not collide with its source.
                if (block is Block pastedBlock)
                    Block.AssignFreshBlockInstanceIds(new[] { pastedBlock });

                blocksArrayProperty.InsertArrayElementAtIndex(blocksArrayProperty.arraySize);
                blocksArrayProperty.GetArrayElementAtIndex(blocksArrayProperty.arraySize - 1).managedReferenceValue = block;
            }

            blocksArrayProperty.serializedObject.ApplyModifiedProperties();

            if (skippedBlocks.Count > 0)
                Debug.LogWarning($"Pasted {compatible.Count} compatible block(s). {DescribeSkipped(skippedBlocks, requiredInterface)}");

            return true;
        }

        static IBlock CloneBlock(IBlock source)
        {
            var clones = BlockCollectionReference.DeepCloneBlocks(new[] { source });
            return clones.Count > 0 ? clones[0] : null;
        }

        static string DescribeSkipped(List<string> skippedBlocks, Type requiredInterface)
        {
            string interfaceName = requiredInterface?.Name ?? "target interface";
            return $"Skipped {skippedBlocks.Count} incompatible block(s) that do not implement '{interfaceName}': {string.Join(", ", skippedBlocks)}";
        }

        public static bool HasBlockInClipboard()
        {
            var clipboardData = EditorGUIUtility.systemCopyBuffer;
            return clipboardData != null && clipboardData.StartsWith(k_BlockPrefix);
        }

        public static bool HasCollectionInClipboard()
        {
            var clipboardData = EditorGUIUtility.systemCopyBuffer;
            return clipboardData != null && clipboardData.StartsWith(k_CollectionPrefix);
        }
    }
}
