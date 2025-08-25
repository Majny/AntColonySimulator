using UnityEngine;

public class FoodItem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // STAV
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Stav

    // Označuje, zda byl předmět již sebrán jiným agentem.
    public bool taken { get; private set; }

    #endregion


    // ─────────────────────────────────────────────────────────────────────────────
    // VEŘEJNÉ API
    // ─────────────────────────────────────────────────────────────────────────────
    #region — Veřejné API

    // Pokusí se vzít předmět, vrací true při úspěchu, false pokud už ho někdo vzal.
    public bool TryTake()
    {
        if (taken) return false;
        taken = true;
        return true;
    }

    #endregion
}