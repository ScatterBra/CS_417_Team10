using System;

namespace Deadzone.Power
{
    public enum PWR_ResourceType
    {
        Diesel,
        Coal,
        CrudeOil,
        Biomass,
        CopperWire,
        Fuse,
        EmptyCell,
        ChargedCell,
        SpareBulb,
        Scrip,
    }

    [Serializable]
    public struct PWR_ResourceAmount
    {
        public PWR_ResourceType type;
        public int amount;

        public PWR_ResourceAmount(PWR_ResourceType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }
}
