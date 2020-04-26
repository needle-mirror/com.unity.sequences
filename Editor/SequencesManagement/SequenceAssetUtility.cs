using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.Sequences.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Sequences
{
    /// <summary>
    /// SequenceAssetException are thrown each time a prefab is not a valid Sequence asset. For example if it
    /// doesn't have the SequenceAsset component, or if it's not a Regular or Variant prefab as expected.
    /// </summary>
    public class SequenceAssetException : Exception
    {
        public SequenceAssetException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// SequenceAssetUtility provides multiple functions to help create and manipulate Sequence asset prefabs and
    /// variants.
    /// A Sequence asset is a Regular or Variant prefab "tagged" using the <paramref name="SequenceAsset"/> component.
    /// Sequence assets are visible and manageable in the Sequences window and are prefabs ready to be used in
    /// a MasterSequence.
    /// </summary>
    public static class SequenceAssetUtility
    {
        /// <summary>
        /// Creates a new Regular prefab with the SequenceAsset component on it.
        /// </summary>
        /// <param name="name">The name of the prefab asset to create.</param>
        /// <param name="type">The type of the sequence asset that should be set on the SequenceAsset component. This
        /// helps categorize sequence assets.</param>
        /// <param name="animate">If true, a new TimelineAsset will be created and setup in the prefab. The
        /// TimelineAsset itself is saved in the AssetDatabase as a sub-asset of the Prefab asset.</param>
        /// <param name="instantiate">If true, the created Prefab asset will be instantiated in the current active
        /// scene.</param>
        /// <returns>The created Regular Prefab asset.</returns>
        public static GameObject CreateSource(string name, string type = null, bool animate = true, bool instantiate = false)
        {
            var sourceGo = new GameObject(name);
            var sequenceAsset = sourceGo.AddComponent<SequenceAsset>();
            sequenceAsset.type = type ?? CollectionType.undefined;

            var outputPath = GenerateUniqueAssetPath(sourceGo);
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                sourceGo,
                outputPath,
                InteractionMode.AutomatedAction);

            if (animate)
                SetupTimeline(prefab);

            if (!instantiate)
                Object.DestroyImmediate(sourceGo);

            EditorGUIUtility.PingObject(prefab);
            AssetDatabase.SaveAssets();

            return prefab;
        }

        /// <summary>
        /// Creates a new Variant prefab for the given source prefab.
        /// </summary>
        /// <param name="source">A Regular prefab to use to create the new Variant. This prefab must have the
        /// SequenceAsset component and it has to be a Regular prefab. No Model nor Variant prefab are supported.</param>
        /// <param name="instantiate">If true, the created Prefab asset will be instantiated in the current active
        /// scene.</param>
        /// <returns>The create Variant prefab asset.</returns>
        /// <exception cref="SequenceAssetException"></exception>
        /// <remarks>If the given source Sequence asset has a timeline setup, a new TimelineAsset will be created and
        /// saved as a sub-asset of the Variant prefab.</remarks>
        public static GameObject CreateVariant(GameObject source, bool instantiate = false)
        {
            if (!IsSource(source))
                throw new SequenceAssetException("Invalid source prefab. It must be a Regular prefab with the " +
                    "SequenceAsset component on it.");

            var sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(source);

            var variantPath = GenerateUniqueVariantAssetPath(source);
            var newVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(sourceInstance, variantPath, InteractionMode.AutomatedAction);

            // We don't need the source prefab instance anymore.
            Object.DestroyImmediate(sourceInstance);

            if (HasTimelineSetup(source))
                DuplicateAndSetupTimeline(source, newVariant); // must be a copy of the original timeline, not a new timeline.

            if (instantiate)
                PrefabUtility.InstantiatePrefab(newVariant);

            EditorGUIUtility.PingObject(newVariant);
            AssetDatabase.SaveAssets();

            return newVariant;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="variant"></param>
        /// <returns></returns>
        public static GameObject DuplicateVariant(GameObject variant)
        {
            var variantPath = AssetDatabase.GetAssetPath(variant);
            var outputPath = AssetDatabase.GenerateUniqueAssetPath(variantPath);
            var success = AssetDatabase.CopyAsset(variantPath, outputPath);

            if (!success)
                return null;

            var duplicatedVariant = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);

            if (HasTimelineSetup(variant))
                DuplicateAndSetupTimeline(variant, duplicatedVariant);

            EditorGUIUtility.PingObject(duplicatedVariant);

            return duplicatedVariant;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="deleteVariants"></param>
        /// <returns></returns>
        public static bool DeleteSourceAsset(GameObject source, bool deleteVariants = true)
        {
            if (!IsSource(source))
                throw new SequenceAssetException("Invalid Sequence Asset Prefab source. It must have a " +
                    "'SequenceAsset' component and be a Regular prefab.");

            var isSuccess = true;
            if (deleteVariants)
            {
                foreach (var variant in GetVariants(source))
                    isSuccess &= DeleteVariantAsset(variant);
            }

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceFolder = Path.GetDirectoryName(sourcePath);

            isSuccess &= DeleteSequenceAsset(source);

            // Also delete the parent folder if it is empty.
            if (SequencesAssetDatabase.IsEmpty(sourceFolder, true))
                isSuccess &= AssetDatabase.DeleteAsset(sourceFolder);

            AssetDatabase.SaveAssets();

            return isSuccess;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="variant"></param>
        /// <returns></returns>
        public static bool DeleteVariantAsset(GameObject variant)
        {
            var success = DeleteSequenceAsset(variant);
            AssetDatabase.SaveAssets();
            return success;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="sequenceDirector"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static GameObject InstantiateInSequence(GameObject prefab, PlayableDirector sequenceDirector, TimelineClip clip = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(sequenceDirector.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate SequenceAsset in Hierarchy");

            var timeline = sequenceDirector.playableAsset as TimelineAsset;
            if (timeline != null && HasTimelineSetup(prefab))
            {
                Undo.RegisterCompleteObjectUndo(timeline, "Add SequenceAsset to Timeline");

                // Make all the fps match.
                var instanceTimeline = GetTimelineAsset(instance);
                instanceTimeline.editorSettings.fps = timeline.editorSettings.fps;

                if (clip == null)
                {
                    var groupTrack = timeline.GetOrCreateTrack<GroupTrack>(GetType(prefab));
                    var track = timeline.CreateTrack<SequenceAssetTrack>(groupTrack, GetSource(prefab).name);
                    clip = track.CreateClip<SequenceAssetPlayableAsset>();
                }

                clip.displayName = instance.name;
                SetClipDuration(clip, timeline, instanceTimeline);

                var clipAsset = clip.asset as SequenceAssetPlayableAsset;
                if (clipAsset == null)
                    return null;

                Undo.RecordObject(clipAsset, "Add SequenceAsset to Timeline");
                Undo.RecordObject(sequenceDirector, "Add SequenceAsset to Timeline");

                var guid = GUID.Generate().ToString();
                clipAsset.director.exposedName = new PropertyName(guid);
                sequenceDirector.SetReferenceValue(clipAsset.director.exposedName, instance.GetComponentInChildren<PlayableDirector>(true));

                EditorUtility.SetDirty(timeline);
                TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
            }

            Undo.SetCurrentGroupName("Instantiate SequenceAsset in Sequence");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            return instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="sequenceDirector"></param>
        public static void RemoveFromSequence(GameObject instance, PlayableDirector sequenceDirector)
        {
            var clip = RemoveFromSequenceInternal(instance, sequenceDirector);
            if (clip == null)
                return;

            var sequenceAssetTrack = clip.GetParentTrack();
            var timeline = sequenceAssetTrack.timelineAsset;

            Undo.RegisterCompleteObjectUndo(timeline, "Remove SequenceAsset from Sequence");

            // Delete clip and tracks related to the removed instance.
            sequenceAssetTrack.DeleteClip(clip);
            var groupTrack = sequenceAssetTrack.parent as GroupTrack;

            if (!sequenceAssetTrack.hasClips)
                timeline.DeleteTrack(sequenceAssetTrack);

            if (groupTrack != null && !groupTrack.GetChildTracks().Any())
                timeline.DeleteTrack(groupTrack);

            EditorUtility.SetDirty(timeline);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);

            Undo.SetCurrentGroupName("Remove SequenceAsset in Sequence");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="oldInstance"></param>
        /// <param name="newPrefab"></param>
        /// <param name="sequenceDirector"></param>
        /// <param name="interactionMode"></param>
        /// <returns></returns>
        public static GameObject UpdateInstanceInSequence(
            GameObject oldInstance,
            GameObject newPrefab,
            PlayableDirector sequenceDirector,
            InteractionMode interactionMode = InteractionMode.UserAction)
        {
            if (!Application.isBatchMode && interactionMode is InteractionMode.UserAction &&
                !UserVerifications.ValidateInstanceChange(oldInstance))
            {
                // Don't proceed with the swap, user choose to keep the current instance.
                return null;
            }

            var clip = RemoveFromSequenceInternal(oldInstance, sequenceDirector);
            var newInstance = InstantiateInSequence(newPrefab, sequenceDirector, clip);

            Undo.SetCurrentGroupName("Update SequenceAsset in Sequence");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            return newInstance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sequenceDirector"></param>
        /// <returns></returns>
        /// <exception cref="SequenceAssetException"></exception>
        public static IEnumerable<GameObject> GetInstancesInSequence(PlayableDirector sequenceDirector, string type = null)
        {
            var timelineInstances = GetInstancesInSequenceTimeline(sequenceDirector, type);
            var gameObjectInstances = GetInstancesUnderSequenceGameObject(sequenceDirector.gameObject, type);
            var allFoundInstances = timelineInstances.Concat(gameObjectInstances);

            // Ensure that one instance is not represented twice in the result.
            var allInstances = new HashSet<GameObject>(allFoundInstances);

            return allInstances;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <param name="renameVariants"></param>
        /// <param name="renameInstances"></param>
        /// <returns></returns>
        public static string Rename(
            GameObject prefab,
            string oldName,
            string newName,
            bool renameVariants = true,
            bool renameInstances = true)
        {
            if (string.IsNullOrEmpty(newName) || oldName == newName)
                return oldName;

            var actualNewName = newName;
            var path = AssetDatabase.GetAssetPath(prefab);
            bool isSource = IsSource(prefab);

            if (isSource)
            {
                var folderPath = Path.GetDirectoryName(path);
                var newFolderPath = AssetDatabase.GenerateUniqueAssetPath(folderPath.Replace(oldName, actualNewName));
                actualNewName = Path.GetFileName(newFolderPath);

                AssetDatabase.RenameAsset(folderPath, actualNewName);
            }

            RenameInternal(prefab, oldName, actualNewName, renameInstances);

            if (isSource && renameVariants)
            {
                foreach (var variant in GetVariants(prefab))
                    RenameInternal(variant, oldName, actualNewName, renameInstances);
            }

            AssetDatabase.SaveAssets();

            return actualNewName;
        }

        /// <summary>
        /// Finds all the SequenceAsset prefab sources that exists in the project.
        /// </summary>
        /// <param name="type">An optional SequenceAsset type to limit the find on Sequence assets that are of this
        /// type.</param>
        /// <returns>Returns an enumerable of all the source Sequence assets found.</returns>
        public static IEnumerable<GameObject> FindAllSources(string type = null)
        {
            var allPrefabsGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in allPrefabsGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                // If not type provided, return all prefabs that are a source SequenceAsset.
                if (IsSource(asset) && string.IsNullOrEmpty(type))
                    yield return asset;

                // If a type is provided, return only prefabs that are source SequenceAsset of the given type.
                else if (IsSource(asset) && GetType(asset) == type)
                    yield return asset;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        /// <exception cref="SequenceAssetException"></exception>
        public static string GetType(GameObject prefab)
        {
            var sequenceAsset = prefab.GetComponent<SequenceAsset>();
            if (sequenceAsset == null)
                throw new SequenceAssetException("The given Prefab is not a Sequence Asset. Sequence " +
                    "Asset Prefabs must have the 'SequenceAsset' component.)");

            return sequenceAsset.type;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab">A Sequence Asset Prefab or Sequence Asset Prefab Variant.</param>
        /// <returns>The Sequence Asset Prefab source of the given Prefab. If the given Prefab is already a source, it is returned as is.</returns>
        /// <exception cref="SequenceAssetException"></exception>
        public static GameObject GetSource(GameObject prefab)
        {
            if (!IsSequenceAsset(prefab))
                throw new SequenceAssetException("The given Prefab is not a Sequence Asset or a Variant of one. " +
                    "It must have a 'SequenceAsset' component.");

            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="SequenceAssetException"></exception>
        public static IEnumerable<GameObject> GetVariants(GameObject source)
        {
            if (!IsSource(source))
                throw new SequenceAssetException("Invalid Sequence Asset source Prefab. It must be a regular " +
                    "Prefab and have a 'SequenceAsset' component.");

            var index = SequenceAssetIndexer.instance.GetIndexOf(source);
            return index < 0 ? new GameObject[] {} : SequenceAssetIndexer.instance.indexes[index].variants;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static bool IsSource(GameObject prefab)
        {
            return prefab.GetComponent<SequenceAsset>() != null &&
                PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Regular;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static bool IsVariant(GameObject prefab)
        {
            return prefab.GetComponent<SequenceAsset>() != null &&
                PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static bool IsSequenceAsset(GameObject prefab)
        {
            return prefab.GetComponent<SequenceAsset>() != null;
        }

        /// <summary>
        /// Checks if a specified Sequence Asset Prefab has Variants.
        /// Only source Sequence Asset can possibly have Variants.
        /// </summary>
        /// <param name="source">The Sequence Asset Prefab source to search existing Variants from.</param>
        /// <returns>True if the specified Sequence Asset Prefab has Variants. Otherwise, false.</returns>
        public static bool HasVariants(GameObject source)
        {
            if (!IsSource(source))
                return false;

            return GetVariants(source).Any();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        internal static GameObject GetAssetFromInstance(GameObject instance)
        {
            if (!IsSequenceAsset(instance))
                throw new SequenceAssetException("The given Prefab instance is not a Sequence Asset or a Variant " +
                    "of one. It must have a 'SequenceAsset' component.");

            if (PrefabUtility.IsPartOfPrefabAsset(instance))
                return instance;

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
            return PrefabUtility.GetCorrespondingObjectFromSourceAtPath(instance, prefabPath);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="sequenceDirector"></param>
        /// <returns></returns>
        internal static TimelineClip GetClipFromInstance(GameObject instance, PlayableDirector sequenceDirector)
        {
            var timeline = sequenceDirector.playableAsset as TimelineAsset;
            if (timeline == null)
                return null;

            foreach (var clip in timeline.GetSequenceAssetClips())
            {
                var bindInstance = GetInstanceFromClip(clip, sequenceDirector);
                if (instance == bindInstance)
                    return clip;
            }

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        static IEnumerable<GameObject> GetInstances(GameObject prefab)
        {
            var sequenceAssetComponents = Resources.FindObjectsOfTypeAll<SequenceAsset>();
            foreach (var sequenceAssetComp in sequenceAssetComponents)
            {
                var instance = sequenceAssetComp.gameObject;
                var sourceAsset = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                if (sourceAsset == prefab)
                    yield return instance;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sequenceDirector"></param>
        /// <returns></returns>
        /// <exception cref="SequenceAssetException"></exception>
        static IEnumerable<GameObject> GetInstancesInSequenceTimeline(PlayableDirector sequenceDirector, string type = null)
        {
            var timeline = sequenceDirector.playableAsset as TimelineAsset;
            if (timeline == null)
                throw new SequenceAssetException("Invalid Sequence's PlayableDirector. This director doesn't control any timeline.");

            foreach (var clip in timeline.GetSequenceAssetClips())
            {
                var instance = GetInstanceFromClip(clip, sequenceDirector);
                if (instance == null || !IsSequenceAsset(instance) || PrefabUtility.IsPrefabAssetMissing(instance))
                    continue;

                if (string.IsNullOrEmpty(type) || type == GetType(instance))
                    yield return instance;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        static IEnumerable<GameObject> GetInstancesUnderSequenceGameObject(GameObject sequence, string type = null)
        {
            for (var i = 0; i < sequence.transform.childCount; ++i)
            {
                var instance = sequence.transform.GetChild(i).gameObject;
                if (!IsSequenceAsset(instance) || PrefabUtility.IsPrefabAssetMissing(instance))
                    continue;

                if (string.IsNullOrEmpty(type) || type == GetType(instance))
                    yield return instance;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        static void SetupTimeline(GameObject prefab)
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = prefab.name + "_Timeline";

            var prefabFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab));
            var timelinePath = Path.Combine(prefabFolderPath, $"{timeline.name}.playable");
            AssetDatabase.CreateAsset(timeline, timelinePath);

            var director = prefab.GetComponentInChildren<PlayableDirector>(true);
            if (director == null)
                director = prefab.AddComponent<PlayableDirector>();

            director.playableAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fromPrefab"></param>
        /// <param name="toPrefab"></param>
        /// <returns></returns>
        static bool DuplicateAndSetupTimeline(GameObject fromPrefab, GameObject toPrefab)
        {
            var fromDirector = fromPrefab.GetComponentInChildren<PlayableDirector>(true);
            if (fromDirector == null)
                return false;

            var fromTimeline = fromDirector.playableAsset as TimelineAsset;
            if (fromTimeline == null)
                return false;

            var fromTimelinePath = AssetDatabase.GetAssetPath(fromTimeline);
            var fromTimelineFolderPath = Path.GetDirectoryName(fromTimelinePath);
            var toTimelinePath = Path.Combine(fromTimelineFolderPath, $"{toPrefab.name}_Timeline.playable");

            var copySucceed = AssetDatabase.CopyAsset(fromTimelinePath, toTimelinePath);
            if (!copySucceed)
                return false;

            var toTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(toTimelinePath);

            var toDirector = toPrefab.GetComponentInChildren<PlayableDirector>(true);
            var oldBindings = toDirector.playableAsset.outputs.ToArray();

            toDirector.playableAsset = toTimeline;
            var newBindings = toDirector.playableAsset.outputs.ToArray();

            for (int i = 0; i < oldBindings.Length; i++)
            {
                toDirector.SetGenericBinding(
                    newBindings[i].sourceObject,
                    toDirector.GetGenericBinding(oldBindings[i].sourceObject));
            }

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        static TimelineAsset GetTimelineAsset(GameObject prefab)
        {
            var director = prefab.GetComponentInChildren<PlayableDirector>(true);
            if (director == null)
                return null;

            var timeline = director.playableAsset as TimelineAsset;
            return timeline;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        static bool HasTimelineSetup(GameObject prefab)
        {
            var playableDirector = prefab.GetComponentInChildren<PlayableDirector>(true);
            return playableDirector != null && (playableDirector.playableAsset as TimelineAsset) != null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="parentTimeline"></param>
        /// <param name="nestedTimeline"></param>
        static void SetClipDuration(TimelineClip clip, TimelineAsset parentTimeline, TimelineAsset nestedTimeline)
        {
            if (nestedTimeline.duration > 0)
            {
                clip.duration = nestedTimeline.duration;
                return;
            }

            var sequence = SequenceUtility.GetSequenceFromTimeline(parentTimeline);
            if (sequence != null)
                clip.duration = sequence.duration;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        static bool DeleteSequenceAsset(GameObject asset)
        {
            var isSuccess = true;

            var variantTimeline = GetTimelineAsset(asset);
            if (variantTimeline != null)
                isSuccess &= AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(variantTimeline));

            isSuccess &= AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));

            return isSuccess;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <param name="renameInstances"></param>
        static void RenameInternal(
            GameObject prefab,
            string oldName,
            string newName,
            bool renameInstances = true)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            var assetName = Path.GetFileNameWithoutExtension(path);
            if (assetName != null && !assetName.Contains(oldName))
            {
                // Assets are renamed by replacing "oldName" by "newName" in them. If the given assets doesn't contains
                // "oldName" then it doesn't need (and won't be able) to be renamed. This case may happen when
                // renaming the variants of a source prefab.
                return;
            }

            var newAssetName = GenerateUniqueAssetName(prefab, oldName, newName);
            AssetDatabase.RenameAsset(path, newAssetName);

            var timeline = GetTimelineAsset(prefab);
            if (timeline != null)
            {
                var newTimelineAssetName = timeline.name.Replace(assetName, newAssetName);
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(timeline), newTimelineAssetName);
            }

            if (renameInstances)
            {
                foreach (var instance in GetInstances(prefab))
                    instance.name = instance.name.Replace(oldName, newName);
            }
        }

        /// <summary>
        /// Get the Sequence Asset prefab instance that holds the PlayableDirector controlled by the given TimelineClip.
        /// </summary>
        /// <param name="clip">A TimelineClip that controls a Sequence Asset prefab PlayableDirector. This TimelineClip asset
        /// must be a SequenceAssetPlayableAsset.</param>
        /// <param name="director">The director that controls the timeline that contains the given clip.</param>
        /// <returns></returns>
        static GameObject GetInstanceFromClip(TimelineClip clip, PlayableDirector director)
        {
            var clipAsset = clip.asset as SequenceAssetPlayableAsset;
            if (clipAsset == null)
                return null;

            var resolvedDirector = (PlayableDirector)director.GetReferenceValue(clipAsset.director.exposedName, out _);
            return resolvedDirector == null ? null : PrefabUtility.GetNearestPrefabInstanceRoot(resolvedDirector.gameObject);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="sequenceDirector"></param>
        /// <returns></returns>
        static TimelineClip RemoveFromSequenceInternal(GameObject instance, PlayableDirector sequenceDirector)
        {
            if (!HasTimelineSetup(instance))
            {
                Undo.DestroyObjectImmediate(instance);
                return null;
            }

            var clip = GetClipFromInstance(instance, sequenceDirector);
            Undo.DestroyObjectImmediate(instance);

            if (clip == null)
                return null;

            Undo.RecordObject(sequenceDirector, "Remove SequenceAsset from Sequence");

            var clipAsset = clip.asset as SequenceAssetPlayableAsset;
            sequenceDirector.ClearReferenceValue(clipAsset.director.exposedName);

            return clip;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        static string GenerateUniqueAssetPath(GameObject prefab)
        {
            var typeFolderPath = Path.Combine(
                "Assets",
                "Sequence Assets");

            if (!AssetDatabase.IsValidFolder(typeFolderPath))
                Directory.CreateDirectory(typeFolderPath);

            var folderPath = Path.Combine(typeFolderPath, prefab.name);
            var uniqueFolderPath = AssetDatabase.GenerateUniqueAssetPath(folderPath);

            Directory.CreateDirectory(uniqueFolderPath);

            var fileName = Path.GetFileName(uniqueFolderPath);
            var uniqueOutputPath = Path.Combine(uniqueFolderPath, $"{fileName}.prefab");

            return uniqueOutputPath;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static string GenerateUniqueVariantAssetPath(GameObject source)
        {
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceFolder = Path.GetDirectoryName(sourcePath);
            var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            var variantName = $"{sourceName} Variant";
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine(sourceFolder, $"{variantName}.prefab"));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        static string GenerateUniqueAssetName(GameObject prefab, string oldName, string newName)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            var folderPath = Path.GetDirectoryName(path);
            var assetName = Path.GetFileNameWithoutExtension(path);

            var newAssetName = assetName.Replace(oldName, newName);
            var newAssetPath = Path.Combine(folderPath, $"{newAssetName}.prefab");
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
            newAssetName = Path.GetFileNameWithoutExtension(newAssetPath);

            return newAssetName;
        }
    }
}
