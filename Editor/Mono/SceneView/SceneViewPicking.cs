// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    // No BaseSelection available:
    // 1. Just cycle through the selection from topmost to bottom

    // SelectionBase available for topmost object:
    // 1. First click selects the base
    // 2. Second click selects the topmost
    // 3. All subsequent clicks cycle through the stack of overlapping objects from top to bottom, regardless of their SelectionBase status
    // 4. When we hit the bottom, we goto 1

    // Example: Scene from back to front (visually): Panel(base), Label, Image, Button(base), Image2, Label2
    // Selection order: Button, Label2, Image2, Image, Label, Panel, goto start

    internal class SceneViewPicking
    {
        private static bool s_RetainHashes = false;
        private static int s_PreviousTopmostHash = 0;
        private static int s_PreviousPrefixHash = 0;

        static SceneViewPicking()
        {
            Selection.selectionChanged += ResetHashes;
        }

        private static void ResetHashes()
        {
            if (!s_RetainHashes)
            {
                s_PreviousTopmostHash = 0;
                s_PreviousPrefixHash = 0;
            }

            s_RetainHashes = false;
        }

        public static GameObject PickGameObject(Vector2 mousePosition)
        {
            s_RetainHashes = true;

            var enumerator = GetAllOverlapping(mousePosition).GetEnumerator();
            if (!enumerator.MoveNext())
                return null;

            var topmost = enumerator.Current;
            var selectionBase = HandleUtility.FindSelectionBase(topmost);
            var first = (selectionBase == null ? topmost : selectionBase);
            int topmostHash = topmost.GetHashCode();
            int prefixHash = topmostHash;

            if (Selection.activeGameObject == null)
            {
                // Nothing selected
                // Return selection base if it exists, otherwise topmost game object
                s_PreviousTopmostHash = topmostHash;
                s_PreviousPrefixHash = prefixHash;
                return first;
            }

            if (topmostHash != s_PreviousTopmostHash)
            {
                // Topmost game object changed
                // Return selection base if exists and is not already selected, otherwise topmost game object
                s_PreviousTopmostHash = topmostHash;
                s_PreviousPrefixHash = prefixHash;
                return (Selection.activeGameObject == selectionBase ? topmost : first);
            }

            s_PreviousTopmostHash = topmostHash;

            // Pick potential selection base before topmost game object
            if (Selection.activeGameObject == selectionBase)
            {
                if (prefixHash == s_PreviousPrefixHash)
                    return topmost;
                else
                {
                    s_PreviousPrefixHash = prefixHash;
                    return selectionBase;
                }
            }

            // Check if active game object will appear in selection stack
            var picked = HandleUtility.PickGameObject(mousePosition, false, null, new GameObject[] { Selection.activeGameObject });
            if (picked == Selection.activeGameObject)
            {
                // Advance enumerator to active game object
                while (enumerator.Current != Selection.activeGameObject)
                {
                    if (!enumerator.MoveNext())
                    {
                        s_PreviousPrefixHash = topmostHash;
                        return first; // Should not occur
                    }

                    UpdateHash(ref prefixHash, enumerator.Current);
                }
            }

            if (prefixHash != s_PreviousPrefixHash)
            {
                // Prefix hash changed, start over
                s_PreviousPrefixHash = topmostHash;
                return first;
            }

            // Move on to next game object
            if (!enumerator.MoveNext())
            {
                s_PreviousPrefixHash = topmostHash;
                return first; // End reached, start over
            }

            UpdateHash(ref prefixHash, enumerator.Current);

            if (enumerator.Current == selectionBase)
            {
                // Skip selection base
                if (!enumerator.MoveNext())
                {
                    s_PreviousPrefixHash = topmostHash;
                    return first; // End reached, start over
                }

                UpdateHash(ref prefixHash, enumerator.Current);
            }

            s_PreviousPrefixHash = prefixHash;
            return enumerator.Current;
        }

        // Use picking system to get us ordered list of all visually overlapping gameobjects in screen position from top to bottom
        private static IEnumerable<GameObject> GetAllOverlapping(Vector2 position)
        {
            var allOverlapping = new List<GameObject>();

            while (true)
            {
                var go = HandleUtility.PickGameObject(position, false, allOverlapping.ToArray());
                if (go == null)
                    break;

                // Prevent infinite loop if game object cannot be ignored when picking (This needs to fixed so print an error)
                if (allOverlapping.Count > 0 && go == allOverlapping.Last())
                {
                    Debug.LogError("GetAllOverlapping failed, could not ignore game object '" + go.name + "' when picking");
                    break;
                }

                yield return go;

                allOverlapping.Add(go);
            }
        }

        private static void UpdateHash(ref int hash, object obj)
        {
            hash = unchecked(hash * 33 + obj.GetHashCode());
        }
    }
}
