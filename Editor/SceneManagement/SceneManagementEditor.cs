using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.Sequences.Timeline;

namespace UnityEditor.Sequences
{
    public class SceneManagement
    {
        public static bool AddNewScene(TimelineSequence sequence)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            if (EditorSceneManager.SaveScene(scene))
            {
                var track = sequence.timeline.CreateTrack<SceneActivationTrack>();
                track.scene = new SceneReference() {path = scene.path};
                track.name = scene.name;
                var activationClip = track.CreateClip<SceneActivationPlayableAsset>();
                activationClip.displayName = "Active";
                activationClip.duration = sequence.duration;

                EditorUtility.SetDirty(sequence.timeline);
                AssetDatabase.SaveAssets();

                TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);

                return true;
            }

            EditorSceneManager.CloseScene(scene, true);
            return false;
        }

        public static void OpenScene(string path, bool deactivate = false)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            if (!scene.isLoaded)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            if (deactivate)
            {
                List<GameObject> rootObjects = new List<GameObject>();
                scene.GetRootGameObjects(rootObjects);

                foreach (GameObject root in rootObjects)
                    root.SetActive(false);
            }
        }

        public static void CloseScene(string path)
        {
            if (IsLoaded(path))
                EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(path), true);
        }

        public static void OpenAllScenes(TimelineSequence sequence, bool deactivate = false)
        {
            foreach (var path in sequence.GetRelatedScenes())
                OpenScene(path, deactivate);
        }

        public static bool IsLoaded(string path)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            return scene.isLoaded;
        }
    }
}
