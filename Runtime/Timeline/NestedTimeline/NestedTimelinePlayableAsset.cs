using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Sequences.Timeline
{
    /// <summary>
    /// Base class for all PlayableAsset that controls another PlayableDirector.
    /// </summary>
    [System.Serializable]
    [HideInMenu]
    public abstract class NestedTimelinePlayableAsset : PlayableAsset
    {
        public ExposedReference<PlayableDirector> director;
        double m_SubTimelineLength = 0.0;

        internal double subTimelineLength
        {
            set => m_SubTimelineLength = value;
        }

        public override double duration => m_SubTimelineLength;

        /// <summary>
        /// Create two playables, one to control the PlayableDirector of this asset and another to control the
        /// activation of this PlayableDirector.
        /// </summary>
        /// <param name="graph">The graph to inject playables into.</param>
        /// <param name="owner">The game object which initiated the build.</param>
        /// <returns>The root playable of all the playables injected.</returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playableDirector = director.Resolve(graph.GetResolver());
            if (playableDirector == null)
                return Playable.Create(graph);

            playableDirector.playOnAwake = false;

            var playable = Playable.Create(graph, 2);

            var directorPlayable = DirectorControlPlayable.Create(graph, playableDirector);
            graph.Connect(directorPlayable, 0, playable, 0);
            playable.SetInputWeight(directorPlayable, 1.0f);

            var activationPlayable = ActivationControlPlayable.Create(
                graph,
                GetGameObjectToActivate(playableDirector),
                ActivationControlPlayable.PostPlaybackState.Revert);
            graph.Connect(activationPlayable, 0, playable, 1);
            playable.SetInputWeight(activationPlayable, 1.0f);

            playable.SetPropagateSetTime(true);
            return playable;
        }

        internal virtual GameObject GetGameObjectToActivate(PlayableDirector playableDirector)
        {
            return playableDirector.gameObject;
        }
    }
}
