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
        /// Affect 값(데미지, 스탯 변화 등)에 곱해질 배율입니다.
        /// </summary>
        /// <remarks>
        /// 스킬 강화, 버프, 패시브 효과 등에 의해 최종 값에 반영됩니다.
        /// 기본값은 1입니다.
        /// </remarks>
        public float ValueMultiplier = 1f;
    }
}