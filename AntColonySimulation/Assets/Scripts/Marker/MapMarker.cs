using UnityEngine;

namespace Marker
{

    public abstract class MapMarker : MonoBehaviour
    {
        protected SpriteRenderer spriteRenderer;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public virtual void SetColor(Color color)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = color;
        }

        public abstract void Tick();
        protected virtual void Update()
        {
            Tick();
        }
    }

}