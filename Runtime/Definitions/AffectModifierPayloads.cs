using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect Modifier의 Kind별 상세 값을 표현하는 공통 Payload 계약입니다.
    /// </summary>
    /// <remarks>
    /// 기존 <see cref="AffectModifierDefinition"/>의 레거시 필드는 유지하면서,
    /// 추후 Kind별 상세 테이블로 분리할 때 같은 DTO 구조를 재사용하기 위한 확장 지점입니다.
    /// </remarks>
    public interface IAffectModifierPayload
    {
        /// <summary>
        /// 이 Payload가 처리하는 Modifier Kind입니다.
        /// </summary>
        ModifierKind Kind { get; }
    }

    /// <summary>
    /// 스탯 변경 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectStatModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.Stat;

        /// <summary>변경할 스탯 ID입니다.</summary>
        public string statId;

        /// <summary>스탯 변경 값입니다.</summary>
        public float value;

        /// <summary>스탯 값의 해석 방식입니다.</summary>
        public StatValueType valueType;

        /// <summary>스탯 누적 연산 방식입니다.</summary>
        public StatOperation operation;
    }

    /// <summary>
    /// 피해 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectDamageModifierPayload : IAffectModifierPayload
    {
        /// <summary>
        /// 피해 Payload가 표현하는 실제 Modifier Kind입니다.
        /// </summary>
        public ModifierKind kind = ModifierKind.Damage;

        /// <summary>
        /// 기본 Damage Payload를 생성합니다.
        /// </summary>
        public AffectDamageModifierPayload()
        {
            showHitEffect = true;
        }

        /// <summary>
        /// Damage Payload를 생성합니다.
        /// </summary>
        /// <param name="kind">피해 계열 Modifier Kind입니다.</param>
        public AffectDamageModifierPayload(ModifierKind kind)
        {
            this.kind = ModifierKind.Damage;
            showHitEffect = true;
        }

        /// <inheritdoc />
        public ModifierKind Kind => kind;

        /// <summary>피해 타입 ID입니다.</summary>
        public string damageTypeId;

        /// <summary>기본 피해 값입니다.</summary>
        public float baseValue;

        /// <summary>피해 계산에 사용할 스케일링 스탯 ID입니다.</summary>
        public string scalingStatId;

        /// <summary>스케일링 계수입니다.</summary>
        public float scalingCoefficient;

        /// <summary>치명타 가능 여부입니다.</summary>
        public bool canCrit;

        /// <summary>지속 피해 여부입니다.</summary>
        public bool isDot;

        /// <summary>일반 피격 반응 억제 여부입니다.</summary>
        public bool suppressDamageReaction;

        /// <summary>피격 시각 효과 표시 여부입니다.</summary>
        public bool showHitEffect;
    }

    /// <summary>
    /// 회복 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectHealModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.Heal;

        /// <summary>기본 회복 값입니다.</summary>
        public float baseValue;

        /// <summary>회복 계산에 사용할 스케일링 스탯 ID입니다.</summary>
        public string scalingStatId;

        /// <summary>회복 스케일링 계수입니다.</summary>
        public float scalingCoefficient;
    }

    /// <summary>
    /// 상태 부여 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectStateModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.State;

        /// <summary>적용할 상태 ID입니다.</summary>
        public string stateId;

        /// <summary>상태 적용 확률입니다.</summary>
        public float chance;

        /// <summary>상태 지속 시간 오버라이드입니다.</summary>
        public float durationOverride;
    }

    /// <summary>
    /// CrowdControl 실행 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectCrowdControlModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.CrowdControl;

        /// <summary>참조할 CrowdControl UID입니다.</summary>
        public int crowdControlUid;
    }

    /// <summary>
    /// 다른 Affect를 추가 적용하는 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectApplyAffectModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.ApplyAffectToTarget;

        /// <summary>추가 적용할 Affect UID입니다.</summary>
        public int applyAffectUid;

        /// <summary>추가 Affect 적용 확률입니다.</summary>
        public float chance;

        /// <summary>추가 Affect 지속 시간 오버라이드입니다.</summary>
        public float durationOverride;

        /// <summary>발동 시 현재 Affect를 1회 소모할지 여부입니다.</summary>
        public bool consumeOnProc;
    }

    /// <summary>
    /// 데미지 공식 전용 변수 Modifier의 상세 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectFormulaVariableModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.FormulaVariable;

        /// <summary>공식 변수 ID입니다.</summary>
        public string variableId;

        /// <summary>공식 변수 값입니다.</summary>
        public float value;

        /// <summary>공식 변수 값의 해석 방식입니다.</summary>
        public StatValueType valueType;

        /// <summary>동일 공식 변수 누적 연산 방식입니다.</summary>
        public StatOperation operation;
    }

    /// <summary>
    /// 아직 전용 Payload가 없는 Custom Modifier용 Payload입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectCustomModifierPayload : IAffectModifierPayload
    {
        /// <inheritdoc />
        public ModifierKind Kind => ModifierKind.Custom;
    }

    /// <summary>
    /// 기존 wide-row 필드와 Kind별 Payload 사이를 변환하는 유틸리티입니다.
    /// </summary>
    public static class AffectModifierPayloadFactory
    {
        /// <summary>
        /// 기존 <see cref="AffectModifierDefinition"/> 필드 값을 기반으로 Kind에 맞는 Payload를 생성합니다.
        /// </summary>
        /// <param name="definition">Payload를 생성할 Modifier 정의입니다.</param>
        /// <returns>Kind에 맞는 Payload입니다.</returns>
        public static IAffectModifierPayload CreateFromLegacyFields(AffectModifierDefinition definition)
        {
            if (definition == null)
                return null;

            switch (definition.kind)
            {
                case ModifierKind.Stat:
                    return new AffectStatModifierPayload
                    {
                        statId = definition.statId,
                        value = definition.statValue,
                        valueType = definition.statValueType,
                        operation = definition.statOperation,
                    };

                case ModifierKind.Damage:
                    return new AffectDamageModifierPayload(definition.kind)
                    {
                        damageTypeId = definition.damageTypeId,
                        baseValue = definition.damageBaseValue,
                        scalingStatId = definition.scalingStatId,
                        scalingCoefficient = definition.scalingCoefficient,
                        canCrit = definition.canCrit,
                        isDot = definition.isDot,
                        suppressDamageReaction = definition.suppressDamageReaction,
                        showHitEffect = definition.showHitEffect,
                    };

                case ModifierKind.Heal:
                    return new AffectHealModifierPayload
                    {
                        baseValue = definition.healBaseValue,
                        scalingStatId = definition.healScalingStatId,
                        scalingCoefficient = definition.healScalingCoefficient,
                    };

                case ModifierKind.State:
                    return new AffectStateModifierPayload
                    {
                        stateId = definition.stateId,
                        chance = definition.stateChance,
                        durationOverride = definition.stateDurationOverride,
                    };

                case ModifierKind.CrowdControl:
                    return new AffectCrowdControlModifierPayload
                    {
                        crowdControlUid = definition.crowdControlUid,
                    };

                case ModifierKind.ApplyAffectToTarget:
                    return new AffectApplyAffectModifierPayload
                    {
                        applyAffectUid = definition.applyAffectUid,
                        chance = definition.applyAffectChance,
                        durationOverride = definition.applyAffectDurationOverride,
                        consumeOnProc = definition.consumeOnProc,
                    };

                case ModifierKind.FormulaVariable:
                    return new AffectFormulaVariableModifierPayload
                    {
                        variableId = definition.formulaVariableId,
                        value = definition.formulaVariableValue,
                        valueType = definition.formulaVariableValueType,
                        operation = definition.formulaVariableOperation,
                    };

                case ModifierKind.Custom:
                default:
                    return new AffectCustomModifierPayload();
            }
        }

        /// <summary>
        /// Payload 값을 기존 레거시 필드에 복사합니다.
        /// </summary>
        /// <param name="definition">값을 복사할 Modifier 정의입니다.</param>
        /// <remarks>
        /// 현재 일부 Executor가 레거시 필드를 읽으므로, 상세 테이블에서 Payload만 만든 경우에도
        /// 이 메서드를 호출하면 기존 실행 흐름을 그대로 사용할 수 있습니다.
        /// </remarks>
        public static void CopyToLegacyFields(AffectModifierDefinition definition)
        {
            if (definition?.payload == null)
                return;

            switch (definition.payload)
            {
                case AffectStatModifierPayload statPayload:
                    definition.kind = ModifierKind.Stat;
                    definition.statId = statPayload.statId;
                    definition.statValue = statPayload.value;
                    definition.statValueType = statPayload.valueType;
                    definition.statOperation = statPayload.operation;
                    break;

                case AffectDamageModifierPayload damagePayload:
                    definition.kind = damagePayload.Kind;
                    definition.damageTypeId = damagePayload.damageTypeId;
                    definition.damageBaseValue = damagePayload.baseValue;
                    definition.scalingStatId = damagePayload.scalingStatId;
                    definition.scalingCoefficient = damagePayload.scalingCoefficient;
                    definition.canCrit = damagePayload.canCrit;
                    definition.isDot = damagePayload.isDot;
                    definition.suppressDamageReaction = damagePayload.suppressDamageReaction;
                    definition.showHitEffect = damagePayload.showHitEffect;
                    break;

                case AffectHealModifierPayload healPayload:
                    definition.kind = ModifierKind.Heal;
                    definition.healBaseValue = healPayload.baseValue;
                    definition.healScalingStatId = healPayload.scalingStatId;
                    definition.healScalingCoefficient = healPayload.scalingCoefficient;
                    break;

                case AffectStateModifierPayload statePayload:
                    definition.kind = ModifierKind.State;
                    definition.stateId = statePayload.stateId;
                    definition.stateChance = statePayload.chance;
                    definition.stateDurationOverride = statePayload.durationOverride;
                    break;

                case AffectCrowdControlModifierPayload crowdControlPayload:
                    definition.kind = ModifierKind.CrowdControl;
                    definition.crowdControlUid = crowdControlPayload.crowdControlUid;
                    break;

                case AffectApplyAffectModifierPayload applyAffectPayload:
                    definition.kind = ModifierKind.ApplyAffectToTarget;
                    definition.applyAffectUid = applyAffectPayload.applyAffectUid;
                    definition.applyAffectChance = applyAffectPayload.chance;
                    definition.applyAffectDurationOverride = applyAffectPayload.durationOverride;
                    definition.consumeOnProc = applyAffectPayload.consumeOnProc;
                    break;

                case AffectFormulaVariableModifierPayload formulaVariablePayload:
                    definition.kind = ModifierKind.FormulaVariable;
                    definition.formulaVariableId = formulaVariablePayload.variableId;
                    definition.formulaVariableValue = formulaVariablePayload.value;
                    definition.formulaVariableValueType = formulaVariablePayload.valueType;
                    definition.formulaVariableOperation = formulaVariablePayload.operation;
                    break;

                case AffectCustomModifierPayload:
                    definition.kind = ModifierKind.Custom;
                    break;
            }
        }
    }
}
