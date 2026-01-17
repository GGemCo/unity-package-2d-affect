using System.Collections.Generic;

namespace GGemCo2DAffect
{
    public interface IAffectDefinitionRepository
    {
        bool TryGetAffect(int affectUid, out AffectDefinition definition);
        IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid);
    }
}
