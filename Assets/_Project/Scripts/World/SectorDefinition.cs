using UnityEngine;

namespace ScrapRush.World
{
    // All visual layout is authored in the prefab.
    public sealed class SectorDefinition : MonoBehaviour
    {
        [SerializeField, Min(8f)] private float size = 18f;
        [SerializeField] private SpriteRenderer idMarker;
        [SerializeField, Tooltip("Row order: A1, A2, A3, B1, B2, B3, C1, C2, C3.")]
        private Sprite[] markerSprites = new Sprite[9];
        public float Size => size;

        public void InitializeMarker(int col, int row)
        {
            int index = row * SectorWorld.GridSize + col;
            if (idMarker == null || markerSprites == null ||
                col < 0 || col >= SectorWorld.GridSize || row < 0 || row >= SectorWorld.GridSize ||
                index >= markerSprites.Length || markerSprites[index] == null)
            {
                Debug.LogError("Assign IDMarker and all nine sector marker sprites on the sector prefab.", this);
                return;
            }

            idMarker.sprite = markerSprites[index];
        }
    }
}
