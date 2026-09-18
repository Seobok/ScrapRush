using System;
using UnityEngine;

namespace ScrapRush.World
{
    public enum SectorResourceProfile
    {
        Stable,
        Dense,
        HighValue
    }

    [CreateAssetMenu(menuName = "Scrap Rush/Ore Spawn Settings")]
    public sealed class OreSpawnSettings : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public OreDefinition definition;
            [Min(0)] public int weight;
        }

        [Serializable]
        public struct ResourceProfile
        {
            public SectorResourceProfile kind;
            [Min(1)] public int targetOreCount;
            [Min(1)] public int spawnCap;
            [Min(0.1f)] public float refillInterval;
            [Min(1)] public int highValueWeightMultiplier;
        }

        public Entry[] ores;
        public ResourceProfile[] profiles;
        public SectorResourceProfile[] sectorProfiles;
        public int seed = 903;
        [Min(1)] public int minClusters = 2;
        [Min(1)] public int maxClusters = 4;
        [Min(1)] public int minOresPerCluster = 7;
        [Min(1)] public int maxOresPerCluster = 12;
        [Min(0.1f)] public float clusterRadius = 2.5f;
        [Min(0)] public float sectorMargin = 1.2f;
        [Min(0)] public float spacing = 0.12f;
        [Min(1)] public int placementAttempts = 40;
        public bool allowNewRefillAnchors;
    }
}
