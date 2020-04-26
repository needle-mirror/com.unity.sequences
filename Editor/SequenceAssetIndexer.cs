using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.Sequences
{
    /// <summary>
    ///
    /// </summary>
    [FilePath("Library/QuickSequenceAsset.index", FilePathAttribute.Location.ProjectFolder)]
    class SequenceAssetIndexer : ScriptableSingleton<SequenceAssetIndexer>
    {
        [Serializable]
        internal class Index
        {
            public GameObject mainPrefab;
            public GameObject[] variants = new GameObject[0];
        }

        internal static event Action indexerChanged;

        // TODO: Use ISerializationCallbackReceiver to build a data structure easier and faster to access.
        [SerializeField] Index[] m_Indexes = new Index[0];

        internal Index[] indexes => m_Indexes;

        public void AddSequenceAsset(GameObject prefab)
        {
            if (SequenceAssetUtility.IsSource(prefab))
            {
                AddSequenceAssetSource(prefab);
            }
            else if (SequenceAssetUtility.IsVariant(prefab))
            {
                GameObject source = SequenceAssetUtility.GetSource(prefab);
                if (source == null)
                    return;

                AddSequenceAssetVariant(source, prefab);
            }

            IndexerChanged();
        }

        public void PruneDeletedSequenceAsset()
        {
            var isIndexerChanged = false;
            foreach (Index item in m_Indexes)
            {
                if (item.mainPrefab == null)
                {
                    ArrayUtility.Remove(ref m_Indexes, item);
                    isIndexerChanged = true;
                    continue;
                }

                for (int i = item.variants.Length - 1; i >= 0; --i)
                {
                    if (item.variants[i] == null)
                    {
                        ArrayUtility.RemoveAt(ref item.variants, i);
                        isIndexerChanged = true;
                    }
                }
            }

            if (isIndexerChanged)
                IndexerChanged();
        }

        public void IndexerChanged()
        {
            instance.Save(true);
            indexerChanged?.Invoke();
        }

        public int GetIndexOf(GameObject prefab)
        {
            return ArrayUtility.FindIndex(m_Indexes, (i) => i.mainPrefab == prefab);
        }

        int AddSequenceAssetSource(GameObject prefab)
        {
            int i = GetIndexOf(prefab);
            if (i > 0)
                return i;

            ArrayUtility.Add(ref m_Indexes, new Index() { mainPrefab = prefab });
            return m_Indexes.Length - 1;
        }

        void AddSequenceAssetVariant(GameObject source, GameObject variant)
        {
            int i = GetIndexOf(source);
            if (i < 0)
                i = AddSequenceAssetSource(source);

            Index data = m_Indexes[i];
            if (ArrayUtility.Contains(data.variants, variant))
                return;

            ArrayUtility.Add(ref data.variants, variant);
        }
    }

    class SequenceAssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                string ext = Path.GetExtension(path);
                if (ext != ".prefab")
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!SequenceAssetUtility.IsSequenceAsset(prefab))
                    continue;

                // Moved assets.
                if (ArrayUtility.Contains(movedAssets, path))
                {
                    SequenceAssetIndexer.instance.IndexerChanged();
                    continue;
                }

                // New Sequence asset source or variant.
                SequenceAssetIndexer.instance.AddSequenceAsset(prefab);
            }

            // If there is at least one deleted prefab, check the indexed prefab to remove all the deleted ones.
            if (Array.Exists(deletedAssets, a => Path.GetExtension(a) == ".prefab"))
                SequenceAssetIndexer.instance.PruneDeletedSequenceAsset();
        }
    }
}
