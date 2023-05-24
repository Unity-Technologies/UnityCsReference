// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.Audio;

static class UIToolkitUtilities
{
    internal static VisualTreeAsset LoadUxml(string filePath)
    {
        var asset = EditorGUIUtility.Load(filePath) as VisualTreeAsset;
        Assert.IsNotNull(asset, $"Could not load UXML file from editor default resources at path: {filePath}.");
        return asset;
    }

    internal static StyleSheet LoadStyleSheet(string filePath)
    {
        var asset = EditorGUIUtility.Load(filePath) as StyleSheet;
        Assert.IsNotNull(asset, $"Could not load UXML file from editor default resources at path: {filePath}.");
        return asset;
    }

    internal static Texture2D LoadIcon(string filename)
    {
        var filePath = $"{Path.Combine("Icons", "Audio", filename)}@2x.png";
        var asset = EditorGUIUtility.LoadIcon(filePath);
        Assert.IsNotNull(asset, $"Could not load icon from editor default resources at path: {filePath}.");
        return asset;
    }

    internal static T GetChildByName<T>(VisualElement parentElement, string childName) where T : VisualElement
    {
        var childElement = parentElement.Query<VisualElement>(childName).First();
        Assert.IsNotNull(childElement, $"Could not find child element '{childName}' in visual tree of element '{parentElement.name}'.");
        var childElementCast = childElement as T;
        Assert.IsNotNull(childElementCast, $"Child element '{childName}' of '{parentElement.name}' is not of type {nameof(T)}");
        return childElementCast;
    }

    internal static T GetChildByClassName<T>(VisualElement parentElement, string childClassName) where T : VisualElement
    {
        var childElement = parentElement.Query<VisualElement>(className: childClassName).First();
        Assert.IsNotNull(childElement, $"Could not find child element '{childClassName}' in visual tree of element '{parentElement.name}'.");
        var childElementCast = childElement as T;
        Assert.IsNotNull(childElementCast, $"Child element '{childClassName}' of '{parentElement.name}' is not of type {nameof(T)}");
        return childElementCast;
    }


    internal static T GetChildAtIndex<T>(VisualElement parentElement, int index) where T : VisualElement
    {
        var childElement = parentElement.ElementAt(index);
        Assert.IsNotNull(childElement, $"{parentElement.name} has no child element at '{index}.");
        var childElementCast = childElement as T;
        Assert.IsNotNull(childElementCast, $"Child element of '{parentElement.name}' at index {index} is not of type {nameof(T)}");
        return childElementCast;
    }
}
