using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.Sequences.Timeline;
using UnityEngine.Timeline;

namespace UnityEditor.Sequences
{
    /// <summary>
    /// Cache tracking all  of the project
    /// </summary>
    [FilePath("Library/SequenceCache.index", FilePathAttribute.Location.ProjectFolder)]
    class SequenceIndexer : ScriptableSingleton<SequenceIndexer>
    {
        /// <summary>
        /// Event raised when there a new Sequence is created in the project.
        /// When a Structure is imported, this event is called for each Sequence.
        /// </summary>
        internal static event Action<SequenceNode> sequenceRegistered;

        /// <summary>
        /// Event raised when there any kind of update in an existing Sequence.
        /// </summary>
        internal static event Action<SequenceNode> sequenceUpdated;

        /// <summary>
        /// Event raised when at least one Sequence has been deleted from the project.
        /// </summary>
        internal static event Action sequencesRemoved;

        internal static event Action indexerInitialized;

        internal static event Action validityChanged;

        [SerializeReference] List<SequenceNode> m_Sequences = new List<SequenceNode>();

        internal bool isEmpty => m_Sequences.Count == 0;

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            string indexerFilePath = Path.Combine(Application.dataPath, "..", GetFilePath());
            if (!File.Exists(indexerFilePath))
            {
                // Delay the initialize call because operations with AssetDatabase must wait the Editor to refresh at least once.
                EditorApplication.delayCall += InitializeWithExistingData;
            }
            else
            {
                // Remove null references. Might happen if files were removed directly from the filesystem.
                instance.PruneDeletedElement();
            }

            instance.RegisterCallbacks();
        }

        /// <summary>
        /// Scan the project for TimelineAssets and initialize the cache with the data found.
        /// </summary>
        internal static void InitializeWithExistingData()
        {
            RepairEditorialTimelineReferencesInLegacyMasterSequences();

            using (new DisableEvent())
            {
                foreach (TimelineAsset timeline in FindAllTimelines())
                    instance.TraverseAndProcess(timeline);
            }

            indexerInitialized?.Invoke();
        }

        void RegisterCallbacks()
        {
            EditorApplication.update += RecomputeSequencesValidity;
        }

        int GetIndexOf(TimelineAsset timeline)
        {
            return m_Sequences.FindIndex(sequence => sequence.timeline == timeline);
        }

