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
    public sealed class CoreAffectTargetAdapter : MonoBehaviour, IAffectTarget, IElementGaugeReceiver
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
        /// 캐릭터가 존재하며 사망 또는 사망 보류 상태가 아닐 때 <c>true</c>를 반환한다.
        /// </summary>
        public bool IsAlive => _character != null && !_character.IsStatusDead() && !_character.IsDeathPending;

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

        /// <inheritdoc />
        public bool AccumulateElementGauge(
            string elementTypeId,
            float gaugeValue,
            object source,
            int sourceAffectUid)
        {
            if (_character == null || gaugeValue <= 0f)
                return false;

            ConfigCommon.DamageType damageType = MapDamageType(elementTypeId);
            if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic)
                return false;

            CharacterElementGaugeController controller = _character.ElementGaugeController;
            if (controller == null)
                return false;

            ElementGaugeAccumulationResult result = controller.AccumulateDirect(
                damageType,
                gaugeValue,
                ResolveSourceGameObject(source),
                null);

            return result.GaugeChanged || result.ThresholdReached || result.RepeatedElementDamage;
        }

        /// <summary>
        /// Affect Source 객체를 Core 속성 게이지 컨텍스트에서 사용할 GameObject로 변환합니다.
        /// </summary>
        /// <param name="source">Affect 컨텍스트에 저장된 원천 객체입니다.</param>
        /// <returns>원천 GameObject입니다. 변환할 수 없으면 <see langword="null"/>입니다.</returns>
        private static GameObject ResolveSourceGameObject(object source)
        {
            if (source is GameObject go)
                return go;

            if (source is Component component)
                return component.gameObject;

            return null;
        }

        /// <summary>
        /// 문자열 기반 속성 타입 ID를 Core의 <see cref="ConfigCommon.DamageType"/>로 매핑합니다.
        /// </summary>
        /// <param name="damageTypeId">속성 타입 ID입니다.</param>
        /// <returns>매핑 결과입니다. 알 수 없으면 <see cref="ConfigCommon.DamageType.None"/>입니다.</returns>
        private static ConfigCommon.DamageType MapDamageType(string damageTypeId)
        {
            if (string.IsNullOrWhiteSpace(damageTypeId)) return ConfigCommon.DamageType.None;

            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Fire, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(damageTypeId, nameof(ConfigCommon.DamageType.Fire), StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Fire;

            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Cold, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(damageTypeId, nameof(ConfigCommon.DamageType.Cold), StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Cold;

            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Lightning, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(damageTypeId, nameof(ConfigCommon.DamageType.Lightning), StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Lightning;

            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Poison, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(damageTypeId, nameof(ConfigCommon.DamageType.Poison), StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Poison;

            return ConfigCommon.DamageType.None;
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

            /// <summary>
            /// 런타임 Temp HP Provider에 적용한 source key입니다.
            /// </summary>
            public readonly int? RuntimeTempHpSourceKey;

            /// <param name="modifiers">Core에 적용한 modifier 목록.</param>
            public StatToken(List<ConfigCommon.StruckStatus> modifiers) => Modifiers = modifiers;

            /// <param name="runtimeTempHpSourceKey">런타임 Temp HP Provider에 적용한 source key입니다.</param>
            public StatToken(int runtimeTempHpSourceKey) => RuntimeTempHpSourceKey = runtimeTempHpSourceKey;
        }

        /// <summary>
        /// Affect의 스탯 변경 요청을 Core의 Suffix(+/-/Increase/Decrease) 기반 modifier 체계로 변환해 적용한다.
        /// </summary>
        private sealed class CoreStatMutable : IStatMutable
        {
            private const int RuntimeTempHpSourceKeyStart = -2100000000;

            private readonly CharacterBase _character;
            private int _nextRuntimeTempHpSourceKey = RuntimeTempHpSourceKeyStart;

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
                statId = ConfigCommon.NormalizeStatId(statId);
                if (string.IsNullOrWhiteSpace(statId)) return null;

                if (TryApplyRuntimeTempHpModifier(statId, value, statValueType, operation, out object runtimeTempHpToken))
                    return runtimeTempHpToken;

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

                _character.ApplyAffectStatModifiers(list);
                _character.RecalculateStats();
                return new StatToken(list);
            }

            /// <summary>
            /// <see cref="ApplyModifier"/>가 반환한 토큰을 사용해 모디파이어를 제거하고 재계산한다.
            /// </summary>
            /// <param name="token">적용 시 반환된 토큰.</param>
            public void RemoveModifier(object token)
            {
                if (token is not StatToken t) return;

                if (t.RuntimeTempHpSourceKey.HasValue)
                {
                    _character.ClearRuntimeBonusHpTemp(t.RuntimeTempHpSourceKey.Value);
                    return;
                }

                if (t.Modifiers == null) return;

                _character.RemoveAffectStatModifiers(t.Modifiers);
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
            /// BASE_HP_TEMP 스탯 변경을 Core의 런타임 Temp HP Provider 경로로 적용합니다.
            /// </summary>
            /// <param name="statId">정규화된 스탯 ID입니다.</param>
            /// <param name="value">적용할 Temp HP 값입니다.</param>
            /// <param name="statValueType">값 해석 방식입니다.</param>
            /// <param name="operation">연산 방식입니다.</param>
            /// <param name="token">성공 시 해제용 토큰입니다.</param>
            /// <returns>런타임 Temp HP로 처리했으면 <c>true</c>를 반환합니다.</returns>
            /// <remarks>
            /// - BASE_HP_TEMP는 단순 스탯 Provider에만 넣으면 TotalHpTemp는 증가하지만 HUD의 Runtime Temp HP 세그먼트에 포함되지 않습니다.
            /// - 따라서 Flat/Add 양수 값은 저장되지 않는 런타임 Temp HP source로 등록하여 현재치까지 즉시 채우고 UI에 표시되게 합니다.
            /// - Percent/Multiply/Override 또는 0 이하 값은 보호막 정책과 의미가 맞지 않으므로 적용하지 않습니다.
            /// </remarks>
            private bool TryApplyRuntimeTempHpModifier(string statId, float value, StatValueType statValueType, StatOperation operation, out object token)
            {
                token = null;

                if (!ConfigCommon.IsHpTempStatId(statId))
                    return false;

                if (operation != StatOperation.Add || statValueType == StatValueType.Percent || value <= 0f)
                {
                    Debug.LogWarning($"[Affect][CoreAffectTargetAdapter] BASE_HP_TEMP는 Flat/Add 양수 값만 런타임 Temp HP로 적용할 수 있습니다. statId={statId}, value={value}, valueType={statValueType}, operation={operation}");
                    return true;
                }

                int sourceKey = AllocateRuntimeTempHpSourceKey();
                long amount = Math.Max(0L, (long)Mathf.RoundToInt(value));
                if (amount <= 0L)
                    return true;

                _character.SetRuntimeBonusHpTemp(sourceKey, amount, fillToMax: true);
                token = new StatToken(sourceKey);
                return true;
            }

            /// <summary>
            /// Affect에서 사용하는 런타임 Temp HP source key를 할당합니다.
            /// </summary>
            /// <returns>다른 런타임 시스템과 충돌 가능성을 낮추기 위한 음수 범위 source key입니다.</returns>
            private int AllocateRuntimeTempHpSourceKey()
            {
                int sourceKey = _nextRuntimeTempHpSourceKey;
                _nextRuntimeTempHpSourceKey++;
                if (_nextRuntimeTempHpSourceKey >= -2000000000)
                    _nextRuntimeTempHpSourceKey = RuntimeTempHpSourceKeyStart;
                return sourceKey;
            }

            /// <summary>
            /// Core에서 제공하는 스탯 값을 Affect의 statId 기준으로 조회합니다.
            /// </summary>
            /// <param name="statId">조회할 스탯 ID입니다.</param>
            /// <returns>매핑된 스탯 값입니다. 알 수 없는 스탯이면 0을 반환합니다.</returns>
            /// <remarks>
            /// - BASE_*는 기본 항목 계산 결과(TotalBase*)를 반환합니다.
            /// - BASE_HP_TEMP는 일반 HP와 분리된 보호막/임시 하트 최대치(TotalHpTemp)를 반환합니다.
            /// - STAT_ATK/STAT_DEF/STAT_HP/STAT_MP/STAT_STAMINA는 성장 스탯 계산 결과(TotalStat*)를 반환합니다.
            /// - 이동속도, 공격속도, 크리티컬, 저항, 기본 속성 데미지는 BASE_* 항목으로만 조회합니다.
            /// </remarks>
            public float GetValue(string statId)
            {
                if (string.IsNullOrWhiteSpace(statId)) return 0f;

                float baseValue = GetBaseStatValue(statId);
                if (baseValue > 0f || ConfigCommon.IsBaseStatId(statId)) return baseValue;

                float growthValue = GetGrowthStatValue(statId);
                if (growthValue > 0f || ConfigCommon.IsStatusStatId(statId)) return growthValue;

                return 0f;
            }

            /// <summary>
            /// BASE_* 계열 스탯 ID를 Core의 기본 항목 계산 결과로 변환합니다.
            /// </summary>
            /// <param name="statId">조회할 BASE_* 스탯 ID입니다.</param>
            /// <returns>매핑된 기본 항목 값입니다. 매핑되지 않은 항목이면 0을 반환합니다.</returns>
            private float GetBaseStatValue(string statId)
            {
                if (statId == ConfigCommon.BaseStatAtk) return _character.TotalBaseAtk.Value;
                if (statId == ConfigCommon.BaseStatDef) return _character.TotalBaseDef.Value;
                if (statId == ConfigCommon.BaseStatHp) return _character.TotalBaseHp.Value;
                if (statId == ConfigCommon.BaseStatMp) return _character.TotalBaseMp.Value;
                if (statId == ConfigCommon.BaseStatStamina) return _character.TotalBaseStamina.Value;
                if (ConfigCommon.IsHpTempStatId(statId)) return _character.TotalHpTemp.Value;
                if (statId == ConfigCommon.BaseStatSuperArmor) return _character.TotalSuperArmor.Value;
                if (statId == ConfigCommon.BaseStatMoveSpeed) return _character.TotalMoveSpeed.Value;
                if (statId == ConfigCommon.BaseStatAttackSpeed) return _character.TotalAttackSpeed.Value;
                if (statId == ConfigCommon.BaseStatCriticalDamage) return _character.TotalCriticalDamage.Value;
                if (statId == ConfigCommon.BaseStatCriticalProbability) return _character.TotalCriticalProbability.Value;
                if (statId == ConfigCommon.BaseStatRegistFire) return _character.TotalRegistFire.Value;
                if (statId == ConfigCommon.BaseStatRegistCold) return _character.TotalRegistCold.Value;
                if (statId == ConfigCommon.BaseStatRegistLightning) return _character.TotalRegistLightning.Value;
                if (statId == ConfigCommon.BaseStatRegistPoison) return _character.TotalRegistPoison.Value;
                if (statId == ConfigCommon.BaseStatDamageFire) return _character.TotalDamageFire.Value;
                if (statId == ConfigCommon.BaseStatDamageCold) return _character.TotalDamageCold.Value;
                if (statId == ConfigCommon.BaseStatDamageLightning) return _character.TotalDamageLightning.Value;
                if (statId == ConfigCommon.BaseStatDamagePoison) return _character.TotalDamagePoison.Value;
                return 0f;
            }

            /// <summary>
            /// STAT_* 계열 스탯 ID를 Core의 성장 스탯 계산 결과로 변환합니다.
            /// </summary>
            /// <param name="statId">조회할 STAT_* 스탯 ID입니다.</param>
            /// <returns>매핑된 스탯 값입니다. 매핑되지 않은 항목이면 0을 반환합니다.</returns>
            private float GetGrowthStatValue(string statId)
            {
                if (statId == ConfigCommon.StatusStatAtk) return _character.TotalStatAtk.Value;
                if (statId == ConfigCommon.StatusStatDef) return _character.TotalStatDef.Value;
                if (statId == ConfigCommon.StatusStatHp) return _character.TotalStatHp.Value;
                if (statId == ConfigCommon.StatusStatMp) return _character.TotalStatMp.Value;
                if (statId == ConfigCommon.StatusStatStamina) return _character.TotalStatStamina.Value;
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

            /// <summary>
            /// Core 캐릭터에 획득한 외부 제어 잠금 토큰입니다.
            /// </summary>
            public readonly object ControlLockToken;

            /// <param name="stateId">적용할 상태 ID.</param>
            /// <param name="controlLockToken">Core 제어 잠금 토큰입니다.</param>
            public StateToken(string stateId, object controlLockToken = null)
            {
                StateId = stateId;
                ControlLockToken = controlLockToken;
            }
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

                object controlLockToken = null;

                // DontControl은 Core의 공용 제어 잠금으로 브리지한다.
                if (string.Equals(stateId, ConfigCommonAffect.State.DontControl, StringComparison.Ordinal))
                {
                    controlLockToken = _character?.AcquireControlLock();
                }

                return new StateToken(stateId, controlLockToken);
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

                if (string.Equals(t.StateId, ConfigCommonAffect.State.DontControl, StringComparison.Ordinal))
                {
                    _character?.ReleaseControlLock(t.ControlLockToken);
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
            /// <param name="isDot">지속 피해 여부입니다. 지속 피해이면 Core 가드/저스트 가드 판정에서 제외합니다.</param>
            /// <param name="source">공격자/원인 객체(가능하면 <see cref="GameObject"/>로 전달).</param>
            public void ApplyDamage(string damageTypeId, float amount, bool canCrit, bool isDot, object source)
            {
                if (_character == null) return;
                if (amount <= 0f) return;

                var affectSource = source as AffectDamageSourceContext;
                object originalSource = affectSource != null ? affectSource.Source : source;
                long roundedAmount = Mathf.RoundToInt(amount);
                ConfigCommon.DamageType damageType = MapDamageType(damageTypeId);
                DamageCalculationBreakdown damageBreakdown = CreateAffectDamageBreakdown(
                    roundedAmount,
                    damageType,
                    isDot);

                var md = new MetadataDamage
                {
                    damage = roundedAmount,
                    attacker = ResolveSourceGameObject(originalSource),
                    damageType = damageType,
                    DamageBreakdown = damageBreakdown,
                    IsDamageOverTime = isDot,
                    GuardInteractionMode = isDot ? GuardInteractionMode.IgnoreGuard : GuardInteractionMode.Normal,
                    GuardAttackType = isDot ? GuardAttackType.None : GuardAttackType.Normal,
                    affectUid = 0,
                    SourceAffectUid = affectSource != null ? affectSource.AffectUid : 0,
                    SuppressDamageReaction = affectSource != null && affectSource.SuppressDamageReaction,
                    SuppressHitEffect = affectSource != null && affectSource.SuppressHitEffect,
                    DeathPresentation = affectSource != null && affectSource.DeathPresentation != null
                        ? affectSource.DeathPresentation.Clone()
                        : null
                };

                _character.TakeDamage(md);
            }

            /// <summary>
            /// Affect에서 전달한 데미지를 Core 데미지 분해 결과로 변환합니다.
            /// </summary>
            /// <param name="amount">Affect Executor가 계산한 대상 저항 적용 전 데미지입니다.</param>
            /// <param name="damageType">Core 데미지 타입입니다.</param>
            /// <param name="isDot">지속 피해 여부입니다.</param>
            /// <returns>Affect 데미지를 표현하는 단일 파트 데미지 분해 결과입니다.</returns>
            private static DamageCalculationBreakdown CreateAffectDamageBreakdown(
                long amount,
                ConfigCommon.DamageType damageType,
                bool isDot)
            {
                var breakdown = new DamageCalculationBreakdown();
                breakdown.AddPart(new DamagePartResult(
                    amount,
                    amount,
                    damageType,
                    0L,
                    false,
                    false,
                    isDot));
                return breakdown;
            }

            /// <summary>
            /// 회복을 적용한다.
            /// </summary>
            /// <param name="amount">회복량(0 이하이면 무시).</param>
            /// <param name="source">회복의 원인 객체(현재는 사용하지 않음).</param>
            /// <remarks>
            /// Player 계열은 공개 메서드 <c>AddHp</c>가 있을 수 있어 우선 Reflection으로 호출한다.
            /// 그 외에는 <c>CurrentHp</c>(일반 HP)를 직접 증가시키고 <c>MaxHp</c>(일반 최대 HP)를 상한으로 클램프한다.
            /// - 표준 정책: Heal은 임시 HP(<c>TotalTempHp</c>/<c>_tempHpCurrent</c>)를 회복하지 않는다.
            /// </remarks>
            public void ApplyHeal(float amount, object source)
            {
                if (_character == null) return;
                if (amount <= 0f) return;

                _character.AddHp(Mathf.CeilToInt(amount));
            }


            /// <summary>
            /// Affect Source 객체를 Core 데미지 메타데이터에서 사용할 GameObject로 변환합니다.
            /// </summary>
            /// <param name="source">Affect 컨텍스트에 저장된 원천 객체입니다.</param>
            /// <returns>원천 GameObject입니다. 변환할 수 없으면 <see langword="null"/>입니다.</returns>
            /// <remarks>
            /// Source가 GameObject가 아니라 Component나 CharacterBase로 전달되는 경우도 안전하게 처리합니다.
            /// </remarks>
            private static GameObject ResolveSourceGameObject(object source)
            {
                if (source is GameObject go)
                    return go;

                if (source is Component component)
                    return component.gameObject;

                return null;
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
