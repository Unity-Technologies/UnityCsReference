// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for onboarding providers, which displays the UI when there is no active graph.
    /// </summary>
    [UnityRestricted]
    internal abstract class OnboardingProvider
    {
        /// <summary>
        /// The USS class name added to a block of the <see cref="OnboardingProvider"/>.
        /// </summary>
        public static readonly string onboardingBlockUssClassName = "onboarding-block";

        /// <summary>
        /// The USS class name added to the title of the <see cref="OnboardingProvider"/>.
        /// </summary>
        public static readonly string onboardingTitleUssClassName = "onboarding-title";

        /// <summary>
        /// The USS class name added to the button container of the <see cref="OnboardingProvider"/>.
        /// </summary>
        public static readonly string onboardingBlockContainerUssClassName = "onboarding-button-container";

        protected VisualElement m_ButtonContainer;

        /// <summary>
        /// The default text on buttons used for graph creation.
        /// </summary>
        protected const string k_ButtonText = "Create new {0}";

        /// <summary>
        /// The default message for the blank page referring to the drag and drop assets functionality.
        /// </summary>
        protected const string k_DnDAssetText = "Drag and drop any {0} graph object here";

        /// <summary>
        /// The <see cref="VisualElement"/> containing the buttons for loading and creating graphs. Use this container when adding new buttons.
        /// </summary>
        protected virtual VisualElement ButtonContainer => m_ButtonContainer ??= new VisualElement();

        /// <summary>
        /// Gets all graph object types that can be loaded into the window when there is no active graph.
        /// </summary>
        /// <returns>The accepted graph object types.</returns>
        public abstract IReadOnlyList<Type> GetAcceptedGraphObjectTypes();

        /// <summary>
        /// Creates a button to browse for a graph object.
        /// </summary>
        /// <param name="commandTarget">The target of the command to create the graph.</param>
        /// <param name="acceptedGraphTypes">The accepted graph types.</param>
        /// <returns>The button.</returns>
        protected static VisualElement AddBrowseGraphsButton(ICommandTarget commandTarget, IReadOnlyList<Type> acceptedGraphTypes)
        {
            const string buttonText = "Browse to open an graph object";

            var searchQuery = "";
            var acceptedGraphTypesStr = "";
            if (acceptedGraphTypes.Count > 0)
            {
                searchQuery = "t:" + acceptedGraphTypes[0];
                acceptedGraphTypesStr = acceptedGraphTypes[0].ToString();
                for (var i = 0; i < acceptedGraphTypes.Count; i++)
                {
                    if (i == 0)
                        continue;

                    searchQuery += " or t:" + acceptedGraphTypes[i];

                    if (i == acceptedGraphTypes.Count - 1)
                        acceptedGraphTypesStr += " or " + acceptedGraphTypes[i];
                    else
                        acceptedGraphTypesStr += ", " + acceptedGraphTypes[i];
                }
            }

            var unsupportedTypeMessage = $"File type not supported, please select a {acceptedGraphTypesStr} graph object";

            var container = new VisualElement();
            container.AddToClassList(onboardingBlockUssClassName);
            var button = new Button { text = buttonText };
            button.clicked += () =>
            {
                SearchService.ShowObjectPicker(
                    (pickedObject, wasClosed) =>
                    {
                        if (!wasClosed)
                        {
                            var graphObject = pickedObject as GraphObject;
                            if (graphObject == null)
                                EditorUtility.DisplayDialog("", unsupportedTypeMessage, "Close");
                            else if (graphObject != null && graphObject.GraphModel != null)
                                commandTarget.Dispatch(new LoadGraphCommand(graphObject.GraphModel));
                        }
                    }, null, searchQuery, null, typeof(GraphObject), flags: SearchFlags.OpenPicker);
            };
            container.Add(button);

            return container;
        }

        /// <summary>
        /// Creates a button to browse for graph objects that can be loaded in the window.
        /// </summary>
        /// <param name="commandTarget">The target of the command to create the graph.</param>
        /// <param name="acceptedGraphTypes">The accepted graph types.</param>
        /// <param name="fileExtensions">The accepted file extensions.</param>
        /// <returns>The button.</returns>
        protected static VisualElement AddBrowseGraphsButtonForForeignAssets(ICommandTarget commandTarget, IReadOnlyList<Type> acceptedGraphTypes, IReadOnlyCollection<string> fileExtensions)
        {
            const string buttonText = "Browse to open a graph";

            var searchQuery = new StringBuilder();
            var acceptedGraphTypesStr = "";

            if (acceptedGraphTypes.Count > 0)
            {
                searchQuery.Append("t:" + acceptedGraphTypes[0]);
                acceptedGraphTypesStr = acceptedGraphTypes[0].ToString();

                for (var i = 1; i < acceptedGraphTypes.Count; i++)
                {
                    searchQuery.Append(" or t:" + acceptedGraphTypes[i]);

                    if (i == acceptedGraphTypes.Count - 1)
                        acceptedGraphTypesStr += " or " + acceptedGraphTypes[i];
                    else
                        acceptedGraphTypesStr += ", " + acceptedGraphTypes[i];
                }
            }

            if (fileExtensions is { Count: > 0 })
            {
                if (searchQuery.Length > 0)
                    searchQuery.Append(" or ");

                var firstExtension = true;
                foreach (var extension in fileExtensions)
                {
                    if (!firstExtension)
                        searchQuery.Append(" or ");
                    else
                        firstExtension = false;

                    searchQuery.Append("ext:" + extension);
                }
            }

            var container = new VisualElement();
            container.AddToClassList(onboardingBlockUssClassName);
            var button = new Button { text = buttonText };
            button.clicked += () =>
            {
                SearchService.ShowObjectPicker(
                    (pickedObject, wasClosed) =>
                    {
                        if (!wasClosed)
                        {
                            var filePath = AssetDatabase.GetAssetPath(pickedObject);

                            var graphObject = GraphObject.LoadGraphObjectAtPath(filePath);
                            if (graphObject != null)
                            {

                                bool typeSupported = false;
                                foreach (var acceptedGraphType in acceptedGraphTypes)
                                {
                                    if (acceptedGraphType.IsAssignableFrom(graphObject.GetType()))
                                    {
                                        typeSupported = true;
                                        break;
                                    }
                                }
                                if (typeSupported)
                                    commandTarget.Dispatch(new LoadGraphCommand(graphObject.GraphModel));
                            }
                        }
                    }, null, searchQuery.ToString(), null, typeof(Object), flags: SearchFlags.OpenPicker);
            };
            container.Add(button);

            return container;
        }

        /// <summary>
        /// Creates a button to create a graph.
        /// </summary>
        /// <param name="commandTarget">The target of the command to create the graph.</param>
        /// <param name="template">The template of the graph.</param>
        /// <param name="buttonText">The text on the button.</param>
        /// <param name="promptTitle">The title on the prompt that shows up when creating the graph.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <param name="createGraphAction">A method called when pressing on the button to create the graph, if any.</param>
        /// <returns>The button.</returns>
        protected static VisualElement AddNewGraphButton<T>(
            ICommandTarget commandTarget,
            GraphTemplate template,
            string promptTitle = null,
            string buttonText = null,
            string prompt = null,
            Action createGraphAction = null) where T : GraphObject
        {
            return AddNewGraphButton(typeof(T), commandTarget, template, promptTitle, buttonText, prompt, createGraphAction);
        }

        protected static VisualElement AddNewGraphButton(
            Type graphAssetType,
            ICommandTarget commandTarget,
            GraphTemplate template,
            string promptTitle = null,
            string buttonText = null,
            string prompt = null,
            Action createGraphAction = null)
        {
            buttonText ??= string.Format(k_ButtonText, template.GraphTypeName);

            var container = new VisualElement();
            container.AddToClassList(onboardingBlockUssClassName);

            var button = new Button { text = buttonText };
            if (createGraphAction == null)
            {
                button.clicked += () =>
                {
                    var graphObject = GraphObjectCreationHelpers.PromptToCreateGraphObject(graphAssetType, template, promptTitle, prompt);
                    if (graphObject != null)
                        commandTarget.Dispatch(new LoadGraphCommand(graphObject.GraphModel));
                };
            }
            else
            {
                button.clicked += createGraphAction;
            }
            container.Add(button);

            return container;
        }

        /// <summary>
        /// Creates a dropdown button for users to select from a range of options to create a graph.
        /// </summary>
        /// <param name="dropdownItems">The list of <see cref="DropdownButton.MenuItem"/>.</param>
        /// <returns>The dropdown button.</returns>
        protected static VisualElement AddNewGraphDropdownButton(DropdownButton.MenuItem[] dropdownItems)
        {
            var container = new VisualElement();
            container.AddToClassList(onboardingBlockUssClassName);

            var dropdownButton = new DropdownButton(dropdownItems);
            container.Add(dropdownButton);

            return container;
        }

        /// <summary>
        /// Creates the elements, typically buttons, for initiating graph creation on the onboarding page.
        /// </summary>
        /// <param name="commandTarget">The target of the commands.</param>
        /// <returns>The container holding the created elements.</returns>
        protected abstract VisualElement CreateOnboardingElements(ICommandTarget commandTarget);

        /// <summary>
        /// The title of the graph displayed on the onboarding page title.
        /// </summary>
        /// <returns>The title.</returns>
        protected abstract string GetGraphName();

        /// <summary>
        /// Creates the title of the onboarding page.
        /// </summary>
        /// <returns>The <see cref="VisualElement"/> containing the title.</returns>
        protected VisualElement CreateOnboardingTitle()
        {
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList(onboardingTitleUssClassName);

            titleContainer.Add(new Label { text = string.Format(k_DnDAssetText, GetGraphName()) });
            titleContainer.Add(new Label { text = "or" });

            return titleContainer;
        }

        /// <summary>
        /// Creates the initial onboarding elements.
        /// </summary>
        /// <param name="commandTarget">The target of the commands.</param>
        /// <returns>The <see cref="VisualElement"/> containing the onboarding elements.</returns>
        internal VisualElement CreateInitialOnboardingElements(ICommandTarget commandTarget)
        {
            var onboardingContainer = new VisualElement();

            // Add title
            onboardingContainer.Add(CreateOnboardingTitle());

            // Add buttons
            ButtonContainer.AddToClassList(onboardingBlockContainerUssClassName);


            var acceptedGraphObjectTypes = GetAcceptedGraphObjectTypes();
            if (acceptedGraphObjectTypes.Count > 0)
            {
                var extensionsForAssetsTypes = new HashSet<string>();

                BlankPage.GetExtensionsForAssetTypes(acceptedGraphObjectTypes, extensionsForAssetsTypes);

                if (extensionsForAssetsTypes.Count == 0)
                {
                    ButtonContainer.Add(AddBrowseGraphsButton(commandTarget, GetAcceptedGraphObjectTypes()));
                }
                else
                {
                    ButtonContainer.Add(AddBrowseGraphsButtonForForeignAssets(commandTarget, acceptedGraphObjectTypes, extensionsForAssetsTypes));
                }
            }

            onboardingContainer.Add(CreateOnboardingElements(commandTarget));

            return onboardingContainer;
        }
    }
}
