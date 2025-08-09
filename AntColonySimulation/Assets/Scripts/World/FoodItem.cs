using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public bool taken { get; private set; }
    public bool TryTake()
    {
        if (taken) return false;
        taken = true;
        return true;
    }
}