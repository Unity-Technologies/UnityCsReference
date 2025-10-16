// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.DataModel;

internal abstract class DocumentReferenceRemapper
{
    private readonly DocumentModel Document;

    internal DocumentReferenceRemapper(DocumentModel document)
    {
        Document = document;
    }

    internal void RemapReferences()
    {
        foreach (ref readonly ObjectModel objectModel in Document.GetObjectModels())
        {
            Remap(objectModel.GetAccessor());

            // Remap components
            foreach (var componentAccessor in Document.GetEcsComponents(objectModel.ObjectId))
            {
                Remap(componentAccessor);
            }
        }
        RemapExternalReferences();
    }

    private void Remap(Accessor accessor)
    {
        Traversal.TraverseReferences(ref accessor, currentAccessor =>
        {
            RemapReference(currentAccessor);
        });
    }

    internal abstract void RemapReference(Accessor referenceAccessor);
    internal abstract void RemapExternalReferences();
}
