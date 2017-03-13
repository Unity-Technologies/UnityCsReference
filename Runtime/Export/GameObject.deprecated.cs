// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public partial class GameObject
    {
        [System.Obsolete("GameObject.SampleAnimation(AnimationClip, float) has been deprecated. Use AnimationClip.SampleAnimation(GameObject, float) instead (UnityUpgradable).", true)]
        public void SampleAnimation(UnityEngine.Object clip, float time)
        {
            throw new NotSupportedException("GameObject.SampleAnimation is deprecated");
        }

        [System.Obsolete("GameObject.AddComponent with string argument has been deprecated. Use GameObject.AddComponent<T>() instead. (UnityUpgradable).", true)]
        public Component AddComponent(string className)
        {
            throw new NotSupportedException("AddComponent(string) is deprecated");
        }

        [Obsolete("Property rigidbody has been deprecated. Use GetComponent<Rigidbody>() instead. (UnityUpgradable)", true)]
        public Component rigidbody
        {
            get { throw new NotSupportedException("rigidbody property has been deprecated"); }
        }

        [Obsolete("Property rigidbody2D has been deprecated. Use GetComponent<Rigidbody2D>() instead. (UnityUpgradable)", true)]
        public Component rigidbody2D
        {
            get { throw new NotSupportedException("rigidbody2D property has been deprecated"); }
        }

        [Obsolete("Property camera has been deprecated. Use GetComponent<Camera>() instead. (UnityUpgradable)", true)]
        public Component camera
        {
            get { throw new NotSupportedException("camera property has been deprecated"); }
        }

        [Obsolete("Property light has been deprecated. Use GetComponent<Light>() instead. (UnityUpgradable)", true)]
        public Component light
        {
            get { throw new NotSupportedException("light property has been deprecated"); }
        }

        [Obsolete("Property animation has been deprecated. Use GetComponent<Animation>() instead. (UnityUpgradable)", true)]
        public Component animation
        {
            get { throw new NotSupportedException("animation property has been deprecated"); }
        }

        [Obsolete("Property constantForce has been deprecated. Use GetComponent<ConstantForce>() instead. (UnityUpgradable)", true)]
        public Component constantForce
        {
            get { throw new NotSupportedException("constantForce property has been deprecated"); }
        }

        [Obsolete("Property renderer has been deprecated. Use GetComponent<Renderer>() instead. (UnityUpgradable)", true)]
        public Component renderer
        {
            get { throw new NotSupportedException("renderer property has been deprecated"); }
        }

        [Obsolete("Property audio has been deprecated. Use GetComponent<AudioSource>() instead. (UnityUpgradable)", true)]
        public Component audio
        {
            get { throw new NotSupportedException("audio property has been deprecated"); }
        }

        [Obsolete("Property guiText has been deprecated. Use GetComponent<GUIText>() instead. (UnityUpgradable)", true)]
        public Component guiText
        {
            get { throw new NotSupportedException("guiText property has been deprecated"); }
        }

        [Obsolete("Property networkView has been deprecated. Use GetComponent<NetworkView>() instead. (UnityUpgradable)", true)]
        public Component networkView
        {
            get { throw new NotSupportedException("networkView property has been deprecated"); }
        }

        [Obsolete("Property guiElement has been deprecated. Use GetComponent<GUIElement>() instead. (UnityUpgradable)", true)]
        public Component guiElement
        {
            get { throw new NotSupportedException("guiElement property has been deprecated"); }
        }

        [Obsolete("Property guiTexture has been deprecated. Use GetComponent<GUITexture>() instead. (UnityUpgradable)", true)]
        public Component guiTexture
        {
            get { throw new NotSupportedException("guiTexture property has been deprecated"); }
        }

        [Obsolete("Property collider has been deprecated. Use GetComponent<Collider>() instead. (UnityUpgradable)", true)]
        public Component collider
        {
            get { throw new NotSupportedException("collider property has been deprecated"); }
        }

        [Obsolete("Property collider2D has been deprecated. Use GetComponent<Collider2D>() instead. (UnityUpgradable)", true)]
        public Component collider2D
        {
            get { throw new NotSupportedException("collider2D property has been deprecated"); }
        }

        [Obsolete("Property hingeJoint has been deprecated. Use GetComponent<HingeJoint>() instead. (UnityUpgradable)", true)]
        public Component hingeJoint
        {
            get { throw new NotSupportedException("hingeJoint property has been deprecated"); }
        }

        [Obsolete("Property particleEmitter has been deprecated. Use GetComponent<ParticleEmitter>() instead. (UnityUpgradable)", true)]
        public Component particleEmitter
        {
            get { throw new NotSupportedException("particleEmitter property has been deprecated"); }
        }

        [Obsolete("Property particleSystem has been deprecated. Use GetComponent<ParticleSystem>() instead. (UnityUpgradable)", true)]
        public Component particleSystem
        {
            get { throw new NotSupportedException("particleSystem property has been deprecated"); }
        }

        [Obsolete("gameObject.PlayAnimation is not supported anymore. Use animation.Play()", true)]
        public void PlayAnimation(UnityEngine.Object animation)
        {
            throw new NotSupportedException("gameObject.PlayAnimation is not supported anymore. Use animation.Play();");
        }

        [Obsolete("gameObject.StopAnimation is not supported anymore. Use animation.Stop()", true)]
        public void StopAnimation()
        {
            throw new NotSupportedException("gameObject.StopAnimation(); is not supported anymore. Use animation.Stop();");
        }
    }
}
