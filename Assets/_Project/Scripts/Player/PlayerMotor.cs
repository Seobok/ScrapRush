using ScrapRush.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrapRush.Player
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Directional Sprites")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private Sprite backSprite;
        [SerializeField] private Sprite leftSprite;
        [SerializeField] private Sprite rightSprite;
        [SerializeField] private Sprite leftFrontSprite;
        [SerializeField] private Sprite rightFrontSprite;
        [SerializeField] private Sprite leftBackSprite;
        [SerializeField] private Sprite rightBackSprite;
        private InputAction move;
        private SectorWorld world;
        private float speed;
        private float radius;
        public Vector2 MoveInput { get; private set; }

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        }

        public void Initialize(SectorWorld sectorWorld, float moveSpeed, float bodyRadius)
        {
            world = sectorWorld;
            speed = moveSpeed;
            radius = bodyRadius;
        }

        private void OnEnable() => move.Enable();
        private void OnDisable() { move.Disable(); MoveInput = Vector2.zero; }
        private void OnDestroy() => move.Dispose();

        private void Update()
        {
            if (world == null) return;
            MoveInput = Application.isFocused ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            UpdateFacing(MoveInput);
            transform.position = world.ClampPosition((Vector2)transform.position + MoveInput * (speed * Time.deltaTime), radius);
        }

        internal void UpdateFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0f || visual == null) return;

            if (!Mathf.Approximately(direction.x, 0f) && !Mathf.Approximately(direction.y, 0f))
            {
                if (direction.y < 0f)
                    visual.sprite = direction.x < 0f ? leftFrontSprite : rightFrontSprite;
                else
                    visual.sprite = direction.x < 0f ? leftBackSprite : rightBackSprite;
                return;
            }

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                visual.sprite = direction.x < 0f ? leftSprite : rightSprite;
            else
                visual.sprite = direction.y < 0f ? frontSprite : backSprite;
        }
    }
}
