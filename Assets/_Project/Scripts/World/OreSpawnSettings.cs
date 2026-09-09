using System;
using UnityEngine;

namespace ScrapRush.World
{
    [CreateAssetMenu(menuName = "Scrap Rush/Ore Spawn Settings")]
    public sealed class OreSpawnSettings : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public OreDefinition definition;
            [Min(0)] public int weight;
        }

        public Entry[] ores;
        public int seed = 903;
        [Min(1)] public int minClusters = 2;
        [Min(1)] public int maxClusters = 4;
        [Min(1)] public int minOresPerCluster = 7;
        [Min(1)] public int maxOresPerCluster = 12;
        [Min(0.1f)] public float clusterRadius = 2.5f;
        [Min(0)] public float sectorMargin = 1.2f;
        [Min(0)] public float spacing = 0.12f;
        [Min(1)] public int placementAttempts = 40;
    }
}
