using CutsceneEngine;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(CameraOverlapTrack))]
    public class CameraOverlapTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);

            var bindingCamera = binding as Camera;
            var mainCamera = Camera.main;
            if(bindingCamera && mainCamera)
            {
                if (bindingCamera == mainCamera)
                {
                    options.errorText = "WARNING: NO CUTSCENE CAMERA\n\n" +
                                        "Setting the main camera as a cutscene camera is not recomended.\n" +
                                        "For the sake of cutscene's modularity, it is recommended to have a separate camera for cutscenes.";
                }
                else if (bindingCamera.depth <= mainCamera.depth)
                {
                    options.errorText = "WARNING: CAMERA PRIORITY\n\n" +
                                         "Setting the main camera as a cutscene camera is not recomended.\n" +
                                         "For the sake of cutscene modularity, it is recommended to have a separate camera for cutscenes.";
                }
            }

            return options;
        }
    }
}