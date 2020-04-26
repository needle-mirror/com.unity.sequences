#if UNITY_EDITOR
using System.ComponentModel;
#endif
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Sequences.Timeline
{
    [System.Serializable]
#if UNITY_EDITOR
    [DisplayName("Activation clip")]
#endif
    public class SceneActivationPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Create(graph);
        }
    }
}
