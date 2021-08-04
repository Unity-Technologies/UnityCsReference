// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditorInternal.InternalEditorUtility;

namespace UnityEditor
{
    /// <summary>
    /// An utility class that provides Dynamic Hints' functionalities
    /// </summary>
    internal static class DynamicHintUtility
    {
        internal static string Serialize(DynamicHintContent hint)
        {
            return $"[Custom Tooltip]<type>{hint.GetType().AssemblyQualifiedName}</type><content>{JsonUtility.ToJson(hint)}</content>";
        }

        internal static DynamicHintContent Deserialize(string hint)
        {
            if (!hint.StartsWith("[Custom Tooltip]")) { return null; }

            int typeStart = hint.IndexOf("<type>", StringComparison.InvariantCulture) + "<type>".Length;
            int typeLength = hint.IndexOf("</type>", StringComparison.InvariantCulture) - typeStart;
            if (typeStart < 0 || typeLength < 0) { return null; }

            var rawType = hint.Substring(typeStart, typeLength);
            var type = Type.GetType(rawType);

            int contentStart = hint.IndexOf("<content>", StringComparison.InvariantCulture) + "<content>".Length;
            int contentLength = hint.IndexOf("</content>", StringComparison.InvariantCulture) - contentStart;
            if (contentStart < 0 || contentLength < 0) { return null; }

            var content = hint.Substring(contentStart, contentLength);
            return JsonUtility.FromJson(content, type) as DynamicHintContent;
        }

        /// <summary>
        /// Loads the StyleSheet that is usually applied to DynamicHints that show how a tool or property works.
        /// </summary>
        /// <returns>The StyleSheet</returns>
        internal static StyleSheet GetDefaultDynamicHintStyleSheet()
        {
            return EditorGUIUtility.Load("StyleSheets/DynamicHints/DynamicHintCommon.uss") as StyleSheet;
        }

        /// <summary>
        /// Loads the StyleSheet that is usually applied to DynamicHints that represent data about the components of an instance of an object.
        /// </summary>
        /// <returns>The StyleSheet</returns>
        internal static StyleSheet GetDefaultInstanceDynamicHintStyleSheet()
        {
            return EditorGUIUtility.Load("StyleSheets/DynamicHints/InstanceDynamicHintCommon.uss") as StyleSheet;
        }

        /// <summary>
        /// Closes the current DynamicHint being displayed
        /// </summary>
        public static void CloseCurrentHint()
        {
            TooltipView.Close();
        }

        /// <summary>
        /// Draws a hint at a specific position of the screen.
        /// </summary>
        /// <param name="hint">The content of the hint</param>
        /// <param name="rect">The rect where the hint will be drawn. The top-left corner of the hint will start at the X coordinate</param>
        public static void DrawHintAt(DynamicHintContent hint, Rect rect)
        {
            TooltipView.s_ForceExtensionOfNextDynamicHint = true;
            TooltipView.Show(hint.ToTooltipString(), rect);
        }

        /// <summary>
        /// Draws a hint next to an object in the Hierarchy, if it exists and the Hierarchy is visible.
        /// </summary>
        /// <param name="hint">The content of the hint</param>
        /// <param name="objectInHierarchy">The object the hint will be drawn next to</param>
        public static void DrawHintNextToHierarchyObject(DynamicHintContent hint, GameObject objectInHierarchy)
        {
            if (!EditorWindow.HasOpenInstances<SceneHierarchyWindow>())
            {
                return;
            }

            EditorWindow.FocusWindowIfItsOpen<SceneHierarchyWindow>();

            Rect rectOfObject = GetRectOfObjectInHierarchy(objectInHierarchy, out bool objectWasFound);

            if (!objectWasFound) { return; }

            AdaptRectToHierarchy(out Rect rectOfHint, GUIUtility.GUIToScreenPoint(new Vector2(rectOfObject.x, rectOfObject.y)), rectOfObject);
            DrawHintAt(hint, rectOfHint);
        }

