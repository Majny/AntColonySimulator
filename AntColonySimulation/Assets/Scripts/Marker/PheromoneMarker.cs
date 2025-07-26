using UnityEngine;

namespace Marker
{
    public class PheromoneMarker : MonoBehaviour
    {
        public MarkerPheromoneType type; // ← tenhle typ je důležitý

        private float timeAlive;
        public float lifetime = 5f;

        public float TimeAliveNormalized => timeAlive / lifetime;

        void Update()
        {
            timeAlive += Time.deltaTime;

            if (timeAlive > lifetime)
                Destroy(gameObject);
        }
    }
}