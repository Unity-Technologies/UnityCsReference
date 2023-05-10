// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor;

class ValidBlocksContainerPart : BlocksContainerPart
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidBlocksContainerPart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    public ValidBlocksContainerPart(string name, ContextNodeModel model, ModelView ownerElement, string parentClassName)
        : base(name, model, ownerElement, parentClassName) { }

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
        using (ContextNodeModel.GraphElementModels.OfTypeToPooledList(out List<BlockNodeModel> blockModels))
        {
            int count = blockModels.Count;
            for (int i = 0; i < count;)
            {
                if (blockModels[i] is IPlaceholder)
                {
                    blockModels.RemoveAt(i);
                    count--;
                }
                else
                    ++i;
            }

            using (m_BlocksContainer.Children().OfTypeToPooledList(out List<ModelView> blocks))
            {
                // Delete blocks that are no longer in the model
                if (UpdateBlocks(blockModels, blocks)) return;

                foreach (ModelView block in blocks)
                    block.UpdateFromModel();
            }
        }
    }
}
