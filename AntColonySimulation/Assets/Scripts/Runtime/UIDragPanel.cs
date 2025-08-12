using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIDragPanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    [Header("What to move")]
    public RectTransform targetPanel;

    [Header("Handle")]
    public RectTransform handle;

    [Header("Options")]
    public bool clampToCanvas = true;
    public float padding = 6f;
    public bool bringToFrontOnDrag = true;
    public bool addTransparentRaycastImageIfMissing = true;

    [Header("Persistence")]
    public bool savePosition = false;
    public string prefsKey;

    RectTransform canvasRect;
    Canvas canvas;

    Vector3 startPanelWorldPos;
    Vector3 startPointerWorldPos;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas ? canvas.transform as RectTransform : null;

        if (!targetPanel) targetPanel = GetComponent<RectTransform>();
        if (!handle) handle = GetComponent<RectTransform>();

        if (addTransparentRaycastImageIfMissing && handle && !handle.GetComponent<Graphic>())
        {
            var img = handle.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.001f);
            img.raycastTarget = true;
        }

        if (canvas && !canvas.GetComponent<GraphicRaycaster>())
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (savePosition && targetPanel)
        {
            string key = GetPrefsKey();
            if (PlayerPrefs.HasKey(key + "_x") && PlayerPrefs.HasKey(key + "_y"))
            {
                var p = targetPanel.anchoredPosition;
                p.x = PlayerPrefs.GetFloat(key + "_x");
                p.y = PlayerPrefs.GetFloat(key + "_y");
                targetPanel.anchoredPosition = p;
            }
        }
    }

    string GetPrefsKey()
    {
        if (!string.IsNullOrEmpty(prefsKey)) return prefsKey;
        return targetPanel ? targetPanel.gameObject.name : gameObject.name;
    }

    public void OnPointerDown(PointerEventData e)
    {
        CacheStart(e);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        CacheStart(e);
    }

    void CacheStart(PointerEventData e)
    {
        if (!canvasRect || !targetPanel) return;

        startPanelWorldPos = targetPanel.position;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out startPointerWorldPos))
            startPointerWorldPos = Vector3.zero;

        if (bringToFrontOnDrag)
            targetPanel.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!canvasRect || !targetPanel) return;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out var pointerWorld))
            return;

        Vector3 proposedWorldPos = startPanelWorldPos + (pointerWorld - startPointerWorldPos);
        targetPanel.position = proposedWorldPos;

        if (clampToCanvas)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, targetPanel);
            var cmin = canvasRect.rect.min + new Vector2(padding, padding);
            var cmax = canvasRect.rect.max - new Vector2(padding, padding);

            float dx = 0f, dy = 0f;
            if (bounds.min.x < cmin.x) dx = cmin.x - bounds.min.x;
            else if (bounds.max.x > cmax.x) dx = cmax.x - bounds.max.x;

            if (bounds.min.y < cmin.y) dy = cmin.y - bounds.min.y;
            else if (bounds.max.y > cmax.y) dy = cmax.y - bounds.max.y;

            if (dx != 0f || dy != 0f)
            {
                Vector3 worldDelta = canvasRect.TransformVector(new Vector3(dx, dy, 0));
                targetPanel.position += worldDelta;
            }
        }

        if (savePosition)
        {
            string key = GetPrefsKey();
            var ap = targetPanel.anchoredPosition;
            PlayerPrefs.SetFloat(key + "_x", ap.x);
            PlayerPrefs.SetFloat(key + "_y", ap.y);
        }
    }
}
