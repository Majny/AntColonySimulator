using UnityEngine;

public class SceneBootstrapper : MonoBehaviour
{
    public GameObject colonyPrefab;
    public Vector2 nestWorldPosition = Vector2.zero;

    private void Start()
    {
        if (colonyPrefab != null)
        {
            Instantiate(colonyPrefab, nestWorldPosition, Quaternion.identity);
        }
    }
}