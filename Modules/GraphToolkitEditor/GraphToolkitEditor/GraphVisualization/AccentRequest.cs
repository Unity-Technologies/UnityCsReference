// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

abstract class AccentRequest
{
    public enum Group
    {
        Static,
        Animation
    }

    public Hash128 TargetID { get; protected set; }

    public abstract Group requestGroup { get; }
}

class PlayLoopAnimationRequest : AccentRequest
{
    public override Group requestGroup => Group.Animation;

    public float animationSpeed;

    public PlayLoopAnimationRequest(Hash128 targetID, float animationSpeed)
    {
        TargetID = targetID;
        this.animationSpeed = animationSpeed;
    }
}

class StopLoopAnimationRequest : AccentRequest
{
    public override Group requestGroup => Group.Animation;

    public StopLoopAnimationRequest(Hash128 target)
    {
        TargetID = target;
    }
}

class FillAmountRequest : AccentRequest
{
    public override Group requestGroup => Group.Static;

    public float amount;

    public FillAmountRequest(Hash128 target, float amount)
    {
        TargetID = target;
        this.amount = amount;
    }
}

// TODO: Unused; should this be removed?
class AccentData
{
    public Hash128 NodeID { get; private set; }
    public AccentRequest Request { get; private set; }

    public AccentData(Hash128 nodeID, AccentRequest request)
    {
        NodeID = nodeID;
        Request = request;
    }
}
