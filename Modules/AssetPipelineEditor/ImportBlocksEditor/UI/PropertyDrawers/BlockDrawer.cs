// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    [CustomPropertyDrawer(typeof(Block))]
    internal class Block_PropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Inside the block list the framework header (BlockItemElement) owns the block's name and
            // enabled state, so the body renders bare fields with the infrastructure fields hidden.
            // Elsewhere the block is an ordinary nested field and keeps its labeled presentation.
            if (IsBlockListElement(property))
                return UiHelpers.CreateDefaultContent(property);

            var root = new VisualElement();

            if (ShouldFlattenSingleArrayChild(property, out var arrayChild))
            {
                root.Add(new PropertyField(arrayChild, GetDisplayName(property)));
            }
            else
            {
                var displayName = GetDisplayName(property);
                var propertyField = new PropertyField(property, displayName);
                propertyField.style.marginLeft = 12;
                root.Add(propertyField);
            }

            return root;
        }

        /// <summary>
        /// Determines if a property has exactly one child that is an array
        /// </summary>
        static bool ShouldFlattenSingleArrayChild(SerializedProperty property, out SerializedProperty arrayChild)
        {
            arrayChild = null;

            if (!property.hasVisibleChildren)
                return false;

            var copy = property.Copy();
            var endProperty = copy.GetEndProperty(false);

            int childCount = 0;
            if (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, endProperty))
            {
                childCount = 1;
                while (copy.NextVisible(false) && !SerializedProperty.EqualContents(copy, endProperty))
                {
                    childCount++;
                }
            }

            // If exactly one child, check if it's an array
            if (childCount == 1)
            {
                var child = property.Copy();
                if (child.NextVisible(true) && child.depth == property.depth + 1)
                {
                    if (child.isArray && child.propertyType != SerializedPropertyType.String)
                    {
                        arrayChild = child;
                        return true;
                    }
                }
            }

            return false;
        }

        // True when the property is an element of a BlockCollection's m_Blocks list — the row whose
        // header BlockItemElement draws. A child field of an element has a '.' after the marker and
        // is not itself a row. The path shape alone would also match a user-defined field that
        // happens to be named m_Blocks, so the list's owner must actually be a BlockCollection.
        internal static bool IsBlockListElement(SerializedProperty property)
        {
            const string marker = ".m_Blocks.Array.data[";
            var path = property.propertyPath;
            var index = path.LastIndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
                return false;
            if (path.IndexOf('.', index + marker.Length) >= 0)
                return false;

            var ownerProperty = property.serializedObject.FindProperty(path.Substring(0, index));
            if (ownerProperty == null)
                return false;

            ScriptAttributeUtility.GetFieldInfoFromProperty(ownerProperty, out var ownerType);
            return ownerType != null
                && ownerType.IsGenericType
                && ownerType.GetGenericTypeDefinition() == typeof(BlockCollection<>);
        }

        internal static string GetDisplayName(SerializedProperty property)
        {
            if (property.managedReferenceValue is Block block)
            {
                return block.Name;
            }

            // Fallback to property display name if not a Block
            return property.displayName;
        }
    }
}
