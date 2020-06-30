using System;
using UnityEngine.Sequences.Timeline;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace UnityEngine.Sequences
{
    /// <summary>
    /// Component used to attach a MasterSequence's Sequence to its representation in GameObject. A GameObject with
    /// a SequenceFilter component should also have a PlayableDirector component that drives the Sequence's Timeline.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class SequenceFilter : MonoBehaviour
    {
        internal enum Type
        {
            MasterSequence,
            Sequence,
            Shot
        }

        [FormerlySerializedAs("m_Cinematic")][SerializeField] MasterSequence m_MasterSequence;
        [SerializeField] int m_ElementIndex;
        [SerializeField] internal Type type;

        /// <summary>
        /// The MasterSequence asset the Sequence is from.
        /// </summary>
        public MasterSequence masterSequence
        {
            get => m_MasterSequence;
            set => m_MasterSequence = value;
        }

        /// <summary>
        /// Index of the Sequence this GameObject represents.
        /// </summary>
        public int elementIndex
        {
            get => m_ElementIndex;
            set => m_ElementIndex = value;
        }

        /// <summary>
        /// Tell if this GameObject is already in the process of being destroyed.
        /// Set to true from OnDestroy().
        /// </summary>
        internal bool isBeingDestroyed { get; private set; }

        void Start()
        {
            if (masterSequence == null || TimelineSequence.IsNullOrEmpty(masterSequence.rootSequence))
                return;

            // Deactivate masterSequence's loaded scenes by default.
            foreach (var scenePath in masterSequence.rootSequence.GetRelatedScenes())
            {
                var scene = SceneActivationManager.GetScene(scenePath);
                if (!scene.isLoaded)
                    continue;

                var rootGameObjects = scene.GetRootGameObjects();

                foreach (GameObject root in rootGameObjects)
                    root.SetActive(false);
            }
        }

        /// <summary>
        /// Recursive function that generates the hierarchy tree for the MasterSequence dragged and dropped and binds
        /// the "item"'s playable director to the Timeline.
        /// </summary>
        /// <param name="masterSequenceent MasterSequence that owns the given item.</param>
        /// <param name="sequence">The TimelineSequence that needs its GameObject created in the hierarchy</param>
        /// <param name="parent">The parent transform of the item</param>
        /// <returns>GameObject created for the TimelineSequence (item) passed</returns>
        /// <exception cref="NullReferenceException">Exception is thrown when the item passed is null</exception>
        internal static GameObject GenerateSequenceRepresentation(MasterSequence masterSequence, TimelineSequence sequence, Transform parent)
        {
            if (sequence == null)
                throw new System.NullReferenceException("Can't build a representation of a null TimelineSequence. Provide a valid item.");

            GameObject child = new GameObject(sequence.name, typeof(PlayableDirector));

            SequenceFilter filter = child.gameObject.AddComponent<SequenceFilter>();
            filter.masterSequence = masterSequence;
            filter.elementIndex = masterSequence.manager.GetIndex(sequence);
            filter.type = (sequence.parent == null) ? Type.MasterSequence : sequence.parent.parent == null ? Type.Sequence : Type.Shot;

            if (parent)
                child.transform.SetParent(parent);

            PlayableDirector director = child.GetComponent<PlayableDirector>();
            director.playableAsset = sequence.timeline;

            foreach (Sequence childClip in sequence.children)
            {
                if (TimelineSequence.IsNullOrEmpty(childClip as TimelineSequence))
                    continue;

                var grandChild = GenerateSequenceRepresentation(masterSequence, childClip as TimelineSequence, child.transform);
                BindDirectorToTimeline(director, grandChild.GetComponent<PlayableDirector>(), childClip as TimelineSequence);
            }

            return child;
        }

        static void BindDirectorToTimeline(PlayableDirector parentDirector, PlayableDirector childDirector, TimelineSequence childClip)
        {
            if (childClip.editorialClip == null) return;

            var clipAsset = childClip.editorialClip.asset as EditorialPlayableAsset;
            // TODO: ttu - 2020-08-10: look for a clean solution
#if UNITY_EDITOR
            var guid = UnityEditor.GUID.Generate().ToString();
#else
            var guid = System.Guid.NewGuid().ToString();
#endif
            clipAsset.director.exposedName = new PropertyName(guid);
            parentDirector.SetReferenceValue(clipAsset.director.exposedName, childDirector);
        }
    }
}
