using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline track for animating Transform properties (position, rotation, scale) during cutscenes.
    /// Binds to Transform components and can optionally control GameObject activation.
    /// </summary>
    [TrackColor(1f, 0f, 0.25f)]
    [TrackBindingType(typeof(Transform))]
    [TrackClipType(typeof(TransformClip))]
    public class TransformTrack : TrackAsset
    {
        /// <summary> The initial position of the binding transform. Used for scene view previews. </summary>
        public Vector3 initialPos { get; private set; }
        /// <summary> The initial rotation of the binding transform. Used for scene view previews. </summary>
        public Quaternion initialRot { get; private set; }
        /// <summary> The initial local scale of the binding transform. Used for scene view previews. </summary>
        public Vector3 initialScale { get; private set; }

        [Tooltip(" If this field is true, it will activate the bound game object only in the area where the clip is located across the entire track.")]
        public bool controlActivation;

        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.duration = 1;
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject gameObject, int inputCount)
        {
            if (gameObject)
            {
                if (gameObject.TryGetComponent(out PlayableDirector director))
                {
                    var binding = director.GetGenericBinding(this) as Transform;
                    if (binding)
                    {
                        initialPos = binding.position;
                        initialRot = binding.rotation;
                        initialScale = binding.localScale;
                    }
                }
            }
            var playable = ScriptPlayable<TransformMixerBehaviour>.Create(graph, inputCount);
            playable.GetBehaviour().controlActivation = controlActivation;
            return playable;
        }
        
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            const string localPosition = "m_LocalPosition";
            const string localRotation = "m_LocalRotation";
            const string localScale = "m_LocalScale";
            
            var binding = director.GetGenericBinding(this) as Transform;

            if(binding)
            {
                driver.AddFromName<Transform>(binding.gameObject, localPosition + ".x");
                driver.AddFromName<Transform>(binding.gameObject, localPosition + ".y");
                driver.AddFromName<Transform>(binding.gameObject, localPosition + ".z");

                driver.AddFromName<Transform>(binding.gameObject, localRotation + ".x");
                driver.AddFromName<Transform>(binding.gameObject, localRotation + ".y");
                driver.AddFromName<Transform>(binding.gameObject, localRotation + ".z");
                driver.AddFromName<Transform>(binding.gameObject, localRotation + ".w");

                driver.AddFromName<Transform>(binding.gameObject, localScale + ".x");
                driver.AddFromName<Transform>(binding.gameObject, localScale + ".y");
                driver.AddFromName<Transform>(binding.gameObject, localScale + ".z");
            }
        }
    }
}
