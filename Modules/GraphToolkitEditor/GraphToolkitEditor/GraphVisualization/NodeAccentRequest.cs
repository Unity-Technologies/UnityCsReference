// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

abstract class NodeAccentRequest
{
    public enum Group
    {
        Static,
        Animation
    }

    public Hash128 NodeID { get; protected set; }

    public abstract Group requestGroup { get; }
}

class PlayLoopAnimationRequest : NodeAccentRequest
{
    public override Group requestGroup => Group.Animation;

    public float animationSpeed;

    public PlayLoopAnimationRequest(Hash128 nodeID, float animationSpeed)
    {
        this.NodeID = nodeID;
        this.animationSpeed = animationSpeed;
    }
}

class StopLoopAnimationRequest : NodeAccentRequest
{
    public override Group requestGroup => Group.Animation;

    public StopLoopAnimationRequest(Hash128 target)
    {
        NodeID = target;
    }
}

class FillAmountRequest : NodeAccentRequest
{
    public override Group requestGroup => Group.Static;

    public float amount;

    public FillAmountRequest(Hash128 target, float amount)
    {
        NodeID = target;
        this.amount = amount;
    }
}

class NodeAccentData
{
    public Hash128 NodeID { get; private set; }
    public NodeAccentRequest Request { get; private set; }

    public NodeAccentData(Hash128 nodeID, NodeAccentRequest request)
    {
        NodeID = nodeID;
        Request = request;
    }
}
