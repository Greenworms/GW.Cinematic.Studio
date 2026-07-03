#if URP || HDRP || UNITY_POST_PROCESSING_STACK_V2
using System;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;
#if URP || HDRP
using VolumeProfileAsset = UnityEngine.Rendering.VolumeProfile;
#elif UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
using VolumeProfileAsset = UnityEngine.Rendering.PostProcessing.PostProcessProfile;
#endif

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(VolumeClip))]
    public class VolumeClipEditor : ClipEditor
    {
        public override void OnClipChanged(TimelineClip clip)
        {
            if (clip.asset is VolumeClip c && c.volumeProfile && c.volumeProfile.name != clip.displayName)
            {
                Rename(clip, c.volumeProfile);
            }
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            var asset = TimelineEditor.inspectedAsset;
            if (!asset) return;

            var volumeClip = (VolumeClip)clip.asset;
            var profile = CreateProfileAsset(clip, clonedFrom);

            profile.name = clip.displayName;
            DuplicateVolumeSettings(profile);

            AssetDatabase.AddObjectToAsset(profile, asset);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);

            volumeClip.volumeProfile = profile;
        }

        static VolumeProfileAsset CreateProfileAsset(TimelineClip clip, TimelineClip clonedFrom)
        {
            VolumeProfileAsset profile;
            if (clonedFrom == null)
            {
                clip.displayName = DefaultClipName;
                profile = ScriptableObject.CreateInstance<VolumeProfileAsset>();
            }
            else
            {
                clip.displayName += "(Clone)";
                var sourceClip = (VolumeClip)clonedFrom.asset;
                if (sourceClip && sourceClip.volumeProfile)
                {
                    profile = Object.Instantiate(sourceClip.volumeProfile);
                }
                else
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfileAsset>();
                }

                if (clip.asset is VolumeClip clonedVolumeClip)
                {
                    if (clonedVolumeClip.volumeProfile) EditorUtility.SetDirty(clonedVolumeClip.volumeProfile);
                    EditorUtility.SetDirty(clonedVolumeClip);
                }
            }

            return profile;
        }

        internal static VolumeProfileAsset CreateProfileCopy(VolumeProfileAsset source, string profileName)
        {
            var profile = source
                ? Object.Instantiate(source)
                : ScriptableObject.CreateInstance<VolumeProfileAsset>();

            profile.name = profileName;
            DuplicateVolumeSettings(profile);
            return profile;
        }

        internal static void DuplicateVolumeSettings(VolumeProfileAsset profile)
        {
#if URP || HDRP
            for (var i = 0; i < profile.components.Count; i++)
            {
                profile.components[i] = Object.Instantiate(profile.components[i]);
            }
#else
            for (var i = 0; i < profile.settings.Count; i++)
            {
                profile.settings[i] = Object.Instantiate(profile.settings[i]);
            }
#endif
        }

        static void Rename(TimelineClip clip, VolumeProfileAsset profile)
        {
            profile.name = clip.displayName;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }

#if URP || HDRP
        const string DefaultClipName = "Volume";
#else
        const string DefaultClipName = "Post Process Volume";
#endif
    }
}
#endif
