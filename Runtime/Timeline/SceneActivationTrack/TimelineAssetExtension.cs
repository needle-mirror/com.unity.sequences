using System.Collections.Generic;
using UnityEngine.Timeline;

namespace UnityEngine.Sequences.Timeline
{
    public static class TimelineAssetExtension
    {
        /// <summary>
        /// Gets a collection of scene paths found in the given timeline.
        /// </summary>
        /// <param name="timeline"></param>
        /// <returns></returns>
        public static IReadOnlyCollection<string> GetScenes(this TimelineAsset timeline)
        {
            List<string> paths = new List<string>();

            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                if (!(track is SceneActivationTrack) || track.muted)
                    continue;

                SceneActivationTrack scene = track as SceneActivationTrack;
                string path = scene.scene.path;

                if (string.IsNullOrEmpty(path))
                    continue;

                paths.Add(path);
            }

            return paths;
        }
    }
}
