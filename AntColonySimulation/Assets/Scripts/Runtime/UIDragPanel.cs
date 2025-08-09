using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIDragPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    RectTransform panel;
    RectTransform canvasRect;
    Vector2 grabOffset;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        var canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas ? canvas.transform as RectTransform : null;
    }

    public void OnPointerDown(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panel, e.position, e.pressEventCamera, out grabOffset);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        OnPointerDown(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!canvasRect) return;

        Vector2 canvasLocal;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out canvasLocal))
        {
            Vector2 target = canvasLocal - grabOffset;

            var halfSize = panel.rect.size * panel.pivot;
            var min = canvasRect.rect.min + halfSize;
            var max = canvasRect.rect.max - (panel.rect.size - halfSize);
            target.x = Mathf.Clamp(target.x, min.x, max.x);
            target.y = Mathf.Clamp(target.y, min.y, max.y);

            panel.anchoredPosition = target;
        }
    }
}