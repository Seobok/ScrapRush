using ScrapRush.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrapRush.Player
{
    public sealed class PlayerMotor : MonoBehaviour
    {
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
            transform.position = world.ClampPosition((Vector2)transform.position + MoveInput * (speed * Time.deltaTime), radius);
        }
    }
}
