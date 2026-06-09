using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect에 의해 적용되는 단일 Modifier(스탯/피해/상태 등)의 정의 데이터.
    /// </summary>
    /// <remarks>
    /// - 테이블 데이터를 런타임에서 해석하기 위한 순수 데이터 객체(DTO)이다.
    /// - 실제 적용 시점, 대상, 확률 처리 등은 런타임 로직(AffectInstance 등)에서 수행한다.
    /// - <see cref="kind"/> 값에 따라 사용되는 필드가 달라진다.
    /// </remarks>
    [Serializable]
    public sealed class AffectModifierDefinition
    {
        /// <summary>
        /// 이 Modifier가 속한 Affect의 UID.
        /// </summary>
        public int affectUid;

        /// <summary>
        /// Affect 내부에서 Modifier를 식별하기 위한 ID.
        /// </summary>
        public int modifierId;

        /// <summary>
        /// Modifier가 적용되는 Affect 처리 단계.
        /// </summary>
        public AffectPhase phase;

        /// <summary>
        /// Modifier의 동작 종류(Stat/Damage/State 등).
        /// </summary>
        public ModifierKind kind;

        // --------------------------------------------------------------------
        // Kind = Stat
        // --------------------------------------------------------------------

        /// <summary>
        /// 변경할 스탯 ID.
        /// </summary>
        public string statId;

        /// <summary>
        /// 스탯 변경 값(부호에 따라 증가/감소).
        /// </summary>
        public float statValue;

        /// <summary>
        /// 스탯 값의 해석 방식(절대값/퍼센트 등).
        /// </summary>
        public StatValueType statValueType;

        /// <summary>
        /// 스탯 연산 방식(Add/Multiply/Override 등).
        /// </summary>
        public StatOperation statOperation;

        // --------------------------------------------------------------------
        // Kind = Damage
        // --------------------------------------------------------------------

        /// <summary>
        /// 피해 타입 ID(예: Fire, Cold, Lightning).
        /// </summary>
        public string damageTypeId;

        /// <summary>
        /// 기본 피해 값.
        /// </summary>
        public float damageBaseValue;

        /// <summary>
        /// 피해 계산에 사용될 스케일링 스탯 ID.
        /// </summary>
        public string scalingStatId;

        /// <summary>
        /// 스케일링 계수(스탯 값에 곱해지는 비율).
        /// </summary>
        public float scalingCoefficient;

        /// <summary>
        /// 치명타 가능 여부.
        /// </summary>
        public bool canCrit;

        /// <summary>
        /// 지속 피해(Damage over Time) 여부.
        /// </summary>
        public bool isDot;

        // --------------------------------------------------------------------
        // Kind = Heal
        // --------------------------------------------------------------------

        /// <summary>
        /// 기본 회복 값(틱당 회복량 또는 즉시 회복량).
        /// </summary>
        public float healBaseValue;

        /// <summary>
        /// 회복 계산에 사용될 스케일링 스탯 ID(옵션).
        /// </summary>
        public string healScalingStatId;

        /// <summary>
        /// 스케일링 계수(스탯 값에 곱해지는 비율).
        /// </summary>
        public float healScalingCoefficient;


        // --------------------------------------------------------------------
        // Kind = State
        // --------------------------------------------------------------------

        /// <summary>
        /// 적용할 상태(State) ID.
        /// </summary>
        public string stateId;

        /// <summary>
        /// 상태 적용 확률(0~1 범위 기대).
        /// </summary>
        public float stateChance;

        /// <summary>
        /// 상태 지속 시간 오버라이드 값(0 이하이면 기본값 사용).
        /// </summary>
        public float stateDurationOverride;

        // --------------------------------------------------------------------
        // Kind == CrowdControl
        // --------------------------------------------------------------------
        /// <summary>
        /// CrowdControl 테이블(Uid)을 참조합니다.
        /// </summary>
        public int crowdControlUid;

        // --------------------------------------------------------------------
        // Kind == ApplyAffectToTarget
        // --------------------------------------------------------------------

        /// <summary>
        /// 피격자에게 추가로 적용할 Affect UID.
        /// </summary>
        public int applyAffectUid;

        /// <summary>
        /// 적용 확률(0~1). 0 이하이면 0, 1 이상이면 1로 처리된다.
        /// </summary>
        public float applyAffectChance;

        /// <summary>
        /// 적용할 Affect 지속시간 오버라이드(0 이하이면 기본값 사용).
        /// </summary>
        public float applyAffectDurationOverride;

        /// <summary>
        /// 발동 시 코팅 버프를 1회 소모(제거)할지 여부(옵션).
        /// </summary>
        public bool consumeOnProc;

        // --------------------------------------------------------------------
        // Kind = ElementGauge
        // --------------------------------------------------------------------

        /// <summary>
        /// 적중 시 누적할 속성 게이지 값입니다.
        /// </summary>
        public float elementGaugeValue;


        // --------------------------------------------------------------------
        // Kind = FormulaVariable
        // --------------------------------------------------------------------

        /// <summary>
        /// Poly 데미지 공식에만 주입할 어펙트 전용 변수 ID입니다.
        /// </summary>
        /// <remarks>
        /// 이 값은 Base*/Stat* 계산에는 절대 반영되지 않으며,
        /// <c>damage_formula</c> 공식 평가 직전에만 공식 변수로 등록됩니다.
        /// 예: <c>FormulaFinalDamageBuff</c>, <c>FORMULA_FINAL_DAMAGE_BUFF</c>.
        /// </remarks>
        public string formulaVariableId;

        /// <summary>
        /// 공식 변수에 누적할 값입니다.
        /// </summary>
        public float formulaVariableValue;

        /// <summary>
        /// 공식 변수 값의 해석 방식입니다.
        /// </summary>
        /// <remarks>
        /// 현재 공식 변수는 원본 수치를 그대로 보관합니다. Percent는 설명/데이터 의미를 표현하기 위한 값이며,
        /// 실제 배율 적용 여부는 <c>damage_formula</c> 수식에서 명시적으로 처리합니다.
        /// </remarks>
        public StatValueType formulaVariableValueType;

        /// <summary>
        /// 같은 공식 변수 ID가 여러 개 적용될 때의 누적 연산 방식입니다.
        /// </summary>
        public StatOperation formulaVariableOperation;

        // --------------------------------------------------------------------
        // Common
        // --------------------------------------------------------------------

        /// <summary>
        /// Modifier 적용 조건 식별자(확장용).
        /// </summary>
        /// <remarks>
        /// 예: <c>TargetHasState:STUN</c>, <c>TargetHpBelow:0.3</c> 등
        /// </remarks>
        public string conditionId;

        /// <summary>
        /// 디버깅 및 로그 출력을 위한 간략 문자열 표현을 반환한다.
        /// </summary>
        /// <returns>Modifier의 핵심 식별 정보 문자열.</returns>
        public override string ToString()
        {
            return $"AffectUid={affectUid}, ModifierId={modifierId}, Phase={phase}, Kind={kind}";
        }
    }
}
