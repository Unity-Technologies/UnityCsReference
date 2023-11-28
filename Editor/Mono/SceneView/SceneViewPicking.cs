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

    class SceneViewPicking
    {
        static bool s_RetainHashes = false;
        static int s_PreviousTopmostHash = 0;
        static int s_PreviousPrefixHash = 0;
        static readonly List<PickingObject> s_ActiveObjectFilter = new List<PickingObject>(1);

        static SceneViewPicking()
        {
            Selection.selectionChanged += ResetHashes;
        }

        static void ResetHashes()
        {
            if (!s_RetainHashes)
            {
                s_PreviousTopmostHash = 0;
                s_PreviousPrefixHash = 0;
            }

            s_RetainHashes = false;
        }

        public static PickingObject PickGameObject(Vector2 mousePosition)
        {
            s_RetainHashes = true;

            var enumerator = GetAllOverlapping(mousePosition).GetEnumerator();

            if (!enumerator.MoveNext())
                return PickingObject.Empty;

            PickingObject topmost = enumerator.Current;
            var pickingBase = topmost.TryGetComponent(out Transform trs)
                ? HandleUtility.FindSelectionBaseForPicking(trs)
                : null;
            // Selection base is only interesting if it's not the topmost
            PickingObject selectionBase = new PickingObject(pickingBase);
            PickingObject first = selectionBase.target == null ? topmost : selectionBase;
            int topmostHash = topmost.GetHashCode();
            int prefixHash = topmostHash;

            if (Selection.activeObject == null)
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
                return Selection.activeObject == selectionBase.target ? topmost : first;
            }

            s_PreviousTopmostHash = topmostHash;

            // Pick potential selection base before topmost game object
            if (Selection.activeObject == selectionBase.target)
            {
                if (prefixHash == s_PreviousPrefixHash)
                    return topmost;
                s_PreviousPrefixHash = prefixHash;
                return selectionBase;
            }

            s_ActiveObjectFilter.Clear();
            s_ActiveObjectFilter.Add((PickingObject)Selection.activeObject);

            // Check if active game object will appear in selection stack
            PickingObject picked = HandleUtility.PickObject(mousePosition, false, null, s_ActiveObjectFilter);

            if (picked == ((PickingObject)Selection.activeObject))
            {
                // Advance enumerator to active game object
                while (enumerator.Current != ((PickingObject)Selection.activeObject))
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

        // Get an ordered list of all visually overlapping GameObjects at the screen position from top to bottom
        internal static IEnumerable<PickingObject> GetAllOverlapping(Vector2 position)
        {
            var overlapping = new List<PickingObject>();
            var ignore = new List<PickingObject>();

            while (true)
            {
                PickingObject res = HandleUtility.PickObject(position, false, ignore, null);

                if (res.target == null)
                    break;

                if (res.TryGetGameObject(out var go) && SceneVisibilityManager.instance.IsPickingDisabled(go))
                {
                    ignore.Add(res);
                    continue;
                }

                // Prevent infinite loop if object cannot be ignored (this needs to be fixed so print an error)
                if (overlapping.Count > 0 && res == overlapping.Last())
                {
                    Debug.LogError($"GetAllOverlapping failed, could not ignore game object '{res}' when picking");
                    break;
                }

                overlapping.Add(res);
                ignore.Add(res);

                yield return res;
            }
        }

        static void UpdateHash(ref int hash, PickingObject obj)
        {
            hash = unchecked(hash * 33 + obj.GetHashCode());
        }
    }
}
