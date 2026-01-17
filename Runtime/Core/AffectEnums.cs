namespace GGemCo2DAffect
{
    /// <summary>Buff/Debuff 분류.</summary>
    public enum DispelType { None = 0, Buff = 1, Debuff = 2 }

    /// <summary>동일 AffectUid 재적용(또는 동일 Group 충돌) 시 중첩 정책.</summary>
    public enum StackPolicy { Ignore = 0, Refresh = 1, Add = 2, Independent = 3 }

    /// <summary>Refresh 시 갱신 범위.</summary>
    public enum RefreshPolicy { None = 0, DurationOnly = 1, ValueAndDuration = 2 }

    /// <summary>Modifier가 실행되는 시점.</summary>
    public enum AffectPhase { OnApply = 0, OnTick = 1, OnExpire = 2 }

    /// <summary>Modifier의 종류(Stat/DamageType/State).</summary>
    public enum ModifierKind { Stat = 0, Damage = 1, State = 2, Custom = 3 }

    /// <summary>값 해석 방식.</summary>
    public enum ValueType { Flat = 0, Percent = 1 }

    /// <summary>Stat Modifier 연산.</summary>
    public enum StatOperation { Add = 0, Multiply = 1, Override = 2 }
}
