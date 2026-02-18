using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Core 캐릭터(<see cref="CharacterBase"/>)를 Affect 런타임의 타깃 계약(<see cref="IAffectTarget"/>)에 연결하는 Unity 컴포넌트 어댑터.
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

        /// <summary>
        /// Affect 시스템이 참조할 Transform을 반환한다. 캐릭터가 있으면 캐릭터 Transform, 없으면 현재 GameObject Transform을 사용한다.
        /// </summary>
        public Transform Transform => _character != null ? _character.transform : transform;

        /// <summary>
        /// 캐릭터가 존재하며 사망 상태가 아닐 때 <c>true</c>를 반환한다.
        /// </summary>
        public bool IsAlive => _character != null && !_character.IsStatusDead();

        /// <summary>
        /// Core 스탯 조작(<see cref="IStatMutable"/>)을 제공한다.
        /// </summary>
        public IStatMutable Stats => _stats;

        /// <summary>
        /// Affect 상태 적용/해제(<see cref="IStateMutable"/>)를 제공한다. (현재는 로컬 상태 집합 기반)
        /// </summary>
        public IStateMutable States => _states;

        /// <summary>
        /// 피해/회복 적용(<see cref="IDamageReceiver"/>)을 제공한다.
        /// </summary>
        public IDamageReceiver Damage => _damage;

        /// <summary>
        /// 캐릭터 컴포넌트를 찾고, Affect 계약 구현체(스탯/상태/피해 수신)를 초기화한다.
        /// </summary>
        /// <remarks>
        /// <see cref="CharacterBase"/>가 없으면 동작할 수 없으므로 컴포넌트를 비활성화한다.
        /// </remarks>
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
            _states = new CoreStateMutable(_character);
            _damage = new CoreDamageReceiver(_character);
        }

        /// <summary>
        /// 스탯 모디파이어 적용 결과를 추적하기 위한 토큰.
        /// </summary>
        private sealed class StatToken
        {
            /// <summary>
            /// Core에 실제로 적용된 modifier 목록(원복 시 동일 목록으로 제거).
            /// </summary>
            public readonly List<ConfigCommon.StruckStatus> Modifiers;

            /// <param name="modifiers">Core에 적용한 modifier 목록.</param>
            public StatToken(List<ConfigCommon.StruckStatus> modifiers) => Modifiers = modifiers;
        }

        /// <summary>
        /// Affect의 스탯 변경 요청을 Core의 Suffix(+/-/Increase/Decrease) 기반 modifier 체계로 변환해 적용한다.
        /// </summary>
        private sealed class CoreStatMutable : IStatMutable
        {
            private readonly CharacterBase _character;

            /// <param name="character">스탯을 적용/조회할 대상 캐릭터.</param>
            public CoreStatMutable(CharacterBase character) => _character = character;

            /// <summary>
            /// 지정 스탯에 모디파이어를 적용하고, 이후 제거를 위한 토큰을 반환한다.
            /// </summary>
            /// <param name="statId">대상 스탯 ID.</param>
            /// <param name="value">적용 값(부호에 따라 증가/감소가 결정됨).</param>
            /// <param name="statValueType">값의 해석(절대/퍼센트 등).</param>
            /// <param name="operation">연산 방식(Add/Multiply/Override 등).</param>
            /// <returns>제거 시 사용할 토큰. 유효하지 않은 입력이면 <c>null</c>.</returns>
            /// <remarks>
            /// - Core는 Suffix 기반(+/-/증가/감소) modifier 체계를 사용한다.
            /// - Affect의 Multiply/Percent는 Core의 Increase/Decrease(퍼센트)로 매핑한다.
            /// - Affect의 Override는 Core에 직접적인 오버라이드가 없어 Add로 폴백한다.
            /// </remarks>
            public object ApplyModifier(string statId, float value, StatValueType statValueType, StatOperation operation)
            {
                if (string.IsNullOrWhiteSpace(statId)) return null;

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

            /// <summary>
            /// <see cref="ApplyModifier"/>가 반환한 토큰을 사용해 모디파이어를 제거하고 재계산한다.
            /// </summary>
            /// <param name="token">적용 시 반환된 토큰.</param>
            public void RemoveModifier(object token)
            {
                if (token is not StatToken t || t.Modifiers == null) return;

                _character.RemoveStatModifiers(t.Modifiers);
                _character.RecalculateStats();
            }

            /// <summary>
            /// 캐릭터의 파생 스탯을 재계산한다.
            /// </summary>
            public void Recalculate()
            {
                _character.RecalculateStats();
            }

            /// <summary>
            /// Core에서 제공하는 대표 스탯을 Affect의 statId로 조회한다.
            /// </summary>
            /// <param name="statId">조회할 스탯 ID.</param>
            /// <returns>매핑된 스탯 값. 알 수 없는 스탯이면 0.</returns>
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
                if (statId == ConfigCommon.StatusStatResistancePoison) return _character.TotalRegistPoison.Value;

                return 0f;
            }
        }

        /// <summary>
        /// 상태 적용 결과를 추적하기 위한 토큰.
        /// </summary>
        private sealed class StateToken
        {
            /// <summary>
            /// 적용된 상태 ID.
            /// </summary>
            public readonly string StateId;

            /// <param name="stateId">적용할 상태 ID.</param>
            public StateToken(string stateId) => StateId = stateId;
        }

        /// <summary>
        /// Affect의 상태(State) 적용/해제 요청을 간단한 문자열 집합으로 관리한다.
        /// </summary>
        /// <remarks>
        /// 실제 Core 상태/면역 시스템과 연동이 필요해지면 여기서 확장한다.
        /// </remarks>
        private sealed class CoreStateMutable : IStateMutable
        {
            // private const string StateDontControl = "DontControl";

            private readonly CharacterBase _character;

            // 동일 State가 여러 경로(그로기+컷씬+대화 등)로 중첩 적용될 수 있어 카운트로 관리한다.
            private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

            public CoreStateMutable(CharacterBase character)
            {
                _character = character;
            }

            /// <summary>
            /// 특정 상태가 적용되어 있는지 확인한다.
            /// </summary>
            /// <param name="stateId">상태 ID.</param>
            /// <returns>적용되어 있으면 <c>true</c>.</returns>
            public bool HasState(string stateId)
            {
                if (string.IsNullOrWhiteSpace(stateId)) return false;
                return _counts.TryGetValue(stateId, out var c) && c > 0;
            }

            /// <summary>
            /// 상태를 적용하고 제거를 위한 토큰을 반환한다.
            /// </summary>
            /// <param name="stateId">적용할 상태 ID.</param>
            /// <param name="duration">지속 시간(현재 구현에서는 저장/감소 처리하지 않음).</param>
            /// <returns>제거 시 사용할 토큰. 유효하지 않은 입력이면 <c>null</c>.</returns>
            /// <remarks>
            /// NOTE: duration 기반 만료/타이머 처리는 아직 구현되어 있지 않다.
            /// </remarks>
            public object ApplyState(string stateId, float duration)
            {
                if (string.IsNullOrWhiteSpace(stateId)) return null;

                int next = 1;
                if (_counts.TryGetValue(stateId, out var cur))
                    next = cur + 1;
                _counts[stateId] = next;

                // DontControl은 Core의 실제 상태로 브릿지한다.
                if (string.Equals(stateId, ConfigCommonAffect.State.DontControl, StringComparison.Ordinal))
                {
                    _character?.SetStatusDontControl();
                }

                return new StateToken(stateId);
            }

            /// <summary>
            /// <see cref="ApplyState"/>가 반환한 토큰을 사용해 상태를 제거한다.
            /// </summary>
            /// <param name="token">적용 시 반환된 토큰.</param>
            public void RemoveState(object token)
            {
                if (token is not StateToken t) return;
                if (string.IsNullOrWhiteSpace(t.StateId)) return;

                if (!_counts.TryGetValue(t.StateId, out var cur) || cur <= 0)
                    return;

                int next = cur - 1;
                if (next <= 0)
                    _counts.Remove(t.StateId);
                else
                    _counts[t.StateId] = next;

                // DontControl이 완전히 해제되면 Idle 상태로 복귀한다.
                // 정책: CharacterBase.Stop()을 호출하여 Idle 처리.
                if (next <= 0 && string.Equals(t.StateId, ConfigCommonAffect.State.DontControl, StringComparison.Ordinal))
                {
                    _character?.Stop();
                }
            }

            /// <summary>
            /// 특정 상태에 대한 면역 여부를 반환한다.
            /// </summary>
            /// <param name="stateId">면역 여부를 확인할 상태 ID.</param>
            /// <returns>현재는 항상 <c>false</c>.</returns>
            public bool IsImmune(string stateId)
            {
                // Core의 면역/저항 정책이 정식 도입되면 여기서 확장한다.
                return false;
            }
        }

        /// <summary>
        /// Affect의 피해/회복 요청을 Core 캐릭터 손상 로직으로 전달한다.
        /// </summary>
        private sealed class CoreDamageReceiver : IDamageReceiver
        {
            private readonly CharacterBase _character;

            /// <param name="character">피해/회복을 적용할 대상 캐릭터.</param>
            public CoreDamageReceiver(CharacterBase character) => _character = character;

            /// <summary>
            /// 피해를 적용한다.
            /// </summary>
            /// <param name="damageTypeId">피해 타입 ID(예: Fire/Cold/Lightning).</param>
            /// <param name="amount">피해량(0 이하이면 무시).</param>
            /// <param name="canCrit">치명타 가능 여부(현재 Core 전달 값에는 반영하지 않음).</param>
            /// <param name="isDot">지속 피해 여부(현재 Core 전달 값에는 반영하지 않음).</param>
            /// <param name="source">공격자/원인 객체(가능하면 <see cref="GameObject"/>로 전달).</param>
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

            /// <summary>
            /// 회복을 적용한다.
            /// </summary>
            /// <param name="amount">회복량(0 이하이면 무시).</param>
            /// <param name="source">회복의 원인 객체(현재는 사용하지 않음).</param>
            /// <remarks>
            /// Player 계열은 공개 메서드 <c>AddHp</c>가 있을 수 있어 우선 Reflection으로 호출한다.
            /// 그 외에는 <c>CurrentHp</c>를 직접 증가시키고 <c>TotalHp</c>를 상한으로 클램프한다.
            /// </remarks>
            public void ApplyHeal(float amount, object source)
            {
                if (_character == null) return;
                if (amount <= 0f) return;

                // Player는 AddHp가 존재하므로 우선 사용, 그 외에는 CurrentHp를 직접 갱신한다.
                var addHp = _character.GetType().GetMethod(
                    "AddHp",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

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

            /// <summary>
            /// 문자열 기반 피해 타입 ID를 Core의 <see cref="ConfigCommon.DamageType"/>로 매핑한다.
            /// </summary>
            /// <param name="damageTypeId">피해 타입 ID.</param>
            /// <returns>매핑 결과. 알 수 없으면 <see cref="ConfigCommon.DamageType.None"/>.</returns>
            private static ConfigCommon.DamageType MapDamageType(string damageTypeId)
            {
                if (string.IsNullOrWhiteSpace(damageTypeId)) return ConfigCommon.DamageType.None;

                // 프로젝트 테이블/정책에 따라 확장 가능.
                if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Fire, StringComparison.OrdinalIgnoreCase))
                    return ConfigCommon.DamageType.Fire;
                if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Cold, StringComparison.OrdinalIgnoreCase))
                    return ConfigCommon.DamageType.Cold;
                if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Lightning, StringComparison.OrdinalIgnoreCase))
                    return ConfigCommon.DamageType.Lightning;
                if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Poison, StringComparison.OrdinalIgnoreCase))
                    return ConfigCommon.DamageType.Poison;

                return ConfigCommon.DamageType.None;
            }
        }
    }
}
