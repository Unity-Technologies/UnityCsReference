// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEngine
{
    ///<summary>This element can filter raycasts. If the top level element is hit it can further 'check' if the location is valid.</summary>
    public interface ICanvasRaycastFilter
    {
        ///<summary>Given a point and a camera is the raycast valid.</summary>
        ///<param name="sp">Screen position.</param>
        ///<param name="eventCamera">Raycast camera.</param>
        ///<returns>Valid.</returns>
        bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera);
    }

    ///<summary>A <see cref="Canvas" /> placable element that can be used to modify children Alpha, Raycasting, Enabled state.</summary>
    ///<remarks>A canvas group can be used to modify the state of children elements.
    ///
    ///An example of this would be a window which fades in over time, by modifying the alpha value of the group the children elements will be affected. The result alpha will be the multiplied result of any nested groups, multiplied with the canvas elements alpha.
    ///
    ///You can configure Canvas Groups to not block raycasts. When you configure a Canvas Group to not block raycasts, graphic raycasting ignores anything in the group.
    ///
    ///Let's say you have a Canvas GameObject with a CanvasGroup component on it, and you set the CanvasGroup component's alpha to 0. In that case, the Canvas does not render any of its child GameObjects.
    ///Now suppose that the Canvas also has a child CanvasGroup GameObject that you do want to render. If you enable IgnoreParentGroups for the CanvasGroup GameObject, the parent Canvas does not render any of its child GameObjects, including the CanvasGroup you want to render.
    ///To get the child CanvasGroup GameObject, do one of two things:
    ///In the parent Canvas, set the CanvasGroup component's alpha to a small, non-zero value.
    ///Add a Canvas component to the child CanvasGroup GameObject that you want to render.</remarks>
    [NativeClass("UI::CanvasGroup", PersistentTypeId = 225),
     NativeHeader("Modules/UI/CanvasGroup.h")]
    [UIModuleHelpURL("class-CanvasGroup")]
    public sealed class CanvasGroup : Behaviour, ICanvasRaycastFilter
    {
        internal CanvasGroup(global::UnityEngine.EntityId id) : base(id) {}
        public CanvasGroup() {}
        ///<summary>Set the alpha of the group.</summary>
        [NativeProperty("Alpha", false, TargetType.Function)] public extern float alpha { get; set; }
        ///<summary>Is the group interactable (are the elements beneath the group enabled).</summary>
        [NativeProperty("Interactable", false, TargetType.Function)] public extern bool interactable { get; set; }
        ///<summary>Does this group block raycasting (allow collision).</summary>
        [NativeProperty("BlocksRaycasts", false, TargetType.Function)] public extern bool blocksRaycasts { get; set; }
        ///<summary>Should the group ignore parent groups?</summary>
        ///<remarks>If set to true the group will ignore any parent group settings.</remarks>
        [NativeProperty("IgnoreParentGroups", false, TargetType.Function)] public extern bool ignoreParentGroups { get; set; }

        ///<summary>Returns true if the Group allows raycasts.</summary>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            return blocksRaycasts;
        }
    }
}
