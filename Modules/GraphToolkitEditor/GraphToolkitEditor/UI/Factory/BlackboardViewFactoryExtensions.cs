// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    [GraphElementsExtensionMethodsCache(typeof(BlackboardView), GraphElementsExtensionMethodsCacheAttribute.k_LowestPriority)]
    [UnityRestricted]
    internal static class BlackboardViewFactoryExtensions
    {
        /// <summary>
        /// Creates the appropriate <see cref="ModelView"/> for the given <see cref="VariableDeclarationModelBase"/>.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="VariableDeclarationModelBase"/> for which an <see cref="ModelView"/> is required.</param>
        /// <returns>A <see cref="ModelView"/> for the given <see cref="VariableDeclarationModelBase"/>.</returns>
        public static ModelView CreateVariableDeclarationModelView(this ElementBuilder elementBuilder, VariableDeclarationModelBase model)
        {
            ModelView ui;

            if (elementBuilder.Context == BlackboardCreationContext.VariablePropertyCreationContext)
            {
                ui = new BlackboardVariablePropertyView();
            }
            else if (elementBuilder.Context == BlackboardCreationContext.VariableCreationContext)
            {
                ui = new BlackboardField();
            }
            else
            {
                ui = new BlackboardRow();
            }

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a blackboard from for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="BlackboardContentModel"/> this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/>.</returns>
        public static ModelView CreateBlackboard(this ElementBuilder elementBuilder, BlackboardContentModel model)
        {
            var ui = new Blackboard();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a <see cref="BlackboardGroup"/> from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="GroupModel"/> this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/>.</returns>
        public static ModelView CreateGroup(this ElementBuilder elementBuilder, GroupModel model)
        {
            ModelView ui = new BlackboardGroup();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a <see cref="BlackboardSection"/> for the given model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The <see cref="SectionModel"/> this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/>.</returns>
        public static ModelView CreateSection(this ElementBuilder elementBuilder, SectionModel model)
        {
            ModelView ui = new BlackboardSection();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
