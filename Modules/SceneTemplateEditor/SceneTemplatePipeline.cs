// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.SceneManagement;

namespace UnityEditor.SceneTemplate
{
    public interface ISceneTemplatePipeline
    {
        bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset);
        void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName);
        void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName);
    }

    public class SceneTemplatePipelineAdapter : ISceneTemplatePipeline
    {
        public virtual bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
        {
            return true;
        }

        public virtual void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName) {}

        public virtual void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName) {}
    }
}
