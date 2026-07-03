namespace CutsceneEngine
{
    public enum CutsceneState
    {
        /// <summary> The cutscene is stopped or ended or not played yet. </summary>
        None,
        /// <summary> The cutscene is playing and its local time is advancing. </summary>
        Playing,
        /// <summary> The cutscene is paused while playing. </summary>
        Paused
    }
}