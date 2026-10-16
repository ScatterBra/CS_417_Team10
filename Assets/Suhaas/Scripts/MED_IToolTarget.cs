// Anything a MED_HarvestTool can hit (harvestables, the herb planter, ...).
public interface MED_IToolTarget
{
    bool TryHarvest(MED_HarvestTool tool, MED_PlayerResources player);
}
