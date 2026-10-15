using System;

namespace Deadzone.Power
{
    public enum PWR_ResourceType
    {
        Scrap,
        CrudeOil,
        RefinedOil,
        Coal,
        CopperWire,
        Glass,
        Fuse,
        Electrolyte,
        Battery,   // crafted; the physical battery also appears on the workbench
        LightBulb, // crafted; the physical bulb also appears on the workbench
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

        public override string ToString() => $"{amount} {PWR_Names.Of(type)}";
    }

    public static class PWR_Names
    {
        public static string Of(PWR_ResourceType type)
        {
            switch (type)
            {
                case PWR_ResourceType.CrudeOil: return "Crude Oil";
                case PWR_ResourceType.RefinedOil: return "Refined Oil";
                case PWR_ResourceType.CopperWire: return "Copper Wire";
                case PWR_ResourceType.LightBulb: return "Light Bulb";
                default: return type.ToString();
            }
        }
    }
}
