using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.Sequences.Timeline;
using UnityEngine.Playables;

namespace UnityEditor.Sequences
{
    /// <summary>
    /// Class makes sure the Hierarchy view reflects data changes.
    /// </summary>
    [InitializeOnLoad]
    class HierarchyDataChangeVerifier
    {
        static HierarchyDataChangeVerifier()
        {
            SequenceUtility.sequenceCreated += OnSequenceCreated;
            SequenceUtility.sequenceDeleted += OnSequenceDeleted;
            Sequence.sequenceRenamed += OnSequenceRenamed;
            SelectionUtility.playableDirectorChanged += OnPlayableDirectorChanged;
        }

        static void OnSequenceCreated(TimelineSequence sequence, MasterSequence masterSequence)
        {
            GameObject newObject = null;
            if (masterSequence.rootSequence == sequence)
                newObject = CreateMasterSequenceGameObject(sequence, masterSequence);
            else
                newObject = CreateSequenceGameObject(sequence, masterSequence);

            Selection.activeGameObject = newObject;
        }

        static void OnSequenceDeleted()
        {
            var allFilters = ObjectsCache.FindObjectsFromScenes<SequenceFilter>();

            foreach (var filter in allFilters)
            {
                // If GameObject is already in the process of being destroyed, do nothing.
                if (filter.isBeingDestroyed)
                    continue;

                // A MasterSequence's SequenceFilter might get destroyed in this loop
                // first, before their children's SequenceFilter. If that's the case, filter
                // is null.
                if (filter == null)
                    continue;

                // If the filter is not bound to a masterSequence, do nothing.
                if (filter.masterSequence == null)
                    continue;

                var sequence = filter.masterSequence.manager.GetAt(filter.elementIndex);
                if (sequence == null)
                {
                    var scene = filter.gameObject.scene;

                    if (PrefabUtility.IsAnyPrefabInstanceRoot(filter.gameObject) && PrefabUtility.IsOutermostPrefabInstanceRoot(filter.gameObject))
                    {
                        // Sequence is the root of a prefab instance.
                        PrefabUtility.UnpackPrefabInstance(filter.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        GameObject.DestroyImmediate(filter.gameObject);
                    }
                    else if (PrefabUtility.IsPartOfAnyPrefab(filter.gameObject))
                    {
                        string rootAssetPath = string.Empty;
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(filter.gameObject))
                        {
                            // Sequence is a prefab and nested in another Prefab.
                            var outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(filter.gameObject);
                            rootAssetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource<GameObject>(outermost));
                        }
                        else
                        {
                            // Sequence is a regular GameObject inside another prefab.
                            rootAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(filter.gameObject);
                        }

                        // Open the right prefab then delete the Sequence.
                        using (var editScope = new PrefabUtility.EditPrefabContentsScope(rootAssetPath))
                        {
                            var children = editScope.prefabContentsRoot.GetComponentsInChildren<SequenceFilter>();
                            foreach (var target in children)
                            {
                                if (filter.Equals(target))
                                {
                                    Object.DestroyImmediate(target.gameObject);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Sequence is a regular GameObject (not in a Prefab instance).
                        GameObject.DestroyImmediate(filter.gameObject);
                    }

                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        static void OnSequenceRenamed(Sequence sequence)
        {
            var allFilters = ObjectsCache.FindObjectsFromScenes<SequenceFilter>();
            foreach (var filter in allFilters)
            {
                if (filter.masterSequence == null)
                    continue;

                var currentSequence = filter.masterSequence.manager.GetAt(filter.elementIndex);
                if (currentSequence != null && currentSequence == sequence)
                {
                    Undo.RecordObject(filter.gameObject, "Rename Sequence");
                    filter.gameObject.name = sequence.name;
                }
            }
        }

        static void OnPlayableDirectorChanged()
        {
            if (SelectionUtility.activePlayableDirector == null)
                return;

            if (Selection.activeGameObject == SelectionUtility.activePlayableDirector.gameObject)
                return;

            Selection.activeGameObject = SelectionUtility.activePlayableDirector.gameObject;
        }

        /// <summary>
        /// Create a MasterSequence GameObject in the Hierarchy view.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="masterSequence"></param>
        /// <returns></returns>
        static GameObject CreateMasterSequenceGameObject(TimelineSequence sequence, MasterSequence masterSequence)
        {
            var go = new GameObject(sequence.name);
            Undo.RegisterCreatedObjectUndo(go, Undo.GetCurrentGroupName());

            var playableDirector = Undo.AddComponent<PlayableDirector>(go);
            masterSequence.rootSequence.childrenTrackName = "Sequences";
            playableDirector.playableAsset = masterSequence.rootSequence.timeline;

            var sequenceFilter = Undo.AddComponent<SequenceFilter>(go);
            sequenceFilter.masterSequence = masterSequence;
            sequenceFilter.type = SequenceFilter.Type.MasterSequence;
            sequenceFilter.elementIndex = masterSequence.rootIndex;

            Selection.activeGameObject = go;

            return go;
        }

        /// <summary>
        /// Creates a Sequence GameObject in a MasterSequence GameObjects hierarchy.
        /// </summary>
        /// <returns></returns>
        static GameObject CreateSequenceGameObject(TimelineSequence sequence, MasterSequence masterSequence)
        {
            int parentIndex = masterSequence.manager.GetIndex(sequence.parent);
            var allFilters = ObjectsCache.FindObjectsFromScenes<SequenceFilter>();
            foreach (var filter in allFilters)
            {
                // Find parent to attach the new GameObject.
                if (filter.masterSequence != masterSequence)
                    continue;
                if (filter.elementIndex != parentIndex)
                    continue;

                Transform parent = filter.gameObject.transform;

                GameObject newGo = new GameObject(sequence.name);
                Undo.RegisterCreatedObjectUndo(newGo, Undo.GetCurrentGroupName());
                Undo.SetTransformParent(newGo.transform, parent, Undo.GetCurrentGroupName());

                var playableDirector = Undo.AddComponent<PlayableDirector>(newGo);
                playableDirector.playableAsset = sequence.timeline;

                var clipAsset = sequence.editorialClip.asset as EditorialPlayableAsset;
                var guid = GUID.Generate().ToString();
                clipAsset.director.exposedName = new PropertyName(guid);
                EditorUtility.SetDirty(clipAsset);

                parent.gameObject.GetComponent<PlayableDirector>().SetReferenceValue(clipAsset.director.exposedName, playableDirector);

                var sequenceFilter = Undo.AddComponent<SequenceFilter>(newGo);
                sequenceFilter.masterSequence = masterSequence;
                sequenceFilter.type = (sequence.parent == masterSequence.rootSequence) ? SequenceFilter.Type.Sequence : SequenceFilter.Type.Shot;
                sequenceFilter.elementIndex = masterSequence.manager.GetIndex(sequence);

                Selection.activeGameObject = newGo;

                return newGo;
            }
            return null;
        }
    }
}
