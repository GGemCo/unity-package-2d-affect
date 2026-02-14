namespace GGemCo2DAffect
{
    /// <summary>Buff/Debuff 분류.</summary>
    public enum DispelType { None, Buff, Debuff }

    /// <summary>동일 AffectUid 재적용(또는 동일 Group 충돌) 시 중첩 정책.</summary>
    public enum StackPolicy { None, Refresh, Add, Independent }

    /// <summary>Refresh 시 갱신 범위.</summary>
    public enum RefreshPolicy { None, DurationOnly, ValueAndDuration }

    /// <summary>Modifier가 실행되는 시점.</summary>
    public enum AffectPhase { OnApply, OnTick, OnExpire }

    /// <summary>Modifier의 종류(Stat/DamageType/State).</summary>
    public enum ModifierKind { Stat, Damage, State, CrowdControl, Custom }

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
}