        /// <summary>
        /// Draws a hint next to a visible asset in the Project Window.
        /// </summary>
        /// <param name="hint">The content of hint tooltip</param>
        /// <param name="objectToFind">The object the hint will be drawn next to</param>
        public static void DrawHintNextToProjectAsset(DynamicHintContent hint, UnityEngine.Object objectToFind)
        {
            if (!EditorWindow.HasOpenInstances<ProjectBrowser>())
            {
                return;
            }

            EditorWindow.FocusWindowIfItsOpen<ProjectBrowser>();

            Rect rectOfObject = GetRectOfObjectInProjectExplorer(objectToFind, out bool objectWasFound);

            if (!objectWasFound) { return; }

            AdaptRectToProjectWindow(out Rect rectOfHint, GUIUtility.GUIToScreenPoint(new Vector2(rectOfObject.x, rectOfObject.y)));
            DrawHintAt(hint, rectOfHint);
        }

        /// <summary>
        /// Draws a hint next to a visible field in the Inspector.
        /// </summary>
        /// <param name="hint">The content of the hint</param>
        /// <param name="fieldPathInClass">The path of the field within the class</param>
        /// <param name="targetType">The type of the class that contains the field</param>
        public static void DrawHintNextToInspectorField(DynamicHintContent hint, string fieldPathInClass, Type targetType)
        {
            if (!EditorWindow.HasOpenInstances<InspectorWindow>())
            {
                return;
            }

            EditorWindow.FocusWindowIfItsOpen<InspectorWindow>();

            Rect rectOfObject = GetRectOfFieldInInspector(fieldPathInClass, targetType, out bool objectWasFound);

            if (!objectWasFound) { return; }

            AdaptRectToInspector(out Rect rectOfHint, GUIUtility.GUIToScreenPoint(new Vector2(rectOfObject.x, rectOfObject.y)));
            DrawHintAt(hint, rectOfHint);
        }

        /// <summary>
        /// Draws a hint at a Rect, if the mouse is in it.
        /// </summary>
        /// <param name="hint">The hint to draw</param>
        /// <param name="tooltipRect">The rect where the hint will be drawn</param>
        internal static void DrawMouseTooltip(DynamicHintContent hint, Rect tooltipRect)
        {
            GUIStyle.SetMouseTooltip(hint.ToTooltipString(), tooltipRect);
        }

        internal static void DrawHint(Rect hintTriggerRect, Vector2 mousePosition, AssetReference assetReference)
        {
            if (!hintTriggerRect.Contains(mousePosition) || !GUIClip.visibleRect.Contains(mousePosition)) { return; }

            string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.guid);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            var tooltipGenerators = TypeCache.GetMethodsWithAttribute<DynamicHintGeneratorAttribute>();

            ScriptableObject scriptableObjectAsset = asset as ScriptableObject;
            if (scriptableObjectAsset != null)
            {
                DynamicHintContent hint = GetDynamicHintContentOf(tooltipGenerators, scriptableObjectAsset);
                if (hint != null)
                {
                    DrawMouseTooltip(hint, hintTriggerRect);
                }
                return;
            }

            if (asset as GameObject != null)
            {
                /* GameObjects can have multiple components with custom tooltips
                 * so for now we'll just display the first one.
                 * If needed, we could:
                 * 1) Implement a "priority system" (like OrderedCallbacks)
                 * 2) Display one big tooltip made up with all elements from custom tooltip
                 */
                GameObject assetAsGameObject = asset as GameObject;
                foreach (Component component in assetAsGameObject.GetComponents<Component>())
                {
                    DynamicHintContent hint = GetDynamicHintContentOf(tooltipGenerators, component);
                    if (hint == null) { continue; }

                    DrawMouseTooltip(hint, hintTriggerRect);
                    return;
                }
            }
        }

