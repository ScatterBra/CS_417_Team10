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

        /// <summary>How the player gets this resource (the "how to fix it" half of error messages).</summary>
        public static string HowToGet(PWR_ResourceType type)
        {
            switch (type)
            {
                case PWR_ResourceType.Scrap: return "Pick up scrap metal from the floor.";
                case PWR_ResourceType.CrudeOil: return "Draw it from an Oil Barrel.";
                case PWR_ResourceType.RefinedOil: return "Refine Crude Oil at the Workbench.";
                case PWR_ResourceType.Coal: return "Break a Wooden Crate with the Wrench.";
                case PWR_ResourceType.CopperWire:
                case PWR_ResourceType.Glass: return "It arrives by the Delivery Chute; collect it from the Receiving Bin.";
                case PWR_ResourceType.Fuse: return "Make one at the Workbench from 2 Copper Wire.";
                case PWR_ResourceType.Electrolyte: return "Collect it from the Electrolyte Tank.";
                default: return "Craft one at the Workbench.";
            }
        }
    }
}
