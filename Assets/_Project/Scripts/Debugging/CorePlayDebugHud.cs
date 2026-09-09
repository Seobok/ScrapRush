using ScrapRush.World;
using UnityEngine;

namespace ScrapRush.Debugging
{
    public sealed class CorePlayDebugHud : MonoBehaviour
    {
        private SectorWorld world;
        private Transform player;
        public void Initialize(SectorWorld sectorWorld, Transform playerTransform)
        { world = sectorWorld; player = playerTransform; }

        private void OnGUI()
        {
            if (world == null || player == null) return;
            Vector2Int sector = world.GetSector(player.position);
            GUI.Box(new Rect(12, 12, 300, 82), "CORE PLAY TEST / P0-A");
            GUI.Label(new Rect(24, 38, 280, 24), "WASD : Move | Start : B2");
            GUI.Label(new Rect(24, 62, 280, 24), $"Sector {SectorWorld.GetSectorName(sector.x, sector.y)}    Position {player.position.x:F1}, {player.position.y:F1}");
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
