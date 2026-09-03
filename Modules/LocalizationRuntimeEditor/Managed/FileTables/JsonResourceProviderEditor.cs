// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Localization.Providers.FileTables;

namespace Unity.Localization.Editor;

[AssetProviderEditor(typeof(JsonResourceProvider))]
class JsonResourceProviderEditor : FileTableProviderEditor
{
    public override ITableFileWriter Writer => JsonTableWriter.Instance;
}
