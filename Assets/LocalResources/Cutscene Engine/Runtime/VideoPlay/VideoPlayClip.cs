using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;

namespace CutsceneEngine
{
    public class VideoPlayClip : PlayableAsset, ITimelineClipAsset
    {
        public override double duration
        {
            get
            {
                if (!video) return base.duration;
                return video.length;
            }
        }

        public ClipCaps clipCaps
        {
            get
            {
                var caps = ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Extrapolation;
                if (loop) caps |= ClipCaps.Looping;
                return caps;
            }
        }

        [Tooltip("The video clip to play.")]
        public VideoClip video;
        [Tooltip("Whether to loop the video.")]
        public bool loop;
        [Tooltip("The aspect ratio of the video.")]
        public VideoAspectRatio aspectRatio = VideoAspectRatio.FitOutside;
        
        [Tooltip("The render target of the video.")]
        public VideoRenderTarget renderTarget = VideoRenderTarget.Screen;
        [Tooltip("The sorting order of the render target.")]
        public int sortingOrder;
        [HideInInspector]
        [Tooltip("The render texture to render the video to.")]
        public RenderTexture renderTexture;
        
        [Tooltip("The audio output target.")]
        public VideoAudioOutputTarget audioOutputTarget = VideoAudioOutputTarget.Direct;
        [HideInInspector]
        [Tooltip("The audio source to play the audio from.")]
        public ExposedReference<AudioSource> audioSource;
        [Tooltip("The audio volume.")]
        [Range(0, 1)]public float audioVolume = 1;
        
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VideoPlayBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            
            var audio = audioSource.Resolve(graph.GetResolver());

            behaviour.owner = owner;
            behaviour.video = video;
            behaviour.loop = loop;
            behaviour.aspectRatio = aspectRatio;
            behaviour.renderTarget = renderTarget;
            behaviour.sortingOrder = sortingOrder;
            behaviour.renderTexture = renderTexture;
            behaviour.audioOutputTarget = audioOutputTarget;
            behaviour.audioSource = audio;
            behaviour.audioVolume = audioVolume;

            return playable;
        }
        

    }
}
