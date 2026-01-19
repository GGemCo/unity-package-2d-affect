using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    public sealed class InMemoryAffectRepository : IAffectDefinitionRepository
    {
        private readonly Dictionary<int, AffectDefinition> _affects = new();
        private readonly Dictionary<int, List<AffectModifierDefinition>> _modifiers = new();

        public void Clear()
        {
            _affects.Clear();
            _modifiers.Clear();
        }

        public void Register(AffectDefinition definition, List<AffectModifierDefinition> modifiers)
        {
            Debug.Log($"affect 등록. Uid: {definition.Uid}");
            _affects[definition.Uid] = definition;
            _modifiers[definition.Uid] = modifiers ?? new List<AffectModifierDefinition>(0);
        }

        public bool TryGetAffect(int affectUid, out AffectDefinition definition)
        {
            Debug.Log($"affect 가져오기. count: {_affects.Count}, uid: {affectUid}");
            return _affects.TryGetValue(affectUid, out definition);
        }

        public IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid)
        {
            if (_modifiers.TryGetValue(affectUid, out var list)) return list;
            return System.Array.Empty<AffectModifierDefinition>();
        }
    }
}
