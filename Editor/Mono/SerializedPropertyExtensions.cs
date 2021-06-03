// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Assertions;

namespace UnityEditor
{
    internal static class SerializedPropertyExtensions
    {
        public static bool TryGetMapEntry(this SerializedProperty map, string key, out SerializedProperty entry)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            for (int i = 0; i < map.arraySize; ++i)
            {
                var element = map.GetArrayElementAtIndex(i);
                var first = element.FindPropertyRelative("first").stringValue;
                if (first == key)
                {
                    entry = element;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        public static bool TryGetMapEntry(this SerializedProperty map, int key, out SerializedProperty entry)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            for (int i = 0; i < map.arraySize; ++i)
            {
                var element = map.GetArrayElementAtIndex(i);
                var first = element.FindPropertyRelative("first").intValue;
                if (first == key)
                {
                    entry = element;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        public static bool TryGetMapEntry(this SerializedProperty map, string key, string elementMapKey, out SerializedProperty entry)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var element))
            {
                var innerMap = element.FindPropertyRelative("second");
                Assert.IsTrue(innerMap.type == "map");
                return innerMap.TryGetMapEntry(elementMapKey, out entry);
            }
            entry = null;
            return false;
        }

        public static void SetMapValue(this SerializedProperty map, string key, int value)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var entry))
            {
                entry.FindPropertyRelative("second").intValue = value;
            }
            else
            {
                var index = map.arraySize;
                map.arraySize += 1;
                var element = map.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("first").stringValue = key;
                element.FindPropertyRelative("second").intValue = value;
            }
        }

        public static void SetMapValue(this SerializedProperty map, int key, string value)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var entry))
            {
                entry.FindPropertyRelative("second").stringValue = value;
            }
            else
            {
                var index = map.arraySize;
                map.arraySize += 1;
                var element = map.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("first").intValue = key;
                element.FindPropertyRelative("second").stringValue = value;
            }
        }

        public static void SetMapValue(this SerializedProperty map, int key, string[] value)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var entry))
            {
                var secondProperty = entry.FindPropertyRelative("second");
                Assert.IsTrue(secondProperty.isArray);

                secondProperty.arraySize = value.Length;
                for (int i = 0; i < secondProperty.arraySize; ++i)
                {
                    secondProperty.GetArrayElementAtIndex(i).stringValue = value[i];
                }
            }
            else
            {
                var index = map.arraySize;
                map.arraySize += 1;
                var element = map.GetArrayElementAtIndex(index);

                element.FindPropertyRelative("first").intValue = key;
                var secondProperty = element.FindPropertyRelative("second");

                secondProperty.arraySize = value.Length;
                for (int i = 0; i < secondProperty.arraySize; ++i)
                {
                    secondProperty.GetArrayElementAtIndex(i).stringValue = value[i];
                }
            }
        }

        public static void SetMapValue(this SerializedProperty map, string key, string value)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var entry))
            {
                entry.FindPropertyRelative("second").stringValue = value;
            }
            else
            {
                var index = map.arraySize;
                map.arraySize += 1;
                var element = map.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("first").stringValue = key;
                element.FindPropertyRelative("second").stringValue = value;
            }
        }

        public static void SetMapValue(this SerializedProperty map, string key, string[] value)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var entry))
            {
                var secondProperty = entry.FindPropertyRelative("second");
                Assert.IsTrue(secondProperty.isArray);

                secondProperty.arraySize = value.Length;
                for (int i = 0; i < secondProperty.arraySize; ++i)
                {
                    secondProperty.GetArrayElementAtIndex(i).stringValue = value[i];
                }
            }
            else
            {
                var index = map.arraySize;
                map.arraySize += 1;
                var element = map.GetArrayElementAtIndex(index);

                element.FindPropertyRelative("first").stringValue = key;
                var secondProperty = element.FindPropertyRelative("second");

                secondProperty.arraySize = value.Length;
                for (int i = 0; i < secondProperty.arraySize; ++i)
                {
                    secondProperty.GetArrayElementAtIndex(i).stringValue = value[i];
                }
            }
        }

        public static void SetMapValue(this SerializedProperty map, string key, string elementMapKey, string elementMapValue)
        {
            Assert.IsTrue(map.type == "map" && !map.serializedObject.isEditingMultipleObjects);
            if (map.TryGetMapEntry(key, out var entry))
            {
                var innerMap = entry.FindPropertyRelative("second");
                Assert.IsTrue(innerMap.type == "map");
                innerMap.SetMapValue(elementMapKey, elementMapValue);
            }
            else
            {
                var index = map.arraySize;
                map.arraySize += 1;
                var element = map.GetArrayElementAtIndex(index);
                var innerMap = element.FindPropertyRelative("second");
                Assert.IsTrue(innerMap.type == "map");
                element.FindPropertyRelative("first").stringValue = key;
                innerMap.SetMapValue(elementMapKey, elementMapValue);
            }
        }
    }
}
