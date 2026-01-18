namespace GGemCo2DAffect
{
    public interface IStatMutable
    {
        object ApplyModifier(string statId, float value, StatValueType statValueType, StatOperation operation);
        void RemoveModifier(object token);
        void Recalculate();
        float GetValue(string statId);
    }
}
