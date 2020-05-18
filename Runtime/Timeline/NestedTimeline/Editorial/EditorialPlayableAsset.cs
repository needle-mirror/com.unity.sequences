using System;
using System.ComponentModel;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace UnityEngine.Sequences.Timeline
{
    /// <summary>
    /// EditorialPlayableAsset controls the PlayableDirector of a Sequence.
    /// </summary>
    [DisplayName("Editorial Clip")]
    public class EditorialPlayableAsset : NestedTimelinePlayableAsset, ITimelineClipAsset
    {
        // TODO: Hide the `director` ExposedReference by a Sequence.
        // TODO: Add validation to ensure the controlled PlayableDirector belong to an actual Sequence.

        public ClipCaps clipCaps => ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;

        internal double GetEditorialContentDuration(PlayableDirector inspectedDirector)
        {
            var resolvedDirector = director.Resolve(inspectedDirector);
            if (resolvedDirector == null)
                return 0.0;

            var timeline = resolvedDirector.playableAsset as TimelineAsset;
            if (timeline == null)
                return 0.0;

            var editorialDuration = 0.0;
            foreach (var track in timeline.GetOutputTracks())
            {
                var editorialTrack = track as EditorialTrack;
                if (editorialTrack == null)
                    continue;

                editorialDuration = Math.Max(editorialDuration, editorialTrack.GetActualDuration());
            }

            return editorialDuration;
        }
    }
}
