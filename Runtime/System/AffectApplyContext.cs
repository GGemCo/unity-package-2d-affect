namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 적용 시 추가 정보를 전달하기 위한 컨텍스트 데이터 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 스킬 레벨, 값 배율, 지속 시간 오버라이드 등
    /// Affect 계산 및 적용 과정에서 참조되는 런타임 파라미터를 묶어 전달합니다.
    /// </remarks>
    public sealed class AffectApplyContext
    {
        /// <summary>
        /// Affect의 발생 원천(Source)입니다.
        /// </summary>
        /// <remarks>
        /// 공격자, 스킬, 아이템 등 Affect를 유발한 주체를 식별하는 데 사용됩니다.
        /// </remarks>
        public object Source;

        /// <summary>
        /// Affect를 발생시킨 스킬의 레벨입니다.
        /// </summary>
        /// <remarks>
        /// 스킬 레벨에 따른 스케일링 계산에 사용될 수 있습니다.
        /// </remarks>
        public int SkillLevel;

        /// <summary>
        /// Affect의 기본 지속 시간을 대체할 오버라이드 값입니다.
        /// </summary>
        /// <remarks>
        /// 0 이하인 경우 기본 지속 시간(Affect 정의의 값)을 사용합니다.
        /// </remarks>
        public float DurationOverride;

        /// <summary>
        /// Affect 기본 또는 오버라이드 지속시간에 추가로 더할 초 단위 보너스입니다.
        /// </summary>
        /// <remarks>
        /// 0 이하이면 추가 지속시간을 적용하지 않습니다.
        /// </remarks>
        public float DurationBonusSeconds;

        /// <summary>
        /// 이번 적용 인스턴스를 세션 수명으로 강제할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// 테이블의 기본 지속 시간이 있더라도 외부 조건으로 직접 제거해야 하는 디버프에 사용합니다.
        /// 이 값이 <see langword="true"/>이면 <see cref="AffectComponent.RemoveAffect"/> 같은 명시적 제거 전까지 자연 만료되지 않습니다.
        /// </remarks>
        public bool ForceSessionLifetime;

        /// <summary>
        /// Affect 값(데미지, 스탯 변화 등)에 곱해질 배율입니다.
        /// </summary>
        /// <remarks>
        /// 스킬 강화, 버프, 패시브 효과 등에 의해 최종 값에 반영됩니다.
        /// 기본값은 1입니다.
        /// </remarks>
        public float ValueMultiplier = 1f;

        /// <summary>
        /// Heal Modifier가 실제 회복량을 계산한 뒤 추가로 더할 HP 값입니다.
        /// </summary>
        /// <remarks>
        /// 스킬 콤보 슬롯, 패시브, 전투 규칙처럼 힐 전용 보정이 필요한 시스템에서 사용합니다.
        /// 0 이하이면 추가 회복량을 적용하지 않습니다.
        /// </remarks>
        public long HealHpBonus;

        /// <summary>
        /// Heal Modifier가 실제 회복량을 계산한 뒤 곱할 배율입니다.
        /// </summary>
        /// <remarks>
        /// 데미지/스탯 Modifier에 영향을 주는 ValueMultiplier와 달리 힐 계열 Modifier에만 적용합니다.
        /// 기본값은 1입니다.
        /// </remarks>
        public float HealHpMultiplier = 1f;
    }
}