        static DynamicHintContent GetDynamicHintContentOf<T>(TypeCache.MethodCollection creatorHandlers, T target)
        {
            if (target == null) //this means the script has been deleted and is "Missing"
            {
                return null;
            }

            Type targetType = target.GetType();
            foreach (var creationHandler in creatorHandlers)
            {
                foreach (var attribute in creationHandler.GetCustomAttributes(typeof(DynamicHintGeneratorAttribute), false))
                {
                    if (targetType == (attribute as DynamicHintGeneratorAttribute).m_Type)
                    {
                        return creationHandler.Invoke(null, new[] { (object)target }) as DynamicHintContent;
                    }
                }
            }
            return null;
        }

        static Rect GetRectOfObjectInHierarchy(GameObject objectToFind, out bool found)
        {
            return GetRectOfObjectInGUIView(nameof(SceneHierarchyWindow), objectToFind, out found);
        }

        static Rect GetRectOfObjectInProjectExplorer(UnityEngine.Object objectToFind, out bool found)
        {
            return GetRectOfObjectInGUIView(nameof(ProjectBrowser), objectToFind, out found);
        }

        static Rect GetRectOfFieldInInspector(string fieldPathInClass, Type targetType, out bool found)
        {
            string viewName = nameof(InspectorWindow);
            var allViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(allViews);

            var propertyInstructions = new List<IMGUIPropertyInstruction>(32);

            foreach (var view in allViews)
            {
                if (view.GetViewName() != viewName) { continue; } //todo: we should have a way to reference the window without using its name

                GUIViewDebuggerHelper.DebugWindow(view);
                view.RepaintImmediately();
                GUIViewDebuggerHelper.GetPropertyInstructions(propertyInstructions);
                var targetTypeName = targetType.AssemblyQualifiedName;

                foreach (var instruction in propertyInstructions) //todo: we should have a way to reference hierarchy objects without using their names
                {
                    if (instruction.targetTypeName == targetTypeName
                        && instruction.path == fieldPathInClass)
                    {
                        found = true;
                        return instruction.rect;
                    }
                }
                var drawInstructions = new List<IMGUIDrawInstruction>(32);
                GUIViewDebuggerHelper.GetDrawInstructions(drawInstructions);

                // Property instruction not found
                // Let's see if we can find any of the ancestor instructions to allow the user to unfold
                Rect regionRect = new Rect();
                found = FindAncestorPropertyRegion(fieldPathInClass, targetTypeName, drawInstructions, propertyInstructions, ref regionRect);
                return regionRect;
            }
            found = false;
            return Rect.zero;
        }

        /// <summary>
        /// Gets the field path of the parameter, relative to the class in which it is declared
        /// </summary>
        /// <param name="field"></param>
        /// <returns>The field path of the parameter, relative to the class in which it is declared</returns>
        public static string GetFieldPathInClass(object field)
        {
            if (field == null) { return string.Empty; }

            Type fieldType = field.GetType();
            string typeAsString = fieldType.ToString();
            bool isArray = typeAsString.EndsWithIgnoreCaseFast("[]]");
            if (isArray)
            {
                return fieldType.GetProperties()[0].Name + ".Array.size";
            }
            return fieldType.GetProperties()[0].Name;
        }

