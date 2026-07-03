using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
#if UNITY_POST_PROCESSING_STACK_V2 && !URP && !HDRP
using UnityEngine.Rendering.PostProcessing;
#endif

#if URP || HDRP
using VolumeProfileAsset = UnityEngine.Rendering.VolumeProfile;
using VolumeComponent = UnityEngine.Rendering.Volume;
#elif UNITY_POST_PROCESSING_STACK_V2
using VolumeProfileAsset = UnityEngine.Rendering.PostProcessing.PostProcessProfile;
using VolumeComponent = UnityEngine.Rendering.PostProcessing.PostProcessVolume;
#endif

namespace CutsceneEngine
{
    public class VolumeBehaviour : PlayableBehaviour
    {
#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
        public VolumeProfileAsset volumeProfile;
        public float volumePriority;
        public int volumeLayer;

        public VolumeComponent instancedVolume => _instancedVolume;
        VolumeComponent _instancedVolume;

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_instancedVolume) _instancedVolume.weight = info.effectiveWeight;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            UnInitialize();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Initialize();
            if (_instancedVolume) _instancedVolume.weight = info.effectiveWeight;
        }

        void Initialize()
        {
            if (_instancedVolume || !volumeProfile) return;
            var go = new GameObject($"Instanced {VolumeObjectName} ({volumeProfile.name})");
            go.layer = volumeLayer;
            go.hideFlags = HideFlags.HideAndDontSave;

            var volume = go.AddComponent<VolumeComponent>();
#if UNITY_POST_PROCESSING_STACK_V2 && !URP && !HDRP
            volume.isGlobal = true;
#endif
            volume.weight = 0;
            volume.priority = volumePriority;
            volume.sharedProfile = volumeProfile;
            volume.profile = volumeProfile;
            _instancedVolume = volume;
        }

        void UnInitialize()
        {
            if (_instancedVolume)
            {
                CutsceneEngineUtility.SmartDestroy(_instancedVolume.gameObject);
            }
        }

        static string VolumeObjectName =>
#if URP || HDRP
            "Volume";
#else
            "PostProcessVolume";
#endif
#endif
    }
}
