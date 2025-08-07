/*using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FoodPile : MonoBehaviour
{
    [Header("Pile")]
    public int units = 50;
    public GameObject unitPrefab;
    public float scatterRadius = 0.2f;
    public bool autoDestroyWhenEmpty = true;

    [Header("Visuals (optional)")]
    public Transform visual;
    public Vector2 scaleRange = new Vector2(0.3f, 1f);

    int initialUnits;

    public bool IsEmpty => units <= 0;

    void Awake()
    {
        initialUnits = Mathf.Max(units, 1);
        UpdateVisuals();
    }

    public void SetInitialUnits(int count)
    {
        units = count;
        initialUnits = Mathf.Max(count, 1);
        UpdateVisuals();
    }

    public Transform TakeOne(Transform parentForCarry = null)
    {
        if (units <= 0 || unitPrefab == null) return null;

        units--;

        Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * scatterRadius * 0.2f;
        var unit = Instantiate(unitPrefab, pos, Quaternion.identity).transform;

        if (parentForCarry != null)
        {
            unit.SetParent(parentForCarry, worldPositionStays: true);
            unit.position = parentForCarry.position;
        }


        UpdateVisuals();

        if (units <= 0 && autoDestroyWhenEmpty)
            Destroy(gameObject);

        return unit;
    }

    void UpdateVisuals()
    {
        if (visual != null)
        {
            float t = Mathf.Clamp01((float)units / initialUnits);
            float s = Mathf.Lerp(scaleRange.x, scaleRange.y, t);
            visual.localScale = Vector3.one * s;
        }
    }
}*/