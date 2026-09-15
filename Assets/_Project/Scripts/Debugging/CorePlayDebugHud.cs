using ScrapRush.World;
using ScrapRush.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrapRush.Debugging
{
    public sealed class CorePlayDebugHud : MonoBehaviour
    {
        private SectorWorld world;
        private Transform player;
        private OreSpawner ores;
        private PlayerAutoMiner miner;
        private ScrapSystem scraps;
        private ElectricSystem electric;
        private float absorbFlash;
        private int recentPickup;
        private Camera viewCamera;
        public void Initialize(SectorWorld sectorWorld, Transform playerTransform, OreSpawner oreSpawner,
            ScrapSystem scrapSystem, ElectricSystem electricSystem)
        { scraps = scrapSystem; electric = electricSystem; world = sectorWorld; player = playerTransform; ores = oreSpawner; miner = player.GetComponent<PlayerAutoMiner>(); viewCamera = Camera.main; scraps.Absorbed += OnAbsorbed; }

        private void OnAbsorbed(OreBreakInfo info) { recentPickup = absorbFlash > 0 ? recentPickup + info.FinalValue : info.FinalValue; absorbFlash = 0.45f; }
        private void Update()
        {
            if (!Application.isFocused) return;
            absorbFlash = Mathf.Max(0, absorbFlash - Time.deltaTime);
            if (electric == null || Keyboard.current == null) return;
            if (Keyboard.current.digit0Key.wasPressedThisFrame) electric.SetTraitCount(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) electric.SetTraitCount(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) electric.SetTraitCount(4);
            else if (Keyboard.current.digit6Key.wasPressedThisFrame) electric.SetTraitCount(6);
        }
        private void OnDestroy() { if (scraps != null) scraps.Absorbed -= OnAbsorbed; }

        private void OnGUI()
        {
            if (world == null || player == null) return;
            if (absorbFlash > 0 && viewCamera != null)
            {
                Vector3 screen = viewCamera.WorldToScreenPoint(player.position);
                Color previous = GUI.color;
                GUI.color = new Color(0.5f, 1f, 1f, absorbFlash / 0.45f);
                GUI.Label(new Rect(screen.x + 16, Screen.height - screen.y - 35 - (0.45f - absorbFlash) * 30, 180, 24), $"+{recentPickup} C");
                GUI.color = previous;
            }
            Vector2Int sector = world.GetSector(player.position);
            GUI.Box(new Rect(12, 12, 480, 298), "CORE PLAY TEST / SCRAP / ELECTRIC");
            GUI.Label(new Rect(24, 38, 440, 24), "WASD : Move | Electric test count : 0 / 2 / 4 / 6");
            GUI.Label(new Rect(24, 62, 280, 24), $"Sector {SectorWorld.GetSectorName(sector.x, sector.y)}    Position {player.position.x:F1}, {player.position.y:F1}");
            GUI.Label(new Rect(24, 86, 340, 24), $"Ores {ores.Nodes.Count} | Broken {ores.BrokenCount} | Hits {miner.HitCount}");
            var target = miner.Target;
            GUI.Label(new Rect(24, 110, 340, 24), target != null && target.IsAlive
                ? $"Target {target.Definition.kind} | HP {target.Health}/{target.Definition.maxHealth}"
                : "Target: none (move within 1.4 units)");
            GUI.Label(new Rect(24, 134, 340, 24), $"Target changes {miner.TargetChanges} | Skipped spawns {ores.SkippedCount}");
            GUI.Label(new Rect(24, 158, 400, 24), $"C {scraps.Credits} | Last 30s {scraps.RecentCredits} C | {scraps.CreditsPerSecond:F1} C/s");
            GUI.Label(new Rect(24, 182, 400, 24), $"Scrap generated {scraps.GeneratedCount} | Absorbed {scraps.AbsorbedCount}");
            GUI.Label(new Rect(24, 206, 400, 24), $"Remaining {scraps.Drops.Count} | Peak {scraps.PeakActiveCount} | Cleared {scraps.ClearedCount}");
            if (electric != null)
            {
                string charge = electric.IsReady ? "READY" : $"{electric.CooldownRemaining:F1}s";
                GUI.Label(new Rect(24, 230, 450, 24), $"Electric {electric.TraitCount} / Tier {electric.ActiveTier} | Charge {charge} | Storm shots {electric.StormShotsRemaining}");
                GUI.Label(new Rect(24, 254, 450, 24), $"Static {electric.StaticTriggerCount} | Storm {electric.StormCount} | Discharges {electric.DischargeCount}");
                GUI.Label(new Rect(24, 278, 450, 24), $"Electric hits {electric.HitCount} | Chain hits {electric.ChainHitCount} | Broken {electric.BrokenCount}");
            }
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
