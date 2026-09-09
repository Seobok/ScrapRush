using ScrapRush.World;
using ScrapRush.Player;
using UnityEngine;

namespace ScrapRush.Debugging
{
    public sealed class CorePlayDebugHud : MonoBehaviour
    {
        private SectorWorld world;
        private Transform player;
        private OreSpawner ores;
        private PlayerAutoMiner miner;
        public void Initialize(SectorWorld sectorWorld, Transform playerTransform, OreSpawner oreSpawner)
        { world = sectorWorld; player = playerTransform; ores = oreSpawner; miner = player.GetComponent<PlayerAutoMiner>(); }

        private void OnGUI()
        {
            if (world == null || player == null) return;
            Vector2Int sector = world.GetSector(player.position);
            GUI.Box(new Rect(12, 12, 360, 154), "CORE PLAY TEST / MINING");
            GUI.Label(new Rect(24, 38, 280, 24), "WASD : Move | Start : B2");
            GUI.Label(new Rect(24, 62, 280, 24), $"Sector {SectorWorld.GetSectorName(sector.x, sector.y)}    Position {player.position.x:F1}, {player.position.y:F1}");
            GUI.Label(new Rect(24, 86, 340, 24), $"Ores {ores.Nodes.Count} | Broken {ores.BrokenCount} | Hits {miner.HitCount}");
            var target = miner.Target;
            GUI.Label(new Rect(24, 110, 340, 24), target != null && target.IsAlive
                ? $"Target {target.Definition.kind} | HP {target.Health}/{target.Definition.maxHealth}"
                : "Target: none (move within 1.4 units)");
            GUI.Label(new Rect(24, 134, 340, 24), $"Target changes {miner.TargetChanges} | Skipped spawns {ores.SkippedCount}");
            const float cell = 38;
            float left = Mathf.Max(12, Screen.width - 3 * cell - 20);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = sector == new Vector2Int(col, row) ? Color.cyan : Color.gray;
                GUI.Box(new Rect(left + col * cell, 16 + row * cell, cell - 2, cell - 2), SectorWorld.GetSectorName(col, row));
                GUI.backgroundColor = previous;
            }
        }
    }
}
