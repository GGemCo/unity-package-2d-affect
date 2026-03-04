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

    /// <summary>Modifier의 종류(Stat/DamageType/State).</summary>
    public enum ModifierKind { Stat, Damage, Heal, State, CrowdControl, ApplyAffectToTarget, Custom }

    /// <summary>값 해석 방식.</summary>
    public enum StatValueType { None, Flat, Percent }

    /// <summary>Stat Modifier 연산.</summary>
    public enum StatOperation { None, Add, Multiply, Override }

    /// <summary>
    /// Affect 이펙트를 표시할 기준 위치 타입.
    /// </summary>
    public enum AffectEffectPositionType { Default, Head }

    /// <summary>
    /// Affect 이펙트가 타겟을 추적할지 여부.
    /// </summary>
    public enum AffectEffectFollowType { None, Follow }

    /// <summary>
    /// Affect 이펙트를 어펙트 지속시간 동안 반복(Loop)할지, 1회만 재생할지 선택합니다.
    /// </summary>
    public enum AffectEffectPlayMode
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
