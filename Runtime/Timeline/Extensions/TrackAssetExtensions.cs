using System.Linq;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace UnityEngine.Sequences.Timeline
{
    public static class TrackAssetExtensions
    {
        // This function is only used in tests.
        public static T GetOrCreateSubTrackByName<T>(this TrackAsset asset, string name)
            where T : TrackAsset, new()
        {
            foreach (var item in asset.GetChildTracks())
            {
                if (item is T && item.name == name)
                    return item as T;
            }
            return asset.timelineAsset.CreateTrack<T>(asset, name);
        }

        // This function is only used in tests.
        public static T GetOrCreateFirstAssetClipOfType<T>(this TrackAsset track)
            where T : PlayableAsset
        {
            foreach (var clip in track.GetClips())
            {
                if (clip.asset is T)
                    return clip.asset as T;
            }

            return track.CreateClip<T>().asset as T;
        }

        public static TimelineClip GetFirstClip(this TrackAsset track)
        {
            return track.GetClips().FirstOrDefault();
        }

        public static TimelineClip GetFirstClipWithName(this TrackAsset track, string name)
        {
            return track.GetClips().FirstOrDefault(clip => clip.displayName == name);
        }

        public static T GetBinding<T>(this TrackAsset track, PlayableDirector director) where T : Object
        {
            if (!track.outputs.Any())
                return null;

            return director.GetGenericBinding(track) as T;
        }

        public static string GetBindingName(this TrackAsset track, PlayableDirector director)
        {
            var binding = track.GetBinding<Object>(director);
            return binding == null ? track.name : binding.name;
        }
    }
}
