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
