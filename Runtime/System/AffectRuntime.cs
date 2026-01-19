namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 시스템 전반에서 공용으로 사용하는 런타임 서비스/리포지토리 접근 지점.
    /// </summary>
    /// <remarks>
    /// - AffectComponent 등 여러 시스템에서 전역적으로 참조한다.
    /// - 기본 구현은 인메모리/Null 객체로 설정되어 있으며,
    ///   실제 게임 초기화 시 외부에서 교체 주입하는 것을 전제로 한다.
    /// </remarks>
    public static class AffectRuntime
    {
        /// <summary>
        /// 어펙트 정의(AffectDefinition)를 조회하기 위한 리포지토리.
        /// </summary>
        /// <remarks>
        /// 기본값은 InMemoryAffectRepository이며,
        /// 런타임 또는 부트스트랩 단계에서 다른 구현(DB/ScriptableObject 기반 등)으로 교체할 수 있다.
        /// </remarks>
        public static IAffectDefinitionRepository AffectRepository { get; set; }
            = new InMemoryAffectRepository();

        /// <summary>
        /// 상태(Status) 정의를 조회하기 위한 리포지토리.
        /// </summary>
        /// <remarks>
        /// 어펙트 Modifier 중 State/Stat 처리 시 참조된다.
        /// </remarks>
        public static IStatusDefinitionRepository StatusRepository { get; set; }
            = new InMemoryStatusRepository();

        /// <summary>
        /// 어펙트에 의해 재생되는 VFX 서비스를 제공한다.
        /// </summary>
        /// <remarks>
        /// 기본값은 NullAffectVfxService로, VFX가 필요 없는 환경에서도
        /// null 체크 없이 안전하게 호출할 수 있도록 한다(Null Object 패턴).
        /// </remarks>
        public static IAffectVfxService VfxService { get; set; }
            = new NullAffectVfxService();
    }
}