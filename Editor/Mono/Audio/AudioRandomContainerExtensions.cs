// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityEditor;

static class AudioRandomContainerExtensions
{
    const string k_BaseAddElementsUndoName = $"Add {nameof(AudioRandomContainer)} element";

    /// <summary>
    /// Adds a number of new, default-initialized <see cref="AudioContainerElement"/> objects to <see cref="AudioRandomContainer.elements"/>.
    /// </summary>
    /// <param name="container">The instance to add the elements to.</param>
    /// <param name="count">The number of elements to add.</param>
    internal static void AddElements(this AudioRandomContainer container, int count)
    {
        ValidateContainer(container);

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Must be greater than zero.");

        AddElementsInner(container, count, (i, element) => { });
    }

    /// <summary>
    /// Adds a given number of new <see cref="AudioContainerElement"/> objects with clips assigned to <see cref="AudioRandomContainer.elements"/>.
    /// </summary>
    /// <param name="container">The instance to add the elements to.</param>
    /// <param name="clips">An array of <see cref="AudioClip"/> objects to be assigned to each new element.</param>
    internal static void AddElements(this AudioRandomContainer container, AudioClip[] clips)
    {
        ValidateContainer(container);

        if (clips == null)
            throw new ArgumentNullException(nameof(clips));

        if (clips.Length == 0)
            throw new ArgumentException("Must not be empty.", nameof(clips));

        AddElementsInner(container, clips.Length, (i, element) =>
        {
            if (clips[i] != null)
                element.audioClip = clips[i];
        });
    }

    static void ValidateContainer(AudioRandomContainer container)
    {
        if (container == null)
            throw new ArgumentNullException(nameof(container));

        if (!EditorUtility.IsPersistent(container))
            throw new ArgumentException("Must be a persistent asset.", nameof(container));
    }

    static void AddElementsInner(AudioRandomContainer container, int count,
        Action<int, AudioContainerElement> configureElement)
    {
        var undoGroupName = count > 1 ? $"{k_BaseAddElementsUndoName}s" : k_BaseAddElementsUndoName;

        Undo.RegisterCompleteObjectUndo(container, undoGroupName);
        Undo.SetCurrentGroupName(undoGroupName);

        if (container.elements == null)
            container.elements = Array.Empty<AudioContainerElement>();

        var newElements = new AudioContainerElement[count];
        var oldAndNewElements = new AudioContainerElement[container.elements.Length + count];
        Array.Copy(container.elements, oldAndNewElements, container.elements.Length);

        for (var i = 0; i < count; i++)
        {
            var element = new AudioContainerElement
            {
                name = $"{nameof(AudioContainerElement)}-{GUID.Generate()}", hideFlags = HideFlags.HideInHierarchy
            };

            configureElement(i, element);
            newElements[i] = element;
            oldAndNewElements[container.elements.Length + i] = element;
            AssetDatabase.AddObjectToAsset(element, container);
            EditorUtility.SetDirty(element);
        }

        container.elements = oldAndNewElements;
        EditorUtility.SetDirty(container);

        foreach (var element in newElements)
            Undo.RegisterCreatedObjectUndo(element, k_BaseAddElementsUndoName);

        // Note: we deliberately don't save the root asset or the sub assets here
        // as this by unspoken Unity convention generally is considered the user's initiative.

        var undoGroup = Undo.GetCurrentGroup();
        Undo.CollapseUndoOperations(undoGroup);
        Undo.IncrementCurrentGroup();
    }
}
