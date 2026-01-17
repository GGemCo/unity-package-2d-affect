namespace GGemCo2DAffect
{
    public interface IStatusDefinitionRepository
    {
        bool IsValidStat(string statId);
        bool IsValidDamageType(string damageTypeId);
        bool IsValidState(string stateId);

        float GetResistancePercent(string damageTypeId, IAffectTarget target);
    }
}
