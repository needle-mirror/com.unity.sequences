using UnityEngine;
using UnityEngine.Sequences;

#if UNITY_2021_2_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#endif

namespace UnityEditor.Sequences
{
    internal static class UserVerifications
    {
        /// <summary>
        /// Skips the Editor popups asking users for confirmation.
        /// Set it to True when methods are called from automation.
        /// </summary>
        internal static bool skipUserVerification = false;

        internal static bool ValidateSequenceDeletion(Sequence sequence)
        {
            if (skipUserVerification)
                return true;

            // First check if the Sequence to delete is part of a Prefab.
            var gameObject = SequenceUtility.GetSequenceGameObject(sequence as TimelineSequence);
            if (gameObject != null &&
                PrefabUtility.IsPartOfPrefabInstance(gameObject) &&
                !PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject) &&
                !PrefabUtility.IsAddedGameObjectOverride(gameObject))
            {
                // Sequence is part of a prefab but is not the root GameObject.
                // User needs to be in isolation to delete the sequence.
                OpenPrefabStage(gameObject);
                return false;
            }

            var deleteAssets = EditorUtility.DisplayDialog(
                "Sequence deletion",
                $"Do you want to delete the \"{sequence.name}\" Sequence and its children?\n\n" +
                "You cannot undo this action.",
                "Delete",
                "Cancel"
            );

            return deleteAssets;
        }

        internal static bool ValidateSequenceAssetDeletion(GameObject deletedSequenceAsset)
        {
            if (skipUserVerification)
                return true;

            var hasVariantMessage = "";
            if (SequenceAssetUtility.HasVariants(deletedSequenceAsset))
                hasVariantMessage = " and its Variants";

            var deleteAssets = EditorUtility.DisplayDialog(
                "Sequence Asset deletion",
                $"Do you want to delete the \"{deletedSequenceAsset.name}\" Sequence Asset{hasVariantMessage}?\n\n" +
                "You cannot undo this action.",
                "Delete",
                "Cancel"
            );

            return deleteAssets;
        }

        internal static bool ValidateInstanceChange(GameObject instance)
        {
            if (!PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false))
                return true;

            // Deactivating an instance counts as an override, so ignore that case
            bool hasNonDefaultOverrides = false;
            if (PrefabUtility.GetAddedComponents(instance).Count > 0 ||
                PrefabUtility.GetAddedGameObjects(instance).Count > 0)
            {
                hasNonDefaultOverrides = true;
            }
            else
            {
                foreach (var modification in PrefabUtility.GetPropertyModifications(instance))
                {
                    if (!PrefabUtility.IsDefaultOverride(modification) &&
                        // m_InitialState is controlled by "Play on Awake" that is changed when the playable director is
                        // targeted by a SequenceAsset clip (i.e. it's a nested timeline).
                        !modification.propertyPath.Equals("m_InitialState"))
                    {
                        hasNonDefaultOverrides = true;
                    }
                }
            }

            if (!hasNonDefaultOverrides)
                return true;

            if (skipUserVerification)
                return true;

            var result = EditorUtility.DisplayDialogComplex(
                "Sequence Asset instance has been modified",
                $"Do you want to save the changes you made on \"{instance.name}\"?\n\n" +
                "Your changes will be lost if you don't save them.",
                "Save Changes",
                "Cancel",
                "Discard Changes"
            );

            switch (result)
            {
                case 0:  // Apply Overrides
                    PrefabUtility.ApplyPrefabInstance(
                        instance,
                        InteractionMode.AutomatedAction);

                    return true;

                case 1:  // Cancel
                    return false;

                case 2:  // Discard Changes
                    return true;

                default:
                    return false;
            }
        }

        internal static void OpenPrefabStage(GameObject gameObject)
        {
            if (skipUserVerification)
                return;

            var openPrefab = EditorUtility.DisplayDialog(
                "Cannot restructure Prefab instance",
                "Children of a Prefab instance cannot be deleted or moved, and components cannot be reordered.\n\n" +
                "You can open the Prefab in Prefab Mode to restructure the Prefab Asset itself, or unpack the Prefab" +
                " instance to remove its Prefab connection.",
                "Open Prefab", "Cancel");

            if (openPrefab)
            {
                var rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(rootPrefab);

#if UNITY_2021_2_OR_NEWER
                var assetPath = AssetDatabase.GetAssetPath(prefabAsset);
                PrefabStageUtility.OpenPrefab(assetPath, rootPrefab);
#else
                AssetDatabase.OpenAsset(prefabAsset, rootPrefab.GetInstanceID());
#endif
            }
        }
    }
}
