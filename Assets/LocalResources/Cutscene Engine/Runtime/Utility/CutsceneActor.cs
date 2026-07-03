using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CutsceneEngine
{
    /// <summary>
    /// Marks a hierarchy as the visual source that can be cloned for a matching <see cref="CutsceneActorPreview"/>.
    /// </summary>
    [AddComponentMenu("Cutscene Engine/Cutscene Actor (Cutscene Engine)")]
    public class CutsceneActor : MonoBehaviour
    {
        static readonly List<CutsceneActor> Instances = new List<CutsceneActor>();

        [SerializeField] string key = "actor1";

        /// <summary> Unique key shared with <see cref="CutsceneActorPreview"/> instances. </summary>
        public string Key => key;

        public delegate void TransformInitializationCallback(Vector3 position, Quaternion rotation);

        /// <summary>
        /// This event is called when a cutscene begins and the actor's position is reset to the Actor Preview location.
        /// This can be considered the timing when the cutscene begins.
        /// If the character's position cannot be changed using Transform,
        /// you can use custom code to directly change the character's position by connecting this event.
        /// </summary>
        public event TransformInitializationCallback onTransformInitialized;
        
        /// <summary>
        /// An event called immediately after a cutscene ends and the player is unbound.
        /// This can be used as a callback to re-enable player input, for example.
        /// </summary>
        public event Action onResetBinding;
        
        void OnEnable()
        {
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }

        void Register()
        {
            if (!Instances.Contains(this))
            {
                Instances.Add(this);
            }
        }

        void Unregister()
        {
            Instances.Remove(this);
        }

        internal void InitializeTransform(Vector3 position, Quaternion rotation)
        {
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            onTransformInitialized?.Invoke(position, rotation);
            if (cc) cc.enabled = true;
        }

        internal void OnResetBinding()
        {
            onResetBinding?.Invoke();
        }

        /// <summary> Finds an active origin that matches the provided key. </summary>
        public static CutsceneActor Find(string lookupKey)
        {
            if (string.IsNullOrWhiteSpace(lookupKey)) return null;

            for (var i = Instances.Count - 1; i >= 0; i--)
            {
                var origin = Instances[i];
                if (!origin)
                {
                    Instances.RemoveAt(i);
                    continue;
                }

                if (!origin.isActiveAndEnabled) continue;
                if (origin.Key == lookupKey) return origin;
            }

            return null;
        }
    }
}
