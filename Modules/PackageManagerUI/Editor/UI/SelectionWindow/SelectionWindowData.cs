// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

[Serializable]
internal class SelectionWindowData : ISerializationCallbackReceiver
{
    private static readonly string k_RemoveText = L10n.Tr("Remove");

    [Serializable]
    internal class Node
    {
        public int parentIndex;
        public int index;
        public List<int> childIndexes = new();

        public Asset asset;
        public string name;
        public bool isFolder;

        public string path => asset?.importedPath;

        public bool isMoved => !isFolder && asset != null && asset.importedPath != asset.origin.assetPath;
    }

    public string headerTitle;
    public string headerDescription;
    public string actionLabel;
    private List<Node> m_Nodes = new();
    public IReadOnlyList<Node> nodes => m_Nodes;

    // This node is not created through the SelectionWindowData.CreateNode function, because we don't want it to be
    // part of the final visible tree. We set the index to `-1` such that if it accidentally end up in the final tree,
    // we will get an InvalidIndexException. This hidden node is only used when we construct trees.
    public Node hiddenRootNode = new() { index = -1, parentIndex = -1, isFolder = true};

    private HashSet<int> m_SelectedIndexes = new();
    [SerializeField]
    private int[] m_SerializedSelectedIndexes = Array.Empty<int>();

    public IReadOnlyList<Asset> selectedAssets => m_SelectedIndexes
        .Where(index => !nodes[index].isFolder)
        .Select(index => nodes[index].asset).ToArray();
    public IReadOnlyList<Asset> assets => nodes.Where(n => !n.isFolder).Select(n => n.asset).ToArray();

    public int selectedNodesCount => m_SelectedIndexes.Count;

    // This constructor only constructs an instance of SelectionWindowData for the remove case.
    public SelectionWindowData(IEnumerable<Asset> assetsList, string packageName, string description)
    {
        headerTitle = packageName;
        headerDescription = description;
        actionLabel = k_RemoveText;

        foreach (var asset in assetsList.OrderBy(a => a.importedPath))
        {
            // We don't support removing assets that are outside of the "Assets" folder.
            // The only known case of that is when a .unitypackage injects a UPM package in the "Packages" folder.
            // In that case, it will be handle as an embedded package.
            if (!asset.importedPath.StartsWith("Assets/"))
            {
                Debug.Log($"[Package Manager Window] Cannot remove the asset at {asset.importedPath}: This asset is outside of the Assets folder.");
                continue;
            }

            // Every imported assets with an AssetOrigin should be in the "Assets/" folder.
            // We skip 1 because we don't want to show "Assets/" as the root node in the selection window.
            var pathParts = asset.importedPath.Split('/').Skip(1).ToArray();

            var currentNode = hiddenRootNode;
            for (var i = 0; i < pathParts.Length; ++i)
            {
                var isLeafNode = i == pathParts.Length - 1;
                var nodeToCreate = pathParts[i];
                var matchingChildNode = GetChildren(currentNode).FirstOrDefault(c => c.name == nodeToCreate);

                if (matchingChildNode == null)
                {
                    var newNode = isLeafNode ? AddFileNode(currentNode, asset, nodeToCreate) : AddFolderNode(currentNode, nodeToCreate);
                    // Move one step down the tree to the newly created node.
                    currentNode = newNode;
                    continue;
                }

                // This handles the case where the package contains duplicate file paths.
                // It should not be possible, but if it ever happens, it will be treated as if it was the same file.
                if (isLeafNode)
                    continue;

                currentNode = matchingChildNode;
            }
        }
    }

    public void AddSelection(int id) => AddSelection(id, true, true);

    private void AddSelection(int id, bool selectChildren, bool selectParent)
    {
        if (id < 0 || id >= nodes.Count)
            return;
        m_SelectedIndexes.Add(id);

        var node = nodes[id];
        if (selectChildren)
            foreach (var childId in node.childIndexes)
                AddSelection(childId, true, false);

        if (selectParent)
            AddSelection(node.parentIndex, false, true);
    }

    public void RemoveSelection(int id) => RemoveSelection(id, true, true);

    private void RemoveSelection(int id, bool deselectChildren, bool refreshParentSelection)
    {
        if (id < 0 || id >= nodes.Count)
            return;
        m_SelectedIndexes.Remove(id);
        var node = nodes[id];
        if (deselectChildren)
            foreach (var childId in node.childIndexes)
                RemoveSelection(childId, true, false);

        if (refreshParentSelection && AllChildrenAreDeselected(node.parentIndex))
            RemoveSelection(node.parentIndex, false, true);
    }

    private bool AllChildrenAreDeselected(int id)
    {
        if (id < 0 || id >= nodes.Count)
            return false;
        return nodes[id].childIndexes.All(childId => !IsSelected(childId));
    }

    public bool IsSelected(int id) => m_SelectedIndexes.Contains(id);
    public void ClearSelection() => m_SelectedIndexes.Clear();
    public void SelectAll() => m_SelectedIndexes = nodes.Select(n => n.index).ToHashSet();

    private Node AddNode(Node parentNode, string name)
    {
        var newNode = new Node
        {
            index = nodes.Count,
            name = name,
        };

        m_Nodes.Add(newNode);
        m_SelectedIndexes.Add(newNode.index);
        parentNode.childIndexes.Add(newNode.index);
        newNode.parentIndex = parentNode.index;
        return newNode;
    }

    private Node AddFileNode(Node parentNode, Asset asset, string name)
    {
        var node = AddNode(parentNode, name);
        node.asset = asset;
        return node;
    }

    private Node AddFolderNode(Node parentNode, string name)
    {
        var node = AddNode(parentNode, name);
        node.asset = null;
        node.isFolder = true;
        return node;
    }

    public IEnumerable<Node> GetChildren(Node node, bool recursive = false)
    {
        foreach (var id in node.childIndexes)
        {
            yield return nodes[id];
            if (!recursive)
                continue;
            foreach (var childNode in GetChildren(nodes[id], true))
                yield return childNode;
        }
    }

    public void OnBeforeSerialize()
    {
        m_SerializedSelectedIndexes = m_SelectedIndexes.ToArray();
    }

    public void OnAfterDeserialize()
    {
        m_SelectedIndexes = m_SerializedSelectedIndexes.ToHashSet();
    }
}