        /// <summary>
        /// Retrieves all the TimelineAssets from the project.
        /// </summary>
        /// <returns></returns>
        static IEnumerable<TimelineAsset> FindAllTimelines()
        {
            var timelinesGuids = AssetDatabase.FindAssets("glob:\"*.playable\"");
            foreach (var guid in timelinesGuids)
            {
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (timeline == null)
                    continue;

                yield return timeline;
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        internal static void Clear()
        {
            instance.m_Sequences.Clear();
            instance.Save(true);
        }

        /// <summary>
        /// Traverse the specified timeline tracks and sub-timelines to find every sequences. Then those sequences are
        /// processed to update the indexer.
        /// </summary>
        internal void TraverseAndProcess(TimelineAsset timeline)
        {
            var sequences = Traverse(timeline, null);
            ProcessSequences(sequences);
        }

        List<SequenceNode> Traverse(TimelineAsset timeline, SequenceNode parent)
        {
            var results = new List<SequenceNode>();
            var sequence = GetOrCreateSequence(timeline);

            // Override the parent sequence if one is specified.
            if (parent != null)
                sequence.parent = parent;

            sequence.RemoveAllChildren(); // Reset the children list.

            results.Add(sequence);

            bool hasEditorialTrack = false;
            foreach (var editorialTrack in timeline.GetEditorialTracks())
            {
                hasEditorialTrack = true;

                foreach (var clip in editorialTrack.GetClips())
                {
                    var editorialAssetClip = clip.asset as EditorialPlayableAsset;
                    if (editorialAssetClip == null || editorialAssetClip.timeline == null)
                        continue;

                    var children = Traverse(editorialAssetClip.timeline, sequence);

                    children[0].editorialClip = clip;
                    if (!sequence.IsParentOf(children[0]))
                        sequence.AddChild(children[0]);

                    results.AddRange(children);
                }
            }

            if (!hasEditorialTrack && sequence.parent == null && !MasterSequenceUtility.IsLegacyMasterTimeline(timeline))
                results.Clear();

            return results;
        }

        // Processes a provided collection of Sequences.
        // Will register a Sequence if it's new or update the existing data.
        void ProcessSequences(IList<SequenceNode> sequences)
        {
            if (sequences.Count == 0)
                return;

            foreach (var sequence in sequences)
            {
                if (m_Sequences.Contains(sequence))
                    UpdateSequence(sequence);
                else
                    RegisterSequence(sequence);
            }
        }

        /// <summary>
        /// Always returns a valid <see cref="SequenceNode"/> instance for the provided <see cref="TimelineAsset"/>.
        /// If it already exists, returns it. Otherwise, this method will return a newly created instance.
        /// </summary>
        /// <param name="timeline"></param>
        /// <returns></returns>
        SequenceNode GetOrCreateSequence(TimelineAsset timeline)
        {
            int idx = GetIndexOf(timeline);
            if (idx < 0)
            {
                var newNode = new SequenceNode() { timeline = timeline };
                return newNode;
            }

            return m_Sequences[idx];
        }

        /// <summary>
        /// Gets the associated <see cref="SequenceNode"/> instance for the provided Timeline.
        /// </summary>
        /// <param name="timeline"></param>
        /// <returns>A valid Sequence. If the TimelineAsset is not registered, returns null.</returns>
        internal SequenceNode GetSequence(TimelineAsset timeline)
        {
            int idx = GetIndexOf(timeline);
            if (idx < 0)
                return null;

            return m_Sequences[idx];
        }

        // Add a new SequenceNode to the indexer.
        void RegisterSequence(SequenceNode sequence)
        {
            m_Sequences.Add(sequence);
            Save(true);

            if (!disableEvents)
                sequenceRegistered?.Invoke(sequence);
        }

        // Update an existing SequenceNode in the indexer.
        void UpdateSequence(SequenceNode sequence)
        {
            Save(true);

            if (!disableEvents)
                sequenceUpdated?.Invoke(sequence);
        }

        // Recompute the validity status and loading status of all sequences.
        void RecomputeSequencesValidity()
        {
            var hasValidityChanged = false;
            foreach (var sequence in m_Sequences)
                hasValidityChanged |= sequence.ComputeValidity();

            if (!disableEvents && hasValidityChanged)
                validityChanged?.Invoke();
        }

        /// <summary>
        /// Removes all Sequences with a null reference to a <see cref="TimelineAsset"/>.
        /// </summary>
        internal void PruneDeletedElement()
        {
            bool hadDeletedAtLeastOneThing = false;

            // To avoid being impacted by the size change of the list we go through
            // it from the end to beginning and remove null elements as we go.
            for (int i = m_Sequences.Count - 1; i >= 0; --i)
            {
                var sequence = m_Sequences[i];
                if (sequence.timeline == null)
                {
                    if (sequence.parent != null)
                        sequence.parent.RemoveChild(sequence);

                    hadDeletedAtLeastOneThing = true;
                    m_Sequences.Remove(sequence);
                }
            }

            if (hadDeletedAtLeastOneThing)
            {
                Save(true);
                if (!disableEvents)
                    sequencesRemoved?.Invoke();
            }
        }

        /// <summary>
        /// Uses the sequences hierarchy serialized in MasterSequence.manager to set EditorialPlayableAsset.timeline
        /// properties in master sequence timelines. This fixes projects being upgraded from versions older than 2.0.0.
        /// EditorialPlayableAsset.timeline didn't exist in these earlier versions.
        /// </summary>
        static void RepairEditorialTimelineReferencesInLegacyMasterSequences()
        {
            foreach (var masterSequence in MasterSequenceUtility.GetLegacyMasterSequences())
            {
                var timelineSequences = masterSequence.manager.sequences
                    .OfType<TimelineSequence>()
                    .ToArray();

                var editorialAssetTimelineMap = timelineSequences
                    .Where(sequence => sequence.editorialClip != null)
                    .ToDictionary(sequence => sequence.editorialClip.asset as EditorialPlayableAsset,
                        sequence => sequence.timeline);

                foreach (var sequence in timelineSequences)
                {
                    var editorialAssetsMissingTimeline = sequence.timeline
                        .GetEditorialClips()
                        .Select(clip => clip.asset as EditorialPlayableAsset)
                        .Where(editorial => editorial != null && editorial.timeline == null);

                    var didRepair = false;

                    foreach (var editorialAsset in editorialAssetsMissingTimeline)
                    {
                        if (editorialAssetTimelineMap.TryGetValue(editorialAsset, out var timeline))
                        {
                            editorialAsset.timeline = timeline;
                            didRepair = true;
                        }
                    }

                    if (didRepair)
                        SequencesAssetDatabase.SaveAsset(sequence.timeline);
                }
            }
        }

        bool disableEvents { get; set; }

        // Context to momentarily disable the events from the SequencesIndexer.
        internal class DisableEvent : IDisposable
        {
            public DisableEvent()
            {
                instance.disableEvents = true;
            }

            public void Dispose()
            {
                instance.disableEvents = false;
            }
        }
    }
}
