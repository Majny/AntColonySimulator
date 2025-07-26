using UnityEngine;
using UnityEngine.InputSystem;

namespace Marker
{
    public class FoodSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject foodMarkerPrefab;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private int defaultAmount = 50;

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);

                GameObject foodObj = Instantiate(foodMarkerPrefab, worldPos, Quaternion.identity);

                FoodMarker marker = foodObj.GetComponent<FoodMarker>();
                if (marker != null)
                {
                    marker.amount = defaultAmount;
                    marker.SetColor(Color.green);
                }
            }
        }
    }
}