using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIDragPanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    [Header("What to move")]
    public RectTransform targetPanel;      // Panel, který se má přetahovat

    [Header("Handle")]
    public RectTransform handle;           // Oblast, za kterou se chytá myší

    [Header("Options")]
    public bool clampToCanvas = true;      // Udržovat panel uvnitř plochy canvasu
    public float padding = 6f;             // Vnitřní okraje při clampování
    public bool bringToFrontOnDrag = true; // Při drag posunout na vrch

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    RectTransform canvasRect;     // RT canvasu pro převody souřadnic
    Canvas canvas;                // Nadřazený canvas

    Vector3 startPanelWorldPos;   // Počáteční světová pozice panelu při drag
    Vector3 startPointerWorldPos; // Počáteční světová pozice kurzoru při drag

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas ? canvas.transform as RectTransform : null;

        if (!targetPanel) targetPanel = GetComponent<RectTransform>();
        if (!handle) handle = GetComponent<RectTransform>();

        if (canvas && !canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>())
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // EVENT HANDLERY
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Event handlery

    public void OnPointerDown(PointerEventData e) => CacheStart(e);

    public void OnBeginDrag(PointerEventData e) => CacheStart(e);

    public void OnDrag(PointerEventData e)
    {
        if (!canvasRect || !targetPanel) return;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out var pointerWorld))
            return;

        Vector3 proposedWorldPos = startPanelWorldPos + (pointerWorld - startPointerWorldPos);
        targetPanel.position = proposedWorldPos;

        if (clampToCanvas)
            ClampToCanvas();
    }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Helpers

    // Uloží počáteční stav při kliknutí nebo začátku dragování
    void CacheStart(PointerEventData e)
    {
        if (!canvasRect || !targetPanel) return;

        // Zapamatuj si pozici panelu ve světových souřadnicích
        startPanelWorldPos = targetPanel.position;

        // Zkus převést pozici kurzoru z obrazovkových na světové souřadnice
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out startPointerWorldPos))
            startPointerWorldPos = Vector3.zero; // Pokud se nepovede, nastavíme nulu

        // Pokud je povoleno, posuň panel na vrch
        if (bringToFrontOnDrag) 
            targetPanel.SetAsLastSibling();
    }


    // Udrží panel uvnitř canvasu s respektem na padding
    void ClampToCanvas()
    {
        // Bounding box panelu relativně k canvasu
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, targetPanel);

        // Povolené minimum/maximum (okraje canvasu +- padding)
        var cmin = canvasRect.rect.min + new Vector2(padding, padding);
        var cmax = canvasRect.rect.max - new Vector2(padding, padding);

        float dx = 0f, dy = 0f;

        // OSA X
        // Přesah vlevo, posunout doprava
        if (bounds.min.x < cmin.x) 
            dx = cmin.x - bounds.min.x;
        // Přesah vpravo, posunout doleva
        else if (bounds.max.x > cmax.x) 
            dx = cmax.x - bounds.max.x;

        // OSA Y
        // Přesah dolů, posunout nahoru 
        if (bounds.min.y < cmin.y) 
            dy = cmin.y - bounds.min.y;
        // Přesah nahoru, posunout dolů
        else if (bounds.max.y > cmax.y) 
            dy = cmax.y - bounds.max.y;

        // Posun
        if (dx != 0f || dy != 0f)
        {
            Vector3 worldDelta = canvasRect.TransformVector(new Vector3(dx, dy, 0f));
            targetPanel.position += worldDelta;
        }
    }


    #endregion
}
