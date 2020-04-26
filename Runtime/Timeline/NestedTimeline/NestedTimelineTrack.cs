using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Sequences.Timeline
{
    /// <summary>
    /// Base class for all Track that host clips that controls other PlayableDirector.
    /// </summary>
    [HideInMenu]
    public abstract class NestedTimelineTrack : TrackAsset
    {
        static readonly HashSet<PlayableDirector> s_ProcessedDirectors = new HashSet<PlayableDirector>();

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            if (director == null)
                return;

            // Avoid recursion
            if (s_ProcessedDirectors.Contains(director))
                return;

            s_ProcessedDirectors.Add(director);
            foreach (var clip in GetClips())
            {
                var clipAsset = clip.asset as NestedTimelinePlayableAsset;
                if (clipAsset == null)
                    continue;

                var resolvedDirector = clipAsset.director.Resolve(director);
                if (resolvedDirector == null)
                    continue;

                Preview(clipAsset, resolvedDirector, driver);
            }
            s_ProcessedDirectors.Remove(director);
        }

        void Preview(NestedTimelinePlayableAsset clipAsset, PlayableDirector subDirector, IPropertyCollector driver)
        {
            if (clipAsset == null || subDirector == null)
                return;

            // Activation
            var gameObject = clipAsset.GetGameObjectToActivate(subDirector);
            driver.AddFromName(gameObject, "m_IsActive");

            // Propagate GatherProperties to sub timelines.
            var timeline = subDirector.playableAsset as TimelineAsset;
            if (timeline == null)
                return;

            timeline.GatherProperties(subDirector, driver);
        }
    }
}
