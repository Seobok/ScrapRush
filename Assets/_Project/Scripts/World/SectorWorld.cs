using UnityEngine;

namespace ScrapRush.World
{
    public sealed class SectorWorld : MonoBehaviour
    {
        public const int GridSize = 3;
        public Rect Bounds { get; private set; }
        public float SectorSize { get; private set; }

        public void Initialize(SectorDefinition sectorPrefab)
        {
            float sectorSize = sectorPrefab.Size;
            SectorSize = sectorSize;
            float size = sectorSize * GridSize;
            Bounds = new Rect(-size / 2f, -size / 2f, size, size);
            for (int row = 0; row < GridSize; row++)
            for (int col = 0; col < GridSize; col++)
            {
                Vector2 center = new Vector2(Bounds.xMin + (col + 0.5f) * sectorSize,
                    Bounds.yMax - (row + 0.5f) * sectorSize);
                var sector = Instantiate(sectorPrefab, transform);
                sector.name = GetSectorName(col, row);
                sector.transform.position = center;
                sector.InitializeMarker(col, row);
            }
        }

        public Vector2 ClampPosition(Vector2 position, float margin)
        {
            return new Vector2(Mathf.Clamp(position.x, Bounds.xMin + margin, Bounds.xMax - margin),
                Mathf.Clamp(position.y, Bounds.yMin + margin, Bounds.yMax - margin));
        }

        public Vector2Int GetSector(Vector2 position)
        {
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((position.x - Bounds.xMin) / SectorSize), 0, 2),
                Mathf.Clamp(Mathf.FloorToInt((Bounds.yMax - position.y) / SectorSize), 0, 2));
        }

        public static string GetSectorName(int col, int row) => $"{(char)('A' + row)}{col + 1}";
    }
}
