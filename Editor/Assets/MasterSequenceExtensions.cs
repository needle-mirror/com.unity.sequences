using System.IO;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    public static class MasterSequenceExtensions
    {
        public static void Save(this MasterSequence masterSequence, string folder = null)
        {
            if (AssetDatabase.Contains(masterSequence))
            {
                SequencesAssetDatabase.SaveAsset(masterSequence);
                return;
            }

            folder = folder ?? masterSequence.name;

            masterSequence.rootSequence.Save(folder);

            var path = SequencesAssetDatabase.GenerateUniqueMasterSequencePath(masterSequence.name, folder);
            SequencesAssetDatabase.SaveAsset(masterSequence, path);
        }

        public static bool Rename(this MasterSequence masterSequence, string newName)
        {
            if (!SequencesAssetDatabase.IsRenameValid(masterSequence.name, newName)) return false;

            newName = SequencesAssetDatabase.GenerateUniqueAssetName(masterSequence, newName);
            SequencesAssetDatabase.RenameAssetFolder(masterSequence, newName);

            masterSequence.rootSequence.Rename(newName);
            SequencesAssetDatabase.RenameAsset(masterSequence, newName);
            return true;
        }

        public static void Delete(this MasterSequence masterSequence)
        {
            SequenceUtility.DeleteSequence(masterSequence.rootSequence, masterSequence);

            var directoryName = Path.GetDirectoryName(SequencesAssetDatabase.GetAssetPath(masterSequence));

            SequencesAssetDatabase.DeleteAsset(masterSequence);
            SequencesAssetDatabase.DeleteFolder(directoryName);
        }
    }
}
