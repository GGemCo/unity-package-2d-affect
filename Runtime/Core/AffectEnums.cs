namespace GGemCo2DAffect
{
    /// <summary>Buff/Debuff 분류.</summary>
    public enum DispelType { None, Buff, Debuff }

    /// <summary>동일 AffectUid 재적용(또는 동일 Group 충돌) 시 중첩 정책.</summary>
    public enum StackPolicy { None, Refresh, Add, Independent }

    /// <summary>Refresh 시 갱신 범위.</summary>
    public enum RefreshPolicy { None, DurationOnly, ValueAndDuration }

    /// <summary>Modifier가 실행되는 시점.</summary>
    public enum AffectPhase { OnApply, OnTick, OnHit, OnExpire }

    /// <summary>
    /// Affect 종료가 발생한 원인을 나타냅니다.
    /// </summary>
    /// <remarks>
    /// OnExpire 페이즈 실행 시, 종료 원인에 따라 일부 Modifier 실행 여부를
    /// 정책적으로 분기할 때 사용합니다.
    /// </remarks>
    public enum AffectExpireReason
    {
        /// <summary>
        /// 지속시간이 0이 되어 자연 만료된 경우입니다.
        /// </summary>
        NaturalExpire,

        /// <summary>
        /// 같은 그룹의 다른 Affect 적용으로 교체 제거된 경우입니다.
        /// </summary>
        ReplacedByGroup,

        /// <summary>
        /// 명시적 RemoveAffect 호출로 제거된 경우입니다.
        /// </summary>
        ManualRemove,

        /// <summary>
        /// Dispel 조건에 의해 해제된 경우입니다.
        /// </summary>
        Dispel,

        /// <summary>
        /// RemoveAll 호출로 일괄 제거된 경우입니다.
        /// </summary>
        RemoveAll
    }

    /// <summary>Modifier의 종류(Stat/DamageType/State).</summary>
    public enum ModifierKind { Stat, Damage, Heal, State, CrowdControl, ApplyAffectToTarget, ElementGauge, Custom }

    /// <summary>값 해석 방식.</summary>
    public enum StatValueType { None, Flat, Percent }

    /// <summary>Stat Modifier 연산.</summary>
    public enum StatOperation { None, Add, Multiply, Override }

    /// <summary>
    /// Affect 이펙트를 표시할 기준 위치 타입.
    /// </summary>
    public enum AffectVfxPositionType { Default, Head }

    /// <summary>
    /// Affect 이펙트가 타겟을 추적할지 여부.
    /// </summary>
    public enum AffectVfxFollowType { None, Follow }

    /// <summary>
    /// Affect 이펙트를 어펙트 지속시간 동안 반복(Loop)할지, 1회만 재생할지 선택합니다.
    /// </summary>
    public enum AffectVfxPlayMode
    {
        /// <summary>
        /// 어펙트 지속 시간(duration)에 맞춰 반복 재생합니다.
        /// (기존 동작과 동일)
        /// </summary>
        LoopDuringAffectDuration,

        /// <summary>
        /// 1회만 재생하고 종료합니다. (어펙트 지속시간과 무관)
        /// </summary>
        Once
    }
}
