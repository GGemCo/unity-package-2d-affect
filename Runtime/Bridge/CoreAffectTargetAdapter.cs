using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Core 캐릭터(<see cref="CharacterBase"/>)를 Affect 런타임(<see cref="IAffectTarget"/>) 계약에 연결하는 어댑터.
    /// </summary>
    /// <remarks>
    /// - 이 클래스는 Affect 패키지에 위치하여 Core에만 의존한다.
    /// - Core는 Affect를 참조하지 않으므로, Core 쪽에서는 Reflection 브리지로 이 컴포넌트를 자동 부착한다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CoreAffectTargetAdapter : MonoBehaviour, IAffectTarget
    {
        private CharacterBase _character;
        private CoreStatMutable _stats;
        private CoreStateMutable _states;
        private CoreDamageReceiver _damage;

        public Transform Transform => _character != null ? _character.transform : transform;
        public bool IsAlive => _character != null && !_character.IsStatusDead();

        public IStatMutable Stats => _stats;
        public IStateMutable States => _states;
        public IDamageReceiver Damage => _damage;

        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            if (_character == null)
            {
                Debug.LogError($"[Affect][CoreAffectTargetAdapter] CharacterBase not found. name={name}");
                enabled = false;
                return;
            }

            _stats = new CoreStatMutable(_character);
            _states = new CoreStateMutable();
            _damage = new CoreDamageReceiver(_character);
        }

        private sealed class StatToken
        {
            public readonly List<ConfigCommon.StruckStatus> Modifiers;
            public StatToken(List<ConfigCommon.StruckStatus> modifiers) => Modifiers = modifiers;
        }

        private sealed class CoreStatMutable : IStatMutable
        {
            private readonly CharacterBase _character;

            public CoreStatMutable(CharacterBase character) => _character = character;

            public object ApplyModifier(string statId, float value, StatValueType statValueType, StatOperation operation)
            {
                if (string.IsNullOrWhiteSpace(statId)) return null;

                // Core는 Suffix(+/-/증가/감소) 기반 modifier 체계를 사용한다.
                // Affect의 Multiply/Percent는 Core의 Increase/Decrease(퍼센트)로 매핑한다.
                if (operation == StatOperation.Override)
                    operation = StatOperation.Add;

                var list = new List<ConfigCommon.StruckStatus>(capacity: 1);

                bool treatAsPercent = operation == StatOperation.Multiply || statValueType == StatValueType.Percent;
                float abs = Mathf.Abs(value);
                if (treatAsPercent)
                {
                    var suffix = value >= 0 ? ConfigCommon.SuffixType.Increase : ConfigCommon.SuffixType.Decrease;
                    list.Add(new ConfigCommon.StruckStatus(statId, suffix, abs));
                }
                else
                {
                    var suffix = value >= 0 ? ConfigCommon.SuffixType.Plus : ConfigCommon.SuffixType.Minus;
                    list.Add(new ConfigCommon.StruckStatus(statId, suffix, abs));
                }

                _character.ApplyStatModifiers(list);
                _character.RecalculateStats();
                return new StatToken(list);
            }

            public void RemoveModifier(object token)
            {
                if (token is not StatToken t || t.Modifiers == null) return;
                _character.RemoveStatModifiers(t.Modifiers);
                _character.RecalculateStats();
            }

            public void Recalculate()
            {
                _character.RecalculateStats();
            }

            public float GetValue(string statId)
            {
                if (string.IsNullOrWhiteSpace(statId)) return 0f;

                // Core의 대표 스탯 매핑 (필요 시 확장)
                if (statId == ConfigCommon.StatusStatAtk) return _character.TotalAtk.Value;
                if (statId == ConfigCommon.StatusStatDef) return _character.TotalDef.Value;
                if (statId == ConfigCommon.StatusStatHp) return _character.TotalHp.Value;
                if (statId == ConfigCommon.StatusStatMp) return _character.TotalMp.Value;
                if (statId == ConfigCommon.StatusStatMoveSpeed) return _character.TotalMoveSpeed.Value;
                if (statId == ConfigCommon.StatusStatAttackSpeed) return _character.TotalAttackSpeed.Value;
                if (statId == ConfigCommon.StatusStatCriticalDamage) return _character.TotalCriticalDamage.Value;
                if (statId == ConfigCommon.StatusStatCriticalProbability) return _character.TotalCriticalProbability.Value;

                // 저항(기존 Core 구현: Fire/Cold/Lightning)
                if (statId == ConfigCommon.StatusStatResistanceFire) return _character.TotalRegistFire.Value;
                if (statId == ConfigCommon.StatusStatResistanceCold) return _character.TotalRegistCold.Value;
                if (statId == ConfigCommon.StatusStatResistanceLightning) return _character.TotalRegistLightning.Value;

                return 0f;
            }
        }

        private sealed class StateToken
        {
            public readonly string StateId;
            public StateToken(string stateId) => StateId = stateId;
        }

        private sealed class CoreStateMutable : IStateMutable
        {
            private readonly HashSet<string> _states = new(StringComparer.Ordinal);

            public bool HasState(string stateId)
            {
                if (string.IsNullOrWhiteSpace(stateId)) return false;
                return _states.Contains(stateId);
            }

            public object ApplyState(string stateId, float duration)
            {
                if (string.IsNullOrWhiteSpace(stateId)) return null;
                _states.Add(stateId);
                return new StateToken(stateId);
            }

            public void RemoveState(object token)
            {
                if (token is not StateToken t) return;
                if (string.IsNullOrWhiteSpace(t.StateId)) return;
                _states.Remove(t.StateId);
            }

            public bool IsImmune(string stateId)
            {
                // Core의 면역/저항 정책이 정식 도입되면 여기서 확장한다.
                return false;
            }
        }

        private sealed class CoreDamageReceiver : IDamageReceiver
        {
            private readonly CharacterBase _character;

            public CoreDamageReceiver(CharacterBase character) => _character = character;

            public void ApplyDamage(string damageTypeId, float amount, bool canCrit, bool isDot, object source)
            {
                if (_character == null) return;
                if (amount <= 0f) return;

                var md = new MetadataDamage
                {
                    damage = (long)Mathf.RoundToInt(amount),
                    attacker = source as GameObject,
                    damageType = MapDamageType(damageTypeId),
                    affectUid = 0
                };

                _character.TakeDamage(md);
            }

            public void ApplyHeal(float amount, object source)
            {
                if (_character == null) return;
                if (amount <= 0f) return;

                // Player는 AddHp가 존재하므로 우선 사용, 그 외에는 CurrentHp를 직접 갱신한다.
                var addHp = _character.GetType().GetMethod("AddHp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (addHp != null)
                {
                    addHp.Invoke(_character, new object[] { (int)Mathf.RoundToInt(amount) });
                    return;
                }

                long newValue = _character.CurrentHp.Value + (long)Mathf.RoundToInt(amount);
                if (newValue > _character.TotalHp.Value)
                    newValue = _character.TotalHp.Value;
                _character.CurrentHp.OnNext(newValue);
            }

            private static SkillConstants.DamageType MapDamageType(string damageTypeId)
            {
                if (string.IsNullOrWhiteSpace(damageTypeId)) return SkillConstants.DamageType.None;

                // 프로젝트 테이블/정책에 따라 확장 가능.
                if (string.Equals(damageTypeId, "Fire", StringComparison.OrdinalIgnoreCase))
                    return SkillConstants.DamageType.Fire;
                if (string.Equals(damageTypeId, "Cold", StringComparison.OrdinalIgnoreCase))
                    return SkillConstants.DamageType.Cold;
                if (string.Equals(damageTypeId, "Lightning", StringComparison.OrdinalIgnoreCase))
                    return SkillConstants.DamageType.Lightning;

                return SkillConstants.DamageType.None;
            }
        }
    }
}
