using UnityEngine;


namespace Marker
{
    public class FoodMarker : MapMarker
    {
        public int amount = 1;

        public bool TakeOne()
        {
            if (amount > 0)
            {
                amount--;
                if (amount == 0)
                    Destroy(gameObject);
                return true;
            }
            return false;
        }

        public override void Tick() {}
    }

}