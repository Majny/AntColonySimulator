/*using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PheromoneProbe : MonoBehaviour
{
    public PheromoneField field;
    public AgentParameters agentParams;
    public Camera cam;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (field == null || agentParams == null || cam == null) return;

        Vector2 worldPos;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        Vector2 screen = Mouse.current.position.ReadValue();
        worldPos = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
#else
        Vector3 screen = Input.mousePosition;
        worldPos = cam.ScreenToWorldPoint(screen);
#endif

        float s = field.SampleStrength(worldPos, agentParams.pheromoneSensorSize, false);
        if (Time.frameCount % 10 == 0)
            Debug.Log($"Probe strength @ {worldPos}: {s:0.000}");
    }
}
*/