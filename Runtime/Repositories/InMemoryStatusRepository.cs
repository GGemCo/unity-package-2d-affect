using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Stat/DamageType/State 3분리 정의 저장소의 간단 구현.
    /// - 저항은 기본적으로 Stat 규칙(RESIST_{DamageTypeId})로 조회한다.
    /// </summary>
    public sealed class InMemoryStatusRepository : IStatusDefinitionRepository
    {
        public string ResistancePrefix = "RESIST_";

        private readonly HashSet<string> _stats = new();
        private readonly HashSet<string> _damageTypes = new();
        private readonly HashSet<string> _states = new();

        public void Clear()
        {
            _stats.Clear();
            _damageTypes.Clear();
            _states.Clear();
        }

        public void RegisterStat(string statId) { if (!string.IsNullOrWhiteSpace(statId)) _stats.Add(statId); }
        public void RegisterDamageType(string damageTypeId) { if (!string.IsNullOrWhiteSpace(damageTypeId)) _damageTypes.Add(damageTypeId); }
        public void RegisterState(string stateId) { if (!string.IsNullOrWhiteSpace(stateId)) _states.Add(stateId); }

        public bool IsValidStat(string statId) => !string.IsNullOrWhiteSpace(statId) && _stats.Contains(statId);
        public bool IsValidDamageType(string damageTypeId) => !string.IsNullOrWhiteSpace(damageTypeId) && _damageTypes.Contains(damageTypeId);
        public bool IsValidState(string stateId) => !string.IsNullOrWhiteSpace(stateId) && _states.Contains(stateId);

        public float GetResistancePercent(string damageTypeId, IAffectTarget target)
        {
            if (target == null || target.Stats == null) return 0f;
            if (string.IsNullOrWhiteSpace(damageTypeId)) return 0f;

            // 기본 규칙: RESIST_{DamageTypeId}
            var statId = ResistancePrefix + damageTypeId;
            float v = target.Stats.GetValue(statId);
            // 0~100 범위를 퍼센트로 취급
            return Mathf.Clamp(v, 0f, 100f);
        }
    }
}
