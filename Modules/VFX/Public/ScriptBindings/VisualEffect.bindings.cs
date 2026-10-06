// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.VFX;
using UnityEngine.Scripting;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VFX
{
    ///<summary>This structure describes an exposed property on a <see cref="VisualEffectAsset" />.</summary>
    ///<remarks>
    ///  <para />
    ///  <para>This example logs detailed information regarding all exposed properties.</para>
    ///</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogExposedProperties.cs}]]></code>
    ///</example>
    ///<seealso cref="VisualEffectAsset.GetExposedProperties" />
    ///<seealso cref="VisualEffectAsset.GetExposedSpace" />
    ///<seealso cref="VisualEffectAsset.GetTextureDimension" />
    [UsedByNativeCode]
    public struct VFXExposedProperty
    {
        ///<summary>The name of the exposed property.</summary>
        public string name;
        ///<summary>The type of the exposed property.</summary>
        public Type type;
    }

    ///<summary>This class is the base for <see cref="VFX.VisualEffectAsset" /> and VisualEffectSubgraph.</summary>
    ///<seealso cref="VFX.VisualEffectAsset" />
    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VisualEffectAssetBindings.h")]
    [NativeHeader("Modules/VFX/Public/VisualEffectAsset.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    [NativeClass("VisualEffectObject", PersistentTypeId = 0x7AC43185)]
    public abstract class VisualEffectObject : Object
    {
        protected VisualEffectObject() {}
        protected internal VisualEffectObject(global::UnityEngine.EntityId id) : base(id) {}
    }

    ///<summary>This class contains a graph of the elements needed to describe a visual effect. These include: the visual effects system, generated shaders, and compiled data.</summary>
    ///<seealso cref="VFX.VisualEffect" />
    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/VisualEffectAsset.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    [NativeClass("VisualEffectAsset", PersistentTypeId = 0x7AB43185)]
    public class VisualEffectAsset : VisualEffectObject
    {
        internal VisualEffectAsset(global::UnityEngine.EntityId id) : base(id) {}
        public VisualEffectAsset() {}
        ///<summary>The default name of the play event.</summary>
        ///<seealso cref="VFX.VisualEffectAsset.PlayEventID" />
        public const string PlayEventName = "OnPlay";
        ///<summary>The default name of the stop event.</summary>
        ///<seealso cref="VFX.VisualEffectAsset.StopEventID" />
        public const string StopEventName = "OnStop";
        ///<summary>The default name ID of the play event.</summary>
        ///<seealso cref="VFX.VisualEffectAsset.PlayEventName" />
        public static readonly int PlayEventID = Shader.PropertyToID(PlayEventName);
        ///<summary>The default name ID of the stop event.</summary>
        ///<seealso cref="VFX.VisualEffectAsset.StopEventName" />
        public static readonly int StopEventID = Shader.PropertyToID(StopEventName);
        static internal extern uint currentRuntimeDataVersion { get; }

        internal extern VFXInstancingMode instancingMode { get; set; }
        ///<summary>Gets the <see cref="Rendering.TextureDimension" /> of a named exposed Texture.</summary>
        ///<remarks>
        ///
        ///This example logs detailed information regarding all exposed properties:</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogExposedProperties.cs}]]></code>
        ///</example>
        ///<seealso cref="VisualEffectAsset.GetExposedProperties" />
        ///<seealso cref="VisualEffectAsset.GetExposedSpace" />
        ///<seealso cref="VisualEffectAsset.GetTextureDimension" />
        [FreeFunction(Name = "VisualEffectAssetBindings::GetTextureDimension", HasExplicitThis = true)] extern public UnityEngine.Rendering.TextureDimension GetTextureDimension(int nameID);
        ///<summary>Provides the configured space of an exposed property in VisualEffectAsset.</summary>
        ///<remarks>
        ///  <para>VFXSpace.None will be returned if the property doesn't exist or isn't spaceable.
        ///
        ///The <see cref="VFX.VisualEffect.SetVector3" /> won't apply any automatic transform, because <c>VisualEffect</c> expects raw values.
        ///
        ///</para>
        ///  <para>This example logs detailed information regarding all exposed properties.</para>
        ///</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The expected space of the property.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogExposedProperties.cs}]]></code>
        ///</example>
        ///<seealso cref="VisualEffectAsset.GetExposedProperties" />
        ///<seealso cref="VisualEffectAsset.GetExposedSpace" />
        ///<seealso cref="VisualEffectAsset.GetTextureDimension" />
        [FreeFunction(Name = "VisualEffectAssetBindings::GetExposedSpace", HasExplicitThis = true)] extern public VFXSpace GetExposedSpace(int nameID);
        ///<summary>Gets the name and type of every exposed property.</summary>
        ///<remarks>The returned <c>System.Type</c> is one of the following:
        ///
        ///- <see cref="AnimationCurve" />
        ///- <see cref="bool" />
        ///- <c>float</c>
        ///- <see cref="Gradient" />
        ///- <see cref="GraphicsBuffer" />
        ///- <c>int</c>
        ///- <see cref="Matrix4x4" />
        ///- <see cref="Mesh" />
        ///- <see cref="SkinnedMeshRenderer" />
        ///- <see cref="Texture" />
        ///- <c>uint</c>
        ///- <see cref="Vector2" />
        ///- <see cref="Vector3" />
        ///- <see cref="Vector4" />
        ///
        ///To determine the <see cref="Rendering.TextureDimension" /> of a Texture, call <see cref="VisualEffectAsset.GetTextureDimension" />.
        ///
        ///To determine the <see cref="VFXSpace" /> of an exposed property, call <see cref="VisualEffectAsset.GetExposedSpace" />.
        ///
        ///To increase the speed of the retrieval process, preallocate the <c>exposedProperties</c> input list.
        ///
        ///This example logs detailed information regarding all exposed properties:</remarks>
        ///<param name="exposedProperties">The List that this function populates with exposed properties.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogExposedProperties.cs}]]></code>
        ///</example>
        [FreeFunction(Name = "VisualEffectAssetBindings::GetExposedProperties", HasExplicitThis = true)] extern public void GetExposedProperties([NotNull][Out] List<VFXExposedProperty> exposedProperties);
        ///<summary>Gets the name of every Event connected to a system.</summary>
        ///<remarks>
        ///  <para>To increase the speed of the retrieval process, preallocate the <c>names</c> input list.
        ///
        ///</para>
        ///  <para>This example logs all available events in the attached <c>VisualEffectAsset</c>.</para>
        ///</remarks>
        ///<param name="names">The List that this function populates with the event system names.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogEvents.cs}]]></code>
        ///</example>
        ///<seealso cref="VisualEffect.SendEvent" />
        [FreeFunction(Name = "VisualEffectAssetBindings::GetEvents", HasExplicitThis = true)] extern public void GetEvents([NotNull][Out] List<string> names);
        [FreeFunction(Name = "VisualEffectAssetBindings::HasSystemFromScript", HasExplicitThis = true)] extern internal bool HasSystem(int nameID);
        [FreeFunction(Name = "VisualEffectAssetBindings::GetSystemNamesFromScript", HasExplicitThis = true)] extern internal void GetSystemNames([NotNull][Out] List<string> names);
        [FreeFunction(Name = "VisualEffectAssetBindings::GetParticleSystemNamesFromScript", HasExplicitThis = true)] extern internal void GetParticleSystemNames([NotNull][Out] List<string> names);
        [FreeFunction(Name = "VisualEffectAssetBindings::GetOutputEventNamesFromScript", HasExplicitThis = true)] extern internal void GetOutputEventNames([NotNull][Out] List<string> names);
        [FreeFunction(Name = "VisualEffectAssetBindings::GetSpawnSystemNamesFromScript", HasExplicitThis = true)] extern internal void GetSpawnSystemNames([NotNull][Out] List<string> names);
        ///<summary>Loads and prewarms all the compute shaders associated with the VisualEffectAsset.</summary>
        ///<remarks>Note: to prewarm rendering shaders, use the <see cref="Rendering.GraphicsStateCollection" /> workflow.</remarks>
        [FreeFunction(Name = "VisualEffectAssetBindings::PrewarmComputeShadersFromScript", HasExplicitThis = true)] extern public void PrewarmComputeShaders();

        ///<summary>Gets the <see cref="Rendering.TextureDimension" /> of a named exposed Texture.</summary>
        ///<remarks>
        ///
        ///This example logs detailed information regarding all exposed properties:</remarks>
        ///<param name="name">The name of the property.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogExposedProperties.cs}]]></code>
        ///</example>
        ///<seealso cref="VisualEffectAsset.GetExposedProperties" />
        ///<seealso cref="VisualEffectAsset.GetExposedSpace" />
        ///<seealso cref="VisualEffectAsset.GetTextureDimension" />
        public UnityEngine.Rendering.TextureDimension GetTextureDimension(string name)
        {
            return GetTextureDimension(Shader.PropertyToID(name));
        }

        ///<summary>Provides the configured space of an exposed property in VisualEffectAsset.</summary>
        ///<remarks>
        ///  <para>VFXSpace.None will be returned if the property doesn't exist or isn't spaceable.
        ///
        ///The <see cref="VFX.VisualEffect.SetVector3" /> won't apply any automatic transform, because <c>VisualEffect</c> expects raw values.
        ///
        ///</para>
        ///  <para>This example logs detailed information regarding all exposed properties.</para>
        ///</remarks>
        ///<param name="name">The name of the property.</param>
        ///<returns>The expected space of the property.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Runtime/Examples/LogExposedProperties.cs}]]></code>
        ///</example>
        ///<seealso cref="VisualEffectAsset.GetExposedProperties" />
        ///<seealso cref="VisualEffectAsset.GetExposedSpace" />
        ///<seealso cref="VisualEffectAsset.GetTextureDimension" />
        public VFXSpace GetExposedSpace(string name)
        {
            return GetExposedSpace(Shader.PropertyToID(name));
        }
    }

    ///<summary>This struct holds information about an output event.</summary>
    public struct VFXOutputEventArgs
    {
        ///<summary>Stores the nameId of the source output event context that triggered this event.</summary>
        ///<remarks>This is the same ID that <see cref="Shader.PropertyToID" /> returns.</remarks>
        public int nameId { get; }
        ///<summary>Stores the current event attribute.</summary>
        ///<remarks>If you need to store this object, use the copy constructor of <see cref="VFXEventAttribute" /> or retrieve the value immediately. This is necessary because Unity recycles this object.</remarks>
        public VFXEventAttribute eventAttribute { get; }

        ///<exclude />
        public VFXOutputEventArgs(int nameId, VFXEventAttribute eventAttribute)
        {
            this.nameId = nameId;
            this.eventAttribute = eventAttribute;
        }
    }

    ///<summary>The visual effect class that references an <see cref="VFX.VisualEffectAsset" /> instance within the Scene.</summary>
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VisualEffectBindings.h")]
    [NativeHeader("Modules/VFX/Public/VisualEffect.h")]
    [NativeClass("VisualEffect", PersistentTypeId = 0x7C28DDA7)]
    [RequireComponent(typeof(Transform))]
    public class VisualEffect : Behaviour
    {
        internal VisualEffect(global::UnityEngine.EntityId id) : base(id) {}
        public VisualEffect() {}
        ///<summary>Use this property to set the pause state of the visual effect.</summary>
        ///<remarks>Unity does not serialize this property. This means that, after loading, it automatically resets to the default value.</remarks>
        extern public bool pause { get; set; }
        ///<summary>A multiplier that Unity applies to the delta time when it updates the VisualEffect. The default value is 1.0f.</summary>
        ///<remarks>To play the visual effect faster than normal, set this property to a value greater than 1.0f. To play the visual effect slower than normal, set this property to a value between 0.0f and 1.0f.
        ///                Unity does not serialize this property. This means that, after loading, it automatically resets to the default value.</remarks>
        extern public float playRate { get; set; }
        ///<summary>The initial seed used for internal random number generator.</summary>
        ///<remarks>Unity ignores this property if you set <see cref="VFX.VisualEffect.resetSeedOnPlay" /> to true.</remarks>
        extern public uint startSeed { get; set; }
        ///<summary>This property controls whether the visual effect generates a new seed for the random number generator with each call to <see cref="VFX.VisualEffect.Play" /> function.</summary>
        extern public bool resetSeedOnPlay { get; set; }
        ///<summary>The default event name ID. To retrieve this value, use the <see cref="Shader.PropertyToID" /> after VisualEffect has awakened or after you've invoked <see cref="VFX.VisualEffect.Reinit" />.</summary>
        ///<seealso cref="VFX.VisualEffect.initialEventName" />
        extern public int initialEventID
        {
            [FreeFunction(Name = "VisualEffectBindings::GetInitialEventID", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "VisualEffectBindings::SetInitialEventID", HasExplicitThis = true)]
            set;
        }

        ///<summary>The default event name. Unity calls this event when the VisualEffect awakes, or when you call <see cref="VisualEffect.Reinit" />.</summary>
        ///<seealso cref="VFX.VisualEffect.initialEventID" />
        extern public string initialEventName
        {
            [FreeFunction(Name = "VisualEffectBindings::GetInitialEventName", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "VisualEffectBindings::SetInitialEventName", HasExplicitThis = true)]
            set;
        }

        ///<summary>Allows the visual effect to be batched with others of the same type.</summary>
        ///<remarks>Instancing feature allows several instances of the same visual effect asset to share the same buffers and be updated together, improving performance.
        ///This property allows this visual effect to opt-out when instancing is enabled in the visual effect asset.
        ///If this property is set to false, this visual effect will use a batch with just this instance.</remarks>
        extern public bool allowInstancing { get; set; }
        ///<summary>This property allows the visual effect to release some buffers when the visual effect is disabled, potentially saving memory.</summary>
        ///<remarks>If the visual effect is using instancing, the batch will only be freed when all the instances have been removed.
        ///This means that setting this property to true and disabling the visual effect will not always result in memory being released. However, it will still allow other visual effects to use its place in the batch, potentially saving memory from being allocated.
        ///Enabling this option means that the instance will be created when the effect is enabled, which can require allocating at that moment, rather than at creation.</remarks>
        extern public bool releaseInstanceWhenDisabled { get; set; }

        ///<summary>Use this property to determine if this visual effect is not visible from any Camera. (RO)</summary>
        extern public bool culled { get; }

        ///<summary>The VisualEffectAsset that the VisualEffect uses.</summary>
        extern public VisualEffectAsset visualEffectAsset { get; set; }

        ///<summary>Use this method to create a new VFXEventAttribute.</summary>
        ///<remarks>You can pass a VFXEventAttribute to <see cref="VFX.VisualEffect.SendEvent" />.</remarks>
        public VFXEventAttribute CreateVFXEventAttribute()
        {
            if (visualEffectAsset == null)
                return null;
            var vfxEventAttribute = VFXEventAttribute.Internal_InstanciateVFXEventAttribute(visualEffectAsset);
            return vfxEventAttribute;
        }

        private void CheckValidVFXEventAttribute(VFXEventAttribute eventAttribute)
        {
            if (eventAttribute != null && eventAttribute.vfxAsset != visualEffectAsset)
            {
                throw new InvalidOperationException("Invalid VFXEventAttribute provided to VisualEffect. It has been created with another VisualEffectAsset. Use CreateVFXEventAttribute.");
            }
        }

        [FreeFunction(Name = "VisualEffectBindings::SendEventFromScript", HasExplicitThis = true)]
        extern private void SendEventFromScript(int eventNameID, VFXEventAttribute eventAttribute);

        ///<summary>Use this method to send a custom named event.</summary>
        ///<param name="eventNameID">The ID of the event. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="eventAttribute">Can be null or a VFXEventAttribute. To create a VFXEventAttribute, use <see cref="VFX.VisualEffect.CreateVFXEventAttribute" />.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers the default visual effect play event once every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                m_VisualEffect.SendEvent(VisualEffectAsset.PlayEventID);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers multiple events during the same frame every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///    private int m_SpawnCountIdentifier;
        ///    private int m_ColorIdentifier;
        ///    private int m_EventNameIdentifier;
        ///
        ///    private void Start()
        ///    {
        ///        m_SpawnCountIdentifier = Shader.PropertyToID("spawnCount");
        ///        m_ColorIdentifier = Shader.PropertyToID("color");
        ///        m_EventNameIdentifier = Shader.PropertyToID("direct");
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                var eventAttribute = m_VisualEffect.CreateVFXEventAttribute();
        ///                // Red
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 1);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(1, 0, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Blue
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 3);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 0, 1));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Green
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 2);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 1, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SendEvent(int eventNameID, VFXEventAttribute eventAttribute)
        {
            CheckValidVFXEventAttribute(eventAttribute);
            SendEventFromScript(eventNameID, eventAttribute);
        }

        ///<summary>Use this method to send a custom named event.</summary>
        ///<param name="eventName">The name of the event.</param>
        ///<param name="eventAttribute">Can be null or a VFXEventAttribute. To create a VFXEventAttribute, use <see cref="VFX.VisualEffect.CreateVFXEventAttribute" />.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers the default visual effect play event once every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                m_VisualEffect.SendEvent(VisualEffectAsset.PlayEventID);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers multiple events during the same frame every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///    private int m_SpawnCountIdentifier;
        ///    private int m_ColorIdentifier;
        ///    private int m_EventNameIdentifier;
        ///
        ///    private void Start()
        ///    {
        ///        m_SpawnCountIdentifier = Shader.PropertyToID("spawnCount");
        ///        m_ColorIdentifier = Shader.PropertyToID("color");
        ///        m_EventNameIdentifier = Shader.PropertyToID("direct");
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                var eventAttribute = m_VisualEffect.CreateVFXEventAttribute();
        ///                // Red
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 1);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(1, 0, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Blue
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 3);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 0, 1));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Green
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 2);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 1, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SendEvent(string eventName, VFXEventAttribute eventAttribute)
        {
            SendEvent(Shader.PropertyToID(eventName), eventAttribute);
        }

        ///<summary>Use this method to send a custom named event.</summary>
        ///<param name="eventNameID">The ID of the event. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers the default visual effect play event once every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                m_VisualEffect.SendEvent(VisualEffectAsset.PlayEventID);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers multiple events during the same frame every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///    private int m_SpawnCountIdentifier;
        ///    private int m_ColorIdentifier;
        ///    private int m_EventNameIdentifier;
        ///
        ///    private void Start()
        ///    {
        ///        m_SpawnCountIdentifier = Shader.PropertyToID("spawnCount");
        ///        m_ColorIdentifier = Shader.PropertyToID("color");
        ///        m_EventNameIdentifier = Shader.PropertyToID("direct");
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                var eventAttribute = m_VisualEffect.CreateVFXEventAttribute();
        ///                // Red
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 1);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(1, 0, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Blue
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 3);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 0, 1));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Green
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 2);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 1, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SendEvent(int eventNameID)
        {
            SendEventFromScript(eventNameID, null);
        }

        ///<summary>Use this method to send a custom named event.</summary>
        ///<param name="eventName">The name of the event.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers the default visual effect play event once every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                m_VisualEffect.SendEvent(VisualEffectAsset.PlayEventID);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // The following example triggers multiple events during the same frame every second.
        ///using UnityEngine;
        ///using UnityEngine.VFX;
        ///
        ///public class SendEventExample : MonoBehaviour
        ///{
        ///    public VisualEffect m_VisualEffect;
        ///    private float m_Waiting = 1.0f;
        ///    private int m_SpawnCountIdentifier;
        ///    private int m_ColorIdentifier;
        ///    private int m_EventNameIdentifier;
        ///
        ///    private void Start()
        ///    {
        ///        m_SpawnCountIdentifier = Shader.PropertyToID("spawnCount");
        ///        m_ColorIdentifier = Shader.PropertyToID("color");
        ///        m_EventNameIdentifier = Shader.PropertyToID("direct");
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        if (m_VisualEffect)
        ///        {
        ///            m_Waiting -= Time.deltaTime;
        ///            if (m_Waiting < 0.0f)
        ///            {
        ///                m_Waiting = 1.0f;
        ///                var eventAttribute = m_VisualEffect.CreateVFXEventAttribute();
        ///                // Red
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 1);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(1, 0, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Blue
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 3);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 0, 1));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///                // Green
        ///                eventAttribute.SetFloat(m_SpawnCountIdentifier, 2);
        ///                eventAttribute.SetVector3(m_ColorIdentifier, new Vector3(0, 1, 0));
        ///                m_VisualEffect.SendEvent(m_EventNameIdentifier, eventAttribute);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SendEvent(string eventName)
        {
            SendEvent(Shader.PropertyToID(eventName), null);
        }

        ///<summary>Use this method to send a play event to every Spawn system.</summary>
        ///<remarks>This call is equivalent to <see cref="VFX.VisualEffect.SendEvent" />("OnPlay");</remarks>
        ///<param name="eventAttribute">Can be null or a VFXEventAttribute. To create a VFXEventAttribute, use <see cref="VFX.VisualEffect.CreateVFXEventAttribute" />.</param>
        public void Play(VFXEventAttribute eventAttribute)
        {
            SendEvent(VisualEffectAsset.PlayEventID, eventAttribute);
        }

        ///<summary>Use this method to send a play event to every Spawn system.</summary>
        ///<remarks>This call is equivalent to <see cref="VFX.VisualEffect.SendEvent" />("OnPlay");</remarks>
        public void Play()
        {
            SendEvent(VisualEffectAsset.PlayEventID);
        }

        ///<summary>Use this method to send a stop event to all Spawn systems.</summary>
        ///<remarks>Equivalent to SendEvent("OnStop").</remarks>
        ///<param name="eventAttribute">Can be null or a VFXEventAttribute. To create a VFXEventAttribute, use <see cref="VFX.VisualEffect.CreateVFXEventAttribute" />.</param>
        public void Stop(VFXEventAttribute eventAttribute)
        {
            SendEvent(VisualEffectAsset.StopEventID, eventAttribute);
        }

        ///<summary>Use this method to send a stop event to all Spawn systems.</summary>
        ///<remarks>Equivalent to SendEvent("OnStop").</remarks>
        public void Stop()
        {
            SendEvent(VisualEffectAsset.StopEventID);
        }

        ///<summary>Reintialize visual effect.</summary>
        ///<remarks>- Restores every system to its initial state.
        ///
        ///- Resets the internal total time to zero.
        ///
        ///- If <see cref="VFX.VisualEffect.resetSeedOnPlay" /> is true, this method recomputes a new random seed for the random value generator.
        ///
        ///- Invokes <see cref="VFX.VisualEffect.SendEvent" /> with <see cref="VFX.VisualEffect.initialEventID" />.</remarks>
        public void Reinit()
        {
            Reinit(true);
        }

        extern internal void Reinit(bool sendInitialEventAndPrewarm = true);

        ///<summary>If <see cref="VFX.VisualEffect.pause" /> is true, this method processes the next visual effect update for exactly one frame with the current delta time.</summary>
        extern public void AdvanceOneFrame();

        extern internal void RecreateData();

        extern internal void RecreateBatchInstance();

        internal enum VFXCPUEffectMarkers
        {
            FullUpdate,
            ProcessUpdate,
            EvaluateExpressions,
        }

        [FreeFunction(Name = "VisualEffectBindings::GetGPUTaskMarkerName", HasExplicitThis = true, ThrowsException = true)]
        [NativeConditional("ENABLE_PROFILER")]
        extern private string GetGPUTaskMarkerName(int nameID, int taskIndex);
        [FreeFunction(Name = "VisualEffectBindings::GetCPUEffectMarkerName", HasExplicitThis = true, ThrowsException = true)]
        [NativeConditional("ENABLE_PROFILER")]
        extern internal string GetCPUEffectMarkerName(int markerIndex);

        [FreeFunction(Name = "VisualEffectBindings::GetCPUSystemMarkerName", HasExplicitThis = true, ThrowsException = true)]
        [NativeConditional("ENABLE_PROFILER")]
        extern private string GetCPUSystemMarkerName(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::RegisterForProfiling", HasExplicitThis = true, ThrowsException = false)]
        [NativeConditional("ENABLE_PROFILER")]
        extern internal void RegisterForProfiling();
        [FreeFunction(Name = "VisualEffectBindings::UnregisterForProfiling", HasExplicitThis = true, ThrowsException = false)]
        [NativeConditional("ENABLE_PROFILER")]
        extern internal void UnregisterForProfiling();

        [FreeFunction(Name = "VisualEffectBindings::IsRegisteredForProfiling", HasExplicitThis = true, ThrowsException = false)]
        [NativeConditional("ENABLE_PROFILER")]
        extern internal bool IsRegisteredForProfiling();


        ///<summary>Use this method to set the overridden state to false. This restores the default value that the Visual Effect Asset specifies.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::ResetOverrideFromScript", HasExplicitThis = true)] extern public void ResetOverride(int nameID);

        // Values check
        ///<summary>Gets expected texture dimension for a named exposed texture.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::GetTextureDimensionFromScript", HasExplicitThis = true)] extern public UnityEngine.Rendering.TextureDimension GetTextureDimension(int nameID);
        ///<summary>Checks if the Visual Effect can override a bool with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<bool>", HasExplicitThis = true)] extern public bool HasBool(int nameID);
        ///<summary>Checks if the Visual Effect can override an integer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<int>", HasExplicitThis = true)] extern public bool HasInt(int nameID);
        ///<summary>Checks if the Visual Effect can override an unsigned integer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<UInt32>", HasExplicitThis = true)] extern public bool HasUInt(int nameID);
        ///<summary>Checks if the Visual Effect can override a float with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<float>", HasExplicitThis = true)] extern public bool HasFloat(int nameID);
        ///<summary>Checks if the Visual Effect can override a Vector2 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Vector2f>", HasExplicitThis = true)] extern public bool HasVector2(int nameID);
        ///<summary>Checks if the Visual Effect can override a Vector3 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Vector3f>", HasExplicitThis = true)] extern public bool HasVector3(int nameID);
        ///<summary>Checks if the Visual Effect can override a Vector4 or Color with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Vector4f>", HasExplicitThis = true)] extern public bool HasVector4(int nameID);
        ///<summary>Checks if the Visual Effect can override a Matrix4x4 with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Matrix4x4f>", HasExplicitThis = true)] extern public bool HasMatrix4x4(int nameID);
        ///<summary>Checks if the Visual Effect can override a texture with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Texture*>", HasExplicitThis = true)] extern public bool HasTexture(int nameID);
        ///<summary>Checks if the Visual Effect can override an Animation Curve with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<AnimationCurve*>", HasExplicitThis = true)] extern public bool HasAnimationCurve(int nameID);
        ///<summary>Checks if the Visual Effect can override a Gradient with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Gradient*>", HasExplicitThis = true)] extern public bool HasGradient(int nameID);
        ///<summary>Checks if the Visual Effect can override a Mesh with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<Mesh*>", HasExplicitThis = true)] extern public bool HasMesh(int nameID);
        ///<summary>Checks if the Visual Effect can override a Skinned Mesh Renderer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<SkinnedMeshRenderer*>", HasExplicitThis = true)] extern public bool HasSkinnedMeshRenderer(int nameID);
        ///<summary>Checks if the Visual Effect can override a GraphicsBuffer with the name you pass in.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        [FreeFunction(Name = "VisualEffectBindings::HasValueFromScript<GraphicsBuffer*>", HasExplicitThis = true)] extern public bool HasGraphicsBuffer(int nameID);

        // Value setters
        ///<summary>Sets the value of a named bool property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="b">The new boolean value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<bool>", HasExplicitThis = true)] extern public void SetBool(int nameID, bool b);
        ///<summary>Sets the value of a named integer property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="i">The new integer value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<int>", HasExplicitThis = true)] extern public void SetInt(int nameID, int i);
        ///<summary>Sets the value of a named unsigned integer property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="i">The new unsigned integer value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<UInt32>", HasExplicitThis = true)] extern public void SetUInt(int nameID, uint i);
        ///<summary>Sets the value of a float property exposed in the blackboard.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="f">The new float value.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Editor/Examples/SetFloatExample.cs}]]></code>
        ///</example>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<float>", HasExplicitThis = true)] extern public void SetFloat(int nameID, float f);
        ///<summary>Sets the value of a named Vector2 property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Vector2 value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Vector2f>", HasExplicitThis = true)] extern public void SetVector2(int nameID, Vector2 v);
        ///<summary>Sets the value of a named Vector3 property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Vector3 value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Vector3f>", HasExplicitThis = true)] extern public void SetVector3(int nameID, Vector3 v);
        ///<summary>Sets the value of a named Vector4 or Color property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Vector4 value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Vector4f>", HasExplicitThis = true)] extern public void SetVector4(int nameID, Vector4 v);
        ///<summary>Sets the value of a named Matrix4x4 property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="v">The new Matrix4x4 value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Matrix4x4f>", HasExplicitThis = true)] extern public void SetMatrix4x4(int nameID, Matrix4x4 v);
        ///<summary>Sets the value of a named texture property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.
        ///If the provided texture dimension doesn't correspond to the expected dimension, this function can log an error. If that happens, this assignment is ignored.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="t">The new texture value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Texture*>", HasExplicitThis = true)] extern public void SetTexture(int nameID, [NotNull] Texture t);
        ///<summary>Sets the value of a named Animation Curve property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="c">The new Animation Curve.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<AnimationCurve*>", HasExplicitThis = true)] extern public void SetAnimationCurve(int nameID, [NotNull] AnimationCurve c);
        ///<summary>Sets the value of a named Gradient property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="g">The new Gradient value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Gradient*>", HasExplicitThis = true)] extern public void SetGradient(int nameID, [NotNull] Gradient g);
        ///<summary>Sets the value of a named Mesh property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="m">The new Mesh value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<Mesh*>", HasExplicitThis = true)] extern public void SetMesh(int nameID, [NotNull] Mesh m);
        ///<summary>Sets the value of a named Skinned Mesh Renderer property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="m">The new Skinned Mesh Renderer value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<SkinnedMeshRenderer*>", HasExplicitThis = true)] extern public void SetSkinnedMeshRenderer(int nameID, SkinnedMeshRenderer m);
        ///<summary>Sets the value of a named GraphicsBuffer property.</summary>
        ///<remarks>Unity does not serialize this reference because GraphicsBuffer isn't a <see cref="UnityEngine.Object" />.</remarks>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="g">The new GraphicsBuffer value.</param>
        [FreeFunction(Name = "VisualEffectBindings::SetValueFromScript<GraphicsBuffer*>", HasExplicitThis = true)] extern public void SetGraphicsBuffer(int nameID, GraphicsBuffer g);
        // Value getters
        ///<summary>Gets the value of a named bool property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the bool you specify. Returns <c>false</c> if <see cref="VFX.VisualEffect.HasBool" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<bool>", HasExplicitThis = true)] extern public bool GetBool(int nameID);
        ///<summary>Get a named exposed integer.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the integer you specify. Returns <c>0</c> if <see cref="VFX.VisualEffect.HasInt" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<int>", HasExplicitThis = true)] extern public int GetInt(int nameID);
        ///<summary>Gets the value of a named unsigned integer property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the unsigned integer you specify. Returns <c>0</c> if <see cref="VFX.VisualEffect.HasUInt" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<UInt32>", HasExplicitThis = true)] extern public uint GetUInt(int nameID);
        ///<summary>Gets the value of a named float property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the float you specify. Returns /0.0f/ if <see cref="VFX.VisualEffect.HasFloat" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<float>", HasExplicitThis = true)] extern public float GetFloat(int nameID);
        ///<summary>Gets the value of a named Vector2 property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Vector2 you specify. Returns /Vector2.zero/ if <see cref="VFX.VisualEffect.HasVector2" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Vector2f>", HasExplicitThis = true)] extern public Vector2 GetVector2(int nameID);
        ///<summary>Gets the value of a named Vector3 property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Vector3 you specify. Returns /Vector3.zero/ if <see cref="VFX.VisualEffect.HasVector3" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Vector3f>", HasExplicitThis = true)] extern public Vector3 GetVector3(int nameID);
        ///<summary>Gets the value of a named Vector4 or Color property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Vector4 you specify. Returns /Vector4.zero/ if <see cref="VFX.VisualEffect.HasVector4" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Vector4f>", HasExplicitThis = true)] extern public Vector4 GetVector4(int nameID);
        ///<summary>Gets the value of a named Matrix4x4 property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Matrix4x4 you specify. Returns /Matrix4x4.identity/ if <see cref="VFX.VisualEffect.HasMatrix4x4" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Matrix4x4f>", HasExplicitThis = true)] extern public Matrix4x4 GetMatrix4x4(int nameID);
        ///<summary>Gets the value of a named texture property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the texture you specify. Returns <c>null</c> if <see cref="VFX.VisualEffect.HasTexture" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Texture*>", HasExplicitThis = true)] extern public Texture GetTexture(int nameID);
        ///<summary>Gets the value of a named Mesh property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Mesh you specify. Returns <c>null</c> if <see cref="VFX.VisualEffect.HasMesh" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<Mesh*>", HasExplicitThis = true)] extern public Mesh GetMesh(int nameID);
        ///<summary>Gets the value of a named Skinned Mesh Renderer property.</summary>
        ///<param name="nameID">The name of the property.</param>
        ///<returns>The value for the Skinned Mesh Renderer you specify. Returns <c>null</c> if <see cref="VFX.VisualEffect.HasSkinnedMeshRenderer" /> returns <c>false</c>.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<SkinnedMeshRenderer*>", HasExplicitThis = true)] extern public SkinnedMeshRenderer GetSkinnedMeshRenderer(int nameID);

        //The internal bindings function is using GraphicsBuffer*.
        //Thus, this function will return a new GraphicsBuffer instead of the original scripting reference.
        //This behavior isn't safe (we can potentially keep a reference on the source ScriptingObjectPtr).
        //In consequence, this getter is internal and only used for debug purpose of editor test.
        [FreeFunction(Name = "VisualEffectBindings::GetValueFromScript<GraphicsBuffer*>", HasExplicitThis = true)] extern internal GraphicsBuffer GetGraphicsBuffer(int nameID);

        ///<summary>Gets the value of a named Gradient property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Gradient you specify. Returns an empty Gradient if <see cref="VFX.VisualEffect.HasGradient" /> returns <c>false</c>.</returns>
        public Gradient GetGradient(int nameID)
        {
            var gradient = new Gradient();
            Internal_GetGradient(nameID, gradient);
            return gradient;
        }

        [FreeFunction(Name = "VisualEffectBindings::Internal_GetGradientFromScript", HasExplicitThis = true)] extern private void Internal_GetGradient(int nameID, Gradient gradient);

        ///<summary>Gets the value of a named Animation Curve property.</summary>
        ///<param name="nameID">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Animation Curve you specify. Returns an empty Animation Curve if <see cref="VFX.VisualEffect.HasAnimationCurve" /> returns <c>false</c>.</returns>
        public AnimationCurve GetAnimationCurve(int nameID)
        {
            var curve = new AnimationCurve();
            Internal_GetAnimationCurve(nameID, curve);
            return curve;
        }

        [FreeFunction(Name = "VisualEffectBindings::Internal_GetAnimationCurveFromScript", HasExplicitThis = true)] extern private void Internal_GetAnimationCurve(int nameID, AnimationCurve curve);

        ///<summary>Gets information on a particle system.</summary>
        ///<param name="nameID">The system ID. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>A <see cref="VFXParticleSystemInfo" /> instance.</returns>
        [FreeFunction(Name = "VisualEffectBindings::GetParticleSystemInfo", HasExplicitThis = true, ThrowsException = true)] extern public VFXParticleSystemInfo GetParticleSystemInfo(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetSpawnSystemInfo", HasExplicitThis = true, ThrowsException = true)] extern private void GetSpawnSystemInfo(int nameID, IntPtr spawnerState);
        ///<summary>Checks if any particle system in the effect is awake.</summary>
        ///<returns>Returns <c>true</c> if at least one of the particle systems of the effect is awake, otherwise returns <c>false</c>.</returns>
        extern public bool HasAnySystemAwake();

        [FreeFunction(Name = "VisualEffectBindings::GetComputedBounds", HasExplicitThis = true)] extern internal Bounds GetComputedBounds(int nameID);
        [FreeFunction(Name = "VisualEffectBindings::GetCurrentBoundsPadding", HasExplicitThis = true)] extern internal Vector3 GetCurrentBoundsPadding(int nameID);

        ///<summary>Gets state on a spawn system.</summary>
        ///<remarks>Use <see cref="VFXSpawnerState" /> as parameter to avoid an allocation.</remarks>
        ///<param name="nameID">The system ID. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<param name="spawnState">A modified  <see cref="VFXSpawnerState" /> instance.</param>
        public void GetSpawnSystemInfo(int nameID, VFXSpawnerState spawnState)
        {
            if (spawnState == null)
                throw new NullReferenceException("GetSpawnSystemInfo expects a non null VFXSpawnerState.");
            IntPtr ptr = spawnState.GetPtr();
            if (ptr == IntPtr.Zero)
                throw new NullReferenceException("GetSpawnSystemInfo use an unexpected not owned VFXSpawnerState.");
            GetSpawnSystemInfo(nameID, ptr);
        }

        ///<summary>Gets state on a spawn system.</summary>
        ///<remarks>Use <see cref="VFXSpawnerState" /> as parameter to avoid an allocation.</remarks>
        ///<param name="nameID">The system ID. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        public VFXSpawnerState GetSpawnSystemInfo(int nameID)
        {
            var spawnState = new VFXSpawnerState();
            GetSpawnSystemInfo(nameID, spawnState);
            return spawnState;
        }

        ///<summary>Use this function to determine if the VisualEffect has the system you pass in.</summary>
        ///<param name="nameID">The system ID. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>true if the VisualEffect has the system you pass in. Returns false otherwise.</returns>
        public bool HasSystem(int nameID)
        {
            var vfxAsset = visualEffectAsset;
            return vfxAsset != null && vfxAsset.HasSystem(nameID);
        }

        ///<summary>Gets the name of every system.</summary>
        ///<remarks>Preallocating the input list speeds up the retrieval process.</remarks>
        ///<param name="names">The List that this function populates with the system names.</param>
        public void GetSystemNames(List<string> names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            var vfxAsset = visualEffectAsset;
            if (vfxAsset)
                vfxAsset.GetSystemNames(names);
            else
                names.Clear();
        }

        ///<summary>Gets the name of every particle system.</summary>
        ///<remarks>Preallocating the input list speeds up the retrieval process.</remarks>
        ///<param name="names">The List that this function populates with the particle system names.</param>
        public void GetParticleSystemNames(List<string> names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            var vfxAsset = visualEffectAsset;
            if (vfxAsset)
                vfxAsset.GetParticleSystemNames(names);
            else
                names.Clear();
        }

        ///<summary>Gets the name of every output event system.</summary>
        ///<remarks>Preallocating the input list speeds up the retrieval process.</remarks>
        ///<param name="names">The List that this function populates with the output event system names.</param>
        public void GetOutputEventNames(List<string> names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            var vfxAsset = visualEffectAsset;
            if (vfxAsset)
                vfxAsset.GetOutputEventNames(names);
            else
                names.Clear();
        }

        ///<summary>Gets the name of every spawn system.</summary>
        ///<remarks>Preallocating the input list speeds up the retrieval process.</remarks>
        ///<param name="names">The List that this function populates with the spawn system names.</param>
        public void GetSpawnSystemNames(List<string> names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            var vfxAsset = visualEffectAsset;
            if (vfxAsset)
                vfxAsset.GetSpawnSystemNames(names);
            else
                names.Clear();
        }

        ///<summary>Use this method to set the overridden state to false. This restores the default value that the Visual Effect Asset specifies.</summary>
        ///<param name="name">The name of the property.</param>
        public void ResetOverride(string name)
        {
            ResetOverride(Shader.PropertyToID(name));
        }

        // Values check
        ///<summary>Checks if the Visual Effect can override an integer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasInt(string name)
        {
            return HasInt(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override an unsigned integer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasUInt(string name)
        {
            return HasUInt(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a float with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasFloat(string name)
        {
            return HasFloat(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Vector2 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasVector2(string name)
        {
            return HasVector2(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Vector3 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasVector3(string name)
        {
            return HasVector3(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Vector4 or Color with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasVector4(string name)
        {
            return HasVector4(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Matrix4x4 with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasMatrix4x4(string name)
        {
            return HasMatrix4x4(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a texture with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasTexture(string name)
        {
            return HasTexture(Shader.PropertyToID(name));
        }

        ///<summary>Gets expected texture dimension for a named exposed texture.</summary>
        ///<param name="name">The name of the property.</param>
        public UnityEngine.Rendering.TextureDimension GetTextureDimension(string name)
        {
            return GetTextureDimension(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override an Animation Curve with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasAnimationCurve(string name)
        {
            return HasAnimationCurve(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Gradient with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasGradient(string name)
        {
            return HasGradient(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Mesh with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasMesh(string name)
        {
            return HasMesh(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a Skinned Mesh Renderer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasSkinnedMeshRenderer(string name)
        {
            return HasSkinnedMeshRenderer(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a GraphicsBuffer with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasGraphicsBuffer(string name)
        {
            return HasGraphicsBuffer(Shader.PropertyToID(name));
        }

        ///<summary>Checks if the Visual Effect can override a bool with the name you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        public bool HasBool(string name)
        {
            return HasBool(Shader.PropertyToID(name));
        }

        // Value setters
        ///<summary>Sets the value of a named integer property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="i">The new integer value.</param>
        public void SetInt(string name, int i)
        {
            SetInt(Shader.PropertyToID(name), i);
        }

        ///<summary>Sets the value of a named unsigned integer property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="i">The new unsigned integer value.</param>
        public void SetUInt(string name, uint i)
        {
            SetUInt(Shader.PropertyToID(name), i);
        }

        ///<summary>Sets the value of a float property exposed in the blackboard.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="f">The new float value.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/SRPTests/Packages/com.unity.testing.visualeffectgraph/Editor/Examples/SetFloatExample.cs}]]></code>
        ///</example>
        public void SetFloat(string name, float f)
        {
            SetFloat(Shader.PropertyToID(name), f);
        }

        ///<summary>Sets the value of a named Vector2 property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Vector2 value.</param>
        public void SetVector2(string name, Vector2 v)
        {
            SetVector2(Shader.PropertyToID(name), v);
        }

        ///<summary>Sets the value of a named Vector3 property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Vector3 value.</param>
        public void SetVector3(string name, Vector3 v)
        {
            SetVector3(Shader.PropertyToID(name), v);
        }

        ///<summary>Sets the value of a named Vector4 or Color property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Vector4 value.</param>
        public void SetVector4(string name, Vector4 v)
        {
            SetVector4(Shader.PropertyToID(name), v);
        }

        ///<summary>Sets the value of a named Matrix4x4 property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="v">The new Matrix4x4 value.</param>
        public void SetMatrix4x4(string name, Matrix4x4 v)
        {
            SetMatrix4x4(Shader.PropertyToID(name), v);
        }

        ///<summary>Sets the value of a named texture property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.
        ///If the provided texture dimension doesn't correspond to the expected dimension, this function can log an error. If that happens, this assignment is ignored.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="t">The new texture value.</param>
        public void SetTexture(string name, Texture t)
        {
            SetTexture(Shader.PropertyToID(name), t);
        }

        ///<summary>Sets the value of a named Animation Curve property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="c">The new Animation Curve.</param>
        ///<param name="name">The name of the property.</param>
        public void SetAnimationCurve(string name, AnimationCurve c)
        {
            SetAnimationCurve(Shader.PropertyToID(name), c);
        }

        ///<summary>Sets the value of a named Gradient property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="g">The new Gradient value.</param>
        public void SetGradient(string name, Gradient g)
        {
            SetGradient(Shader.PropertyToID(name), g);
        }

        ///<summary>Sets the value of a named Mesh property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="m">The new Mesh value.</param>
        public void SetMesh(string name, Mesh m)
        {
            SetMesh(Shader.PropertyToID(name), m);
        }

        ///<summary>Sets the value of a named Skinned Mesh Renderer property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="m">The new Skinned Mesh Renderer value.</param>
        public void SetSkinnedMeshRenderer(string name, SkinnedMeshRenderer m)
        {
            SetSkinnedMeshRenderer(Shader.PropertyToID(name), m);
        }

        ///<summary>Sets the value of a named GraphicsBuffer property.</summary>
        ///<remarks>Unity does not serialize this reference because GraphicsBuffer isn't a <see cref="UnityEngine.Object" />.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="g">The new GraphicsBuffer value.</param>
        public void SetGraphicsBuffer(string name, GraphicsBuffer g)
        {
            SetGraphicsBuffer(Shader.PropertyToID(name), g);
        }

        ///<summary>Sets the value of a named bool property.</summary>
        ///<remarks>Automatically changes overridden state for this property to true.</remarks>
        ///<param name="name">The name of the property.</param>
        ///<param name="b">The new boolean value.</param>
        public void SetBool(string name, bool b)
        {
            SetBool(Shader.PropertyToID(name), b);
        }

        // Value getters
        ///<summary>Get a named exposed integer.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the integer you specify. Returns <c>0</c> if <see cref="VFX.VisualEffect.HasInt" /> returns <c>false</c>.</returns>
        public int GetInt(string name)
        {
            return GetInt(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named unsigned integer property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the unsigned integer you specify. Returns <c>0</c> if <see cref="VFX.VisualEffect.HasUInt" /> returns <c>false</c>.</returns>
        public uint GetUInt(string name)
        {
            return GetUInt(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named float property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the float you specify. Returns /0.0f/ if <see cref="VFX.VisualEffect.HasFloat" /> returns <c>false</c>.</returns>
        public float GetFloat(string name)
        {
            return GetFloat(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Vector2 property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Vector2 you specify. Returns /Vector2.zero/ if <see cref="VFX.VisualEffect.HasVector2" /> returns <c>false</c>.</returns>
        public Vector2 GetVector2(string name)
        {
            return GetVector2(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Vector3 property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Vector3 you specify. Returns /Vector3.zero/ if <see cref="VFX.VisualEffect.HasVector3" /> returns <c>false</c>.</returns>
        public Vector3 GetVector3(string name)
        {
            return GetVector3(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Vector4 or Color property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Vector4 you specify. Returns /Vector4.zero/ if <see cref="VFX.VisualEffect.HasVector4" /> returns <c>false</c>.</returns>
        public Vector4 GetVector4(string name)
        {
            return GetVector4(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Matrix4x4 property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Matrix4x4 you specify. Returns /Matrix4x4.identity/ if <see cref="VFX.VisualEffect.HasMatrix4x4" /> returns <c>false</c>.</returns>
        public Matrix4x4 GetMatrix4x4(string name)
        {
            return GetMatrix4x4(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named texture property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the texture you specify. Returns <c>null</c> if <see cref="VFX.VisualEffect.HasTexture" /> returns <c>false</c>.</returns>
        public Texture GetTexture(string name)
        {
            return GetTexture(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Mesh property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Mesh you specify. Returns <c>null</c> if <see cref="VFX.VisualEffect.HasMesh" /> returns <c>false</c>.</returns>
        public Mesh GetMesh(string name)
        {
            return GetMesh(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Skinned Mesh Renderer property.</summary>
        ///<param name="name">The ID of the property. This is the same ID that <see cref="Shader.PropertyToID" /> returns.</param>
        ///<returns>The value for the Skinned Mesh Renderer you specify. Returns <c>null</c> if <see cref="VFX.VisualEffect.HasSkinnedMeshRenderer" /> returns <c>false</c>.</returns>
        public SkinnedMeshRenderer GetSkinnedMeshRenderer(string name)
        {
            return GetSkinnedMeshRenderer(Shader.PropertyToID(name));
        }

        internal GraphicsBuffer GetGraphicsBuffer(string name)
        {
            return GetGraphicsBuffer(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named bool property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the bool you specify. Returns <c>false</c> if <see cref="VFX.VisualEffect.HasBool" /> returns <c>false</c>.</returns>
        public bool GetBool(string name)
        {
            return GetBool(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Animation Curve property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Animation Curve you specify. Returns an empty Animation Curve if <see cref="VFX.VisualEffect.HasAnimationCurve" /> returns <c>false</c>.</returns>
        public AnimationCurve GetAnimationCurve(string name)
        {
            return GetAnimationCurve(Shader.PropertyToID(name));
        }

        ///<summary>Gets the value of a named Gradient property.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>The value for the Gradient you specify. Returns an empty Gradient if <see cref="VFX.VisualEffect.HasGradient" /> returns <c>false</c>.</returns>
        public Gradient GetGradient(string name)
        {
            return GetGradient(Shader.PropertyToID(name));
        }

        ///<summary>Use this function to determine if the VisualEffect has the system you pass in.</summary>
        ///<param name="name">The name of the property.</param>
        ///<returns>true if the VisualEffect has the system you pass in. Returns false otherwise.</returns>
        public bool HasSystem(string name)
        {
            return HasSystem(Shader.PropertyToID(name));
        }

        ///<summary>Gets information on a particle system.</summary>
        ///<param name="name">The name of the particle system.</param>
        ///<returns>A <see cref="VFXParticleSystemInfo" /> instance.</returns>
        public VFXParticleSystemInfo GetParticleSystemInfo(string name)
        {
            return GetParticleSystemInfo(Shader.PropertyToID(name));
        }

        internal string GetGPUTaskMarkerName(string systemName, int taskIndex)
        {
            return GetGPUTaskMarkerName(Shader.PropertyToID(systemName), taskIndex);
        }

        internal string GetCPUSystemMarkerName(string systemName)
        {
            return GetCPUSystemMarkerName(Shader.PropertyToID(systemName));
        }

        internal string GetCPUEffectMarkerName(VFXCPUEffectMarkers markerId)
        {
            return GetCPUEffectMarkerName((int)markerId);
        }

        ///<summary>Gets state on a spawn system.</summary>
        ///<remarks>Use <see cref="VFXSpawnerState" /> as parameter to avoid an allocation.</remarks>
        public VFXSpawnerState GetSpawnSystemInfo(string name)
        {
            return GetSpawnSystemInfo(Shader.PropertyToID(name));
        }

        internal Bounds GetComputedBounds(string name)
        {
            return GetComputedBounds(Shader.PropertyToID(name));
        }

        internal Vector3 GetCurrentBoundsPadding(string name)
        {
            return GetCurrentBoundsPadding(Shader.PropertyToID(name));
        }

        ///<summary>Returns the sum of all alive particles within the visual effect.</summary>
        ///<remarks>If the particle count is only available on the GPU, the actual result can be delayed. This is because the system uses an asynchronous readback to retrieve the result.</remarks>
        extern public int aliveParticleCount { get; }

        extern internal float time { get; }

        ///<summary>Use this method to fast-forward the visual effect by simulating all systems for several step counts using the specified delta time.</summary>
        ///<param name="stepDeltaTime">The delta time, in seconds, the simulation applies to each step.</param>
        ///<param name="stepCount">Number of steps to proceed.</param>
        extern public void Simulate(float stepDeltaTime, uint stepCount = 1);

        //Could be exposed publicly but requires a specific function from bindings which doesn't call BaseObject::Reset (because it also resets the awake flags)
        //extern internal void Reset();

        private VFXEventAttribute m_cachedEventAttribute;
        [RequiredByNativeCode]
        private static VFXEventAttribute InvokeGetCachedEventAttributeForOutputEvent_Internal(VisualEffect source)
        {
            //If outputEventReceived is null, skip this behavior, InvokeOutputEventReceived_Internal will be not triggered
            if (source.outputEventReceived == null)
                return null;

            if (source.m_cachedEventAttribute == null)
                source.m_cachedEventAttribute = source.CreateVFXEventAttribute();
            return source.m_cachedEventAttribute;
        }

        ///<summary>Output event are reported trough this callback.</summary>
        ///<remarks>If you need to store the <see cref="VFXEventAttribute" /> in the <see cref="VFXOutputEventArgs" />, use the copy constructor of <see cref="VFXEventAttribute" /> or retrieve the value immediately. This is necessary because Unity recycles the <see cref="VFXEventAttribute" />.</remarks>
        public Action<VFXOutputEventArgs> outputEventReceived;
        [RequiredByNativeCode]
        private static void InvokeOutputEventReceived_Internal(VisualEffect source, int eventNameId)
        {
            var evt = new VFXOutputEventArgs(eventNameId, source.m_cachedEventAttribute);
            source.outputEventReceived.Invoke(evt);
        }
    }

    // This type must be tagged as [RequiredByNativeCode] because it's implicitly required by the VisualEffect component.
    // Otherwise, the type may get stripped if "Strip Engine Code" is enabled in Player settings, causing VisualEffect
    // to crash when it tries to create its renderer.
    // The public constructor with [RequiredMember] is necessary for the same reason.
    // See UUM-99927 for details.
    ///<summary>Renders a <see cref="VFX.VisualEffect" />.</summary>
    [RequiredByNativeCode]
    [NativeHeader("Modules/VFX/Public/VFXRenderer.h"), RejectDragAndDropMaterial]
    [NativeClass("VFXRenderer", PersistentTypeId = 0x045FFA89)]
    public sealed partial class VFXRenderer : Renderer
    {
        internal VFXRenderer(global::UnityEngine.EntityId id) : base(id) {}
        ///<exclude />
        [UnityEngine.Scripting.RequiredMember]
        public VFXRenderer()
        {
        }
    }

    ///<summary>This structure provides information data on a particle system.</summary>
    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/Systems/VFXParticleSystem.h")]
    public struct VFXParticleSystemInfo
    {
        ///<summary>Number of alive particles within the particle system, the value is lower than <see cref="capacity"/>.</summary>
        public uint aliveCount;
        ///<summary>The capacity (maximum <see cref="aliveCount" />) of the particle system.</summary>
        public uint capacity;
        ///<summary>The sleep state of the particle system.</summary>
        ///<remarks>Unity skips render and dispatch commands while a particle system sleeps. The particle system wakes up if a spawn context triggers new hits.</remarks>
        public bool sleeping;
        ///<summary>The rendering bound of this particle system.</summary>
        ///<remarks>
        ///  <see cref="VisualEffect.culled" /> relies on the union of every particles system rendering bounds.</remarks>
        public Bounds bounds;

        ///<exclude />
        public VFXParticleSystemInfo(uint aliveCount, uint capacity, bool sleeping, Bounds bounds)
        {
            this.aliveCount = aliveCount;
            this.capacity = capacity;
            this.sleeping = sleeping;
            this.bounds = bounds;
        }
    }
}
