using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIDragPanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    // ─────────────────────────────────────────────────────────────────────────────
    // KONFIGURACE Z INSPECTORU
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Konfigurace

    [Header("What to move")]
    public RectTransform targetPanel; // Panel, který se má přetahovat

    [Header("Handle")]
    public RectTransform handle;      // Oblast, za kterou se chytá myší

    [Header("Options")]
    public bool clampToCanvas = true; // Udržovat panel uvnitř plochy canvasu
    public float padding = 6f;        // Vnitřní okraje při clampování
    public bool bringToFrontOnDrag = true;               // Při drag posunout na vrch
    public bool addTransparentRaycastImageIfMissing = true; // Přidat neviditelný Graphic pro raycast

    [Header("Persistence")]
    public bool savePosition = false; // Ukládat pozici do PlayerPrefs
    public string prefsKey;           // Volitelný klíč pro PlayerPrefs

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VNITŘNÍ STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Vnitřní stav

    RectTransform canvasRect; // RT canvasu pro převody souřadnic
    Canvas canvas;            // Nadřazený canvas

    Vector3 startPanelWorldPos;   // Počáteční světová pozice panelu při drag
    Vector3 startPointerWorldPos; // Počáteční světová pozice kurzoru při drag

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Unity lifecycle

    // Nastaví reference na canvas, handle a panel, přidá raycast Image a obnoví uloženou pozici.
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

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // EVENT HANDLERY (IPointerDown / IBeginDrag / IDrag)
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Event handlery

    // Uloží výchozí stav při stisku tlačítka nad handlem.
    public void OnPointerDown(PointerEventData e)
    {
        CacheStart(e);
    }

    // Uloží výchozí stav na začátku přetahování.
    public void OnBeginDrag(PointerEventData e)
    {
        CacheStart(e);
    }

    // Přepočítá světovou pozici panelu podle pohybu kurzoru a případně omezí na canvas.
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

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // POMOCNÉ FUNKCE
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Helpers

    // Sestaví klíč pro PlayerPrefs.
    string GetPrefsKey()
    {
        if (!string.IsNullOrEmpty(prefsKey)) return prefsKey;
        return targetPanel ? targetPanel.gameObject.name : gameObject.name;
    }

    // Zapamatuje si výchozí pozici panelu i kurzoru a případně posune panel na vrch hierarchie.
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

    #endregion
}
