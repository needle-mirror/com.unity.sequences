using System.Collections.Generic;
using System.Linq;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Sequences.Timeline
{
    public static partial class TimelineAssetExtensions
    {
        public static T GetTrack<T>(this TimelineAsset asset, string name) where T : TrackAsset
        {
            var allTracks = asset.GetRootTracks().Concat(asset.GetOutputTracks());
            foreach (var item in allTracks)
            {
                if (item is T && item.name == name)
                    return item as T;
            }

            return null;
        }

        public static T GetOrCreateTrack<T>(this TimelineAsset asset, string name)
            where T : TrackAsset, new()
        {
            return asset.GetTrack<T>(name) ?? asset.CreateTrack<T>(name);
        }

        /// <summary>
        /// Find a PlayableDirector in scenes who references the given TimelineAsset.
        /// </summary>
        /// <param name="timeline">TimelineAsset used by a PlayableDirector.</param>
        /// <returns>A valid PlayableDirector reference if one is found. Otherwise, it returns null.</returns>
        public static PlayableDirector FindDirector(this TimelineAsset timeline)
        {
            if (timeline == null)
                throw new System.NullReferenceException("timeline");

            PlayableDirector[] playables = Resources.FindObjectsOfTypeAll<PlayableDirector>();
            foreach (var playable in playables)
            {
                if (playable.gameObject.scene == default)
                    continue;

                if (playable.playableAsset == timeline)
                    return playable;
            }
            return null;
        }

        internal static IEnumerable<TimelineClip> GetSequenceAssetClips(this TimelineAsset timeline)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (!(track is SequenceAssetTrack))
                    continue;

                foreach (var clip in track.GetClips())
                {
                    var clipAsset = clip.asset as SequenceAssetPlayableAsset;
                    if (clipAsset != null)
                        yield return clip;
                }
            }
        }
    }
}
