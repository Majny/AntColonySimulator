using UnityEngine;


namespace Marker
{
    using UnityEngine;

    public class PheromoneFactory : MonoBehaviour
    {
        [SerializeField] private GameObject markerToFoodPrefab;
        [SerializeField] private GameObject markerToHomePrefab;

        public void SpawnMarker(Vector2 position, MarkerPheromoneType type)
        {
            GameObject prefab = type == MarkerPheromoneType.ToFood ? markerToFoodPrefab : markerToHomePrefab;
            GameObject marker = Instantiate(prefab, position, Quaternion.identity);

            marker.transform.localScale = Vector3.one;

            var match = marker.GetComponent<MatchColliderToScale>();
            if (match != null)
                match.SendMessage("UpdateColliderRadius");
        }


    }

}