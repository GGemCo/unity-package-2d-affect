namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect Modifier 실행기를 위한 공통 인터페이스.
    /// </summary>
    /// <remarks>
    /// - Modifier의 종류(Stat/Damage/State 등)에 따라 서로 다른 실행기가 이 인터페이스를 구현한다.
    /// - 하나의 Modifier는 Affect 수명 주기(Apply/Tick/Expire)에 따라 서로 다른 시점에서 실행될 수 있다.
    /// - 실제 실행 여부와 호출 타이밍은 Affect 런타임(AffectInstance/AffectComponent)에서 제어한다.
    /// </remarks>
    internal interface IModifierExecutor
    {
        /// <summary>
        /// Affect가 대상에 최초 적용될 때 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용될 대상.</param>
        /// <param name="instance">현재 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo);

        /// <summary>
        /// Affect의 틱(Tick) 주기마다 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용된 대상.</param>
        /// <param name="instance">현재 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo);

        /// <summary>
        /// Affect가 만료되거나 제거될 때 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용되었던 대상.</param>
        /// <param name="instance">만료되는 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo);
    }
}