        /* todo: the following method seems to behave differently for properties representing Arrays, compared to how it behaves for non-array properties.
         * What happens is that regionRect becomes the "wrong side" of the property in the inspector */
        static bool FindAncestorPropertyRegion(string propertyPath, string targetTypeName,
            List<IMGUIDrawInstruction> drawInstructions, List<IMGUIPropertyInstruction> propertyInstructions,
            ref Rect regionRect)
        {
            while (true)
            {
                // Remove last component of property path
                var lastIndexOfDelimiter = propertyPath.LastIndexOf(".");
                if (lastIndexOfDelimiter < 1)
                {
                    // No components left, give up
                    return false;
                }
                propertyPath = propertyPath.Substring(0, lastIndexOfDelimiter);
                foreach (var instruction in propertyInstructions)
                {
                    if (instruction.targetTypeName != targetTypeName || instruction.path != propertyPath) { continue; }
                    regionRect = instruction.rect;

                    // The property rect itself does not contain the foldout arrow
                    // Expand region to include all draw instructions for this property
                    var unifiedInstructions = new List<IMGUIInstruction>(128);
                    GUIViewDebuggerHelper.GetUnifiedInstructions(unifiedInstructions);
                    var collectDrawInstructions = false;
                    var propertyBeginLevel = 0;
                    foreach (var unifiedInstruction in unifiedInstructions)
                    {
                        if (collectDrawInstructions)
                        {
                            if (unifiedInstruction.level <= propertyBeginLevel) { break; }
                            if (unifiedInstruction.type != InstructionType.kStyleDraw) { continue; }

                            var drawRect = drawInstructions[unifiedInstruction.typeInstructionIndex].rect;
                            if (drawRect.xMin < regionRect.xMin)
                            {
                                regionRect.xMin = drawRect.xMin;
                            }
                            if (drawRect.yMin < regionRect.yMin)
                            {
                                regionRect.yMin = drawRect.yMin;
                            }
                            if (drawRect.xMax > regionRect.xMax)
                            {
                                regionRect.xMax = drawRect.xMax;
                            }
                            if (drawRect.yMax > regionRect.yMax)
                            {
                                regionRect.yMax = drawRect.yMax;
                            }
                        }
                        else
                        {
                            if (unifiedInstruction.type != InstructionType.kPropertyBegin) { continue; }

                            var propertyInstruction = propertyInstructions[unifiedInstruction.typeInstructionIndex];
                            if (propertyInstruction.targetTypeName == targetTypeName
                                && propertyInstruction.path == propertyPath)
                            {
                                collectDrawInstructions = true;
                                propertyBeginLevel = unifiedInstruction.level;
                            }
                        }
                    }

                    return true;
                }
            }
        }

        static Rect GetRectOfObjectInGUIView(string viewName, UnityEngine.Object objectToFind, out bool found)
        {
            var allViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(allViews);

            var drawInstructions = new List<IMGUIDrawInstruction>(32);
            foreach (var view in allViews)
            {
                if (view.GetViewName() != viewName) { continue; } //todo: we should have a way to reference the window without using its name

                GUIViewDebuggerHelper.DebugWindow(view);
                view.RepaintImmediately();
                GUIViewDebuggerHelper.GetDrawInstructions(drawInstructions);
                foreach (var drawInstruction in drawInstructions) //todo: we should have a way to reference hierarchy objects without using their names
                {
                    //If we can reference the object represented by the draw instruction, we can find it like this
                    /*if (drawInstruction.usedGUIContent.representedObject != null)
                    {
                        if (drawInstruction.usedGUIContent.representedObject == objectToFind)
                        {
                            found = true;
                            return drawInstruction.rect;
                        }
                    }*/

                    if (drawInstruction.usedGUIContent.text != objectToFind.name) { continue; }
                    found = true;
                    return drawInstruction.rect;
                }
                found = false;
                return Rect.zero;
            }
            found = false;
            return Rect.zero;
        }

        static void AdaptRectToHierarchy(out Rect rectOfHint, Vector2 screenPositionOfObject, Rect rectOfObject)
        {
            rectOfHint = new Rect();
            rectOfHint.x = screenPositionOfObject.x + rectOfObject.width;
            rectOfHint.y = screenPositionOfObject.y;
            rectOfHint.width = 0;
            rectOfHint.height = 0;
        }

        static void AdaptRectToInspector(out Rect rectOfHint, Vector2 screenPositionOfObject)
        {
            rectOfHint = new Rect();
            rectOfHint.x = screenPositionOfObject.x;
            rectOfHint.y = screenPositionOfObject.y;
            rectOfHint.width = 0;
            rectOfHint.height = 0;
        }

        static void AdaptRectToProjectWindow(out Rect rectOfHint, Vector2 screenPositionOfObject)
        {
            const int offsetForImprovedPositioning = 200;

            rectOfHint = new Rect();
            rectOfHint.x = screenPositionOfObject.x + offsetForImprovedPositioning;
            rectOfHint.y = screenPositionOfObject.y;
            rectOfHint.width = 0;
            rectOfHint.height = 0;
        }
    }
}
