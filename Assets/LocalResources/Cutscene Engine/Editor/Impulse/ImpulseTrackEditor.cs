using CutsceneEngine;
#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(ImpulseTrack))]
    public class ImpulseTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            options.icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cutscene Engine/Editor/Icons/Impulse Icon.png");

            var director = TimelineEditor.inspectedDirector;
            if (director)
            {
#if CINEMACHINE_3_OR_NEWER
                var cutsceneCam = director.GetComponentInChildren<CinemachineCamera>();
                if (cutsceneCam)
                {
                    var impulseListener = director.GetComponentInChildren<CinemachineImpulseListener>();
                    if (!impulseListener)
                    {
                        options.errorText = "No CinemachineImpulseListener found in the cameras. \n" +
                            "Without that component, impulse signals cannot be received.";
                    }
                    else if(!impulseListener.ReactionSettings.m_SecondaryNoise)
                    {
                        options.errorText =
                            "The SecondaryNoise setting of CinemachineImpulseListener is empty. \n" +
                            "This causes the camera shake effect to not be applied properly.";
                    }
                }
#elif CINEMACHINE_2_8_OR_NEWER
                var cutsceneCam = director.GetComponentInChildren<CinemachineVirtualCamera>();
                if (cutsceneCam)
                {
                    var impulseListener = director.GetComponentInChildren<CinemachineImpulseListener>();
                    if (!impulseListener)
                    {
                        options.errorText = "No CinemachineImpulseListener found in the cameras. \n" +
                            "Without that component, impulse signals cannot be received.";
                    }
                    else if(!impulseListener.m_ReactionSettings.m_SecondaryNoise)
                    {
                        options.errorText =
                            "The SecondaryNoise setting of CinemachineImpulseListener is empty. \n" +
                            "This causes the camera shake effect to not be applied properly.";
                    }
                }
#elif CINEMACHINE
                var cutsceneCam = director.GetComponentInChildren<CinemachineVirtualCamera>();
                if (cutsceneCam)
                {
                    var impulseListener = director.GetComponentInChildren<CinemachineImpulseListener>();
                    if (!impulseListener)
                    {
                        options.errorText = "No CinemachineImpulseListener found in the cameras. \n" +
                                            "Without that component, impulse signals cannot be received.";
                    }
                }
#else
                options.errorText = "You need to install the Cinemachine package to use the impulse feature.";
#endif
            }
            
            return options;
        }
    }
}