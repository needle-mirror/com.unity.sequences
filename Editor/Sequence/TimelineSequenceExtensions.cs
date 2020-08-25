using System;
using System.IO;
using System.Linq;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    /// <summary>
    /// This class extends the TimelineSequence asset management with Unity Editor basic capabilities: save, delete,
    /// rename, record.
    /// </summary>
    public static class TimelineSequenceExtensions
    {
        /// <summary>
        /// Saves the specified Sequence on disk. Technically speaking, this method saves the TimelineAsset associated
        /// to the specified Sequence in the folder of the MasterSequence.
        /// </summary>
        /// <param name="clip">The Sequence to save.</param>
        /// <param name="folder">Optional sub-folders of the Assets/Sequences folder to save the MasterSequence asset to.
        /// The method creates the specified sub-folders if they doesn't already exist.</param>
        public static void Save(this TimelineSequence clip, string folder = null)
        {
            folder = folder ?? GetParentSequencePath(clip);

            if (clip.timeline != null)
            {
                string timelinePath = null;
                if (!AssetDatabase.Contains(clip.timeline))
                    timelinePath = SequencesAssetDatabase.GenerateUniqueMasterSequencePath(clip.timeline.name, folder, ".playable");

                SequencesAssetDatabase.SaveAsset(clip.timeline, timelinePath);
            }
        }

        /// <summary>
        /// Renames the specified Sequence. Technically speaking, this method renames the TimelineAsset associated to
        /// the specified Sequence.
        /// </summary>
        /// <param name="clip">The Sequence to rename.</param>
        /// <param name="newName">The new name to use.</param>
        public static void Rename(this TimelineSequence clip, string newName)
        {
            if (TimelineSequence.IsNullOrEmpty(clip) || !SequencesAssetDatabase.IsRenameValid(clip.name, newName))
                return;

            SequencesAssetDatabase.RenameAsset(clip.timeline, newName + "_Timeline");
            SequencesAssetDatabase.RenameSequenceFolder(clip, newName);

            // Parent track hold the name of the editorial clip.
            // This makes sure the rename gets reported to this asset.
            if (clip.editorialClip != null)
                EditorUtility.SetDirty(clip.editorialClip.GetParentTrack());

            clip.name = newName;
        }

        /// <summary>
        /// Deletes the specified Sequence from disk. Technically speaking, this method deletes the TimelineAsset
        /// associated to the specified Sequence.
        /// </summary>
        /// <param name="clip">The Sequence to delete.</param>
        public static void Delete(this TimelineSequence clip)
        {
            if (TimelineSequence.IsNullOrEmpty(clip))
                return;

            SequencesAssetDatabase.DeleteAsset(clip.timeline);
        }

        /// <summary>
        /// Records a sequence using its <see cref="Sequence.start"/>/<see cref="Sequence.end"/> time,
        /// <see cref="Sequence.fps"/> values and <see cref="Sequence.name"/> to set up the Recorder settings.
        /// </summary>
        /// <param name="clip">The Sequence to record.</param>
        /// <param name="recordAs">True to open the RecorderWindow and allow manual parameterization before recording,
        /// false to launch a record with current settings.</param>
        public static void Record(this TimelineSequence clip, bool recordAs = false)
        {
            var controllerSettings = RecorderControllerSettings.GetGlobalSettings();

            clip.GetRecordFrameStartAndEnd(out var frameStart, out var frameEnd);
            controllerSettings.SetRecordModeToFrameInterval(frameStart, frameEnd);
            controllerSettings.FrameRate = clip.fps;

            // TODO: It would be best to use Recorder token to set the path (could be by adding a <MasterSequence> token)
            foreach (var recorderSettings in controllerSettings.RecorderSettings)
                recorderSettings.FileNameGenerator.Leaf = Path.Combine("Recordings", SequencesAssetDatabase.GetSequenceContextPath(clip));

            // TODO: When Recorder will allow to start a record without opening the RecorderWindow, this code will need
            //       to be refactored to not open the RecorderWindow when not needed (i.e. recordAs is false and at
            //       least one recorder exist and is enabled).
            var recorderWindow = EditorWindow.GetWindow<RecorderWindow>();
            recorderWindow.titleContent = new GUIContent($"Record {clip.name}");
            recorderWindow.SetRecorderControllerSettings(controllerSettings);

            var settings = controllerSettings.RecorderSettings.ToList();
            if (recorderWindow != null && !recordAs && settings.Count > 0 && settings.Any(item => item.Enabled))
            {
                // Start the recording immediately if recordAs is false and if there is at least one enabled recorder.
                recorderWindow.StartRecording();
            }
        }

        /// <summary>
        /// Get the right frame start and end to setup records.
        /// </summary>
        /// <param name="clip">The Sequence to record and use to compute start and end frames.</param>
        /// <param name="frameStart">The computed start frame number (as an integer).</param>
        /// <param name="frameEnd">The computed end frame number (as an integer).</param>
        /// <remarks>This conversion is mainly used to workaround the fact that if we provide time in second to
        /// recorder, then the output record contains 1 extra frame at the end that is not wanted.</remarks>
        internal static void GetRecordFrameStartAndEnd(this TimelineSequence clip, out int frameStart, out int frameEnd)
        {
            frameStart = (int)Math.Ceiling(clip.start * clip.fps);
            frameEnd = (int)Math.Ceiling(clip.end * clip.fps) - 1;
        }

        static string GetParentSequencePath(this Sequence clip)
        {
            if (clip.parent == null) return "";

            var path = clip.parent != null ? GetParentSequencePath(clip.parent as TimelineSequence) : "";
            return Path.Combine(path, clip.parent.name);
        }
    }
}
