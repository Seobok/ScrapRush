using UnityEngine;

namespace ScrapRush.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        private Transform target;
        private Rect bounds;
        private Camera view;
        private Vector3 velocity;
        private float smoothTime;

        public void Initialize(Transform followTarget, Rect worldBounds, float size, float damping)
        {
            target = followTarget;
            bounds = worldBounds;
            smoothTime = damping;
            view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = size;
            transform.rotation = Quaternion.identity;
            transform.position = Constrain(target.position);
        }

        private Vector3 Constrain(Vector3 position)
        {
            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            return new Vector3(
                halfWidth * 2 >= bounds.width ? bounds.center.x : Mathf.Clamp(position.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth),
                halfHeight * 2 >= bounds.height ? bounds.center.y : Mathf.Clamp(position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight), -10f);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Constrain(Vector3.SmoothDamp(transform.position, Constrain(target.position),
                ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime));
        }
    }
}
