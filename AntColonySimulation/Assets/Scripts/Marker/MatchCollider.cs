using UnityEngine;

namespace Marker
{

    [RequireComponent(typeof(CircleCollider2D))]
    public class MatchColliderToScale : MonoBehaviour
    {
        private CircleCollider2D _collider;

        void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            UpdateColliderRadius();
        }

        void UpdateColliderRadius()
        {
            float spriteRadius = transform.localScale.x * 0.5f;
            _collider.radius = spriteRadius;
        }

        void Update()
        { 
            if (Application.isPlaying && transform.hasChanged)
            {
                UpdateColliderRadius();
                transform.hasChanged = false;
            }
        }
    }

}