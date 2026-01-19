namespace GGemCo2DAffect
{
    /// <summary>
    /// AffectModifierDefinition에 정의된 스탯(Stat) 변경을 대상에게 적용하는 실행기(Executor)입니다.
    /// </summary>
    /// <remarks>
    /// 스킬 레벨/강화 등으로 인한 값 배율(ValueMultiplier)을 반영하여 스탯 모디파이어를 적용하고,
    /// 반환된 토큰을 AffectInstance에 기록해 이후 정리 단계에서 회수할 수 있도록 합니다.
    /// 적용 직후 Stats.Recalculate()를 호출하여 최종 스탯을 갱신합니다.
    /// </remarks>
    internal sealed class StatModifierExecutor : IModifierExecutor
    {
        /// <summary>
        /// 모디파이어가 적용(Apply)되는 시점에 스탯(Stat) 모디파이어를 부여하고 스탯을 재계산합니다.
        /// </summary>
        /// <param name="target">스탯 모디파이어를 적용할 대상입니다.</param>
        /// <param name="instance">현재 적용 중인 Affect 인스턴스입니다(값 배율/토큰 기록에 사용).</param>
        /// <param name="mod">스탯 ID/값/값 타입/연산 방식 등의 설정을 담은 모디파이어 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">Status 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            if (target == null || target.Stats == null) return;
            if (mod == null || string.IsNullOrWhiteSpace(mod.statId)) return;

            // 값 배율(스킬 레벨/강화 등) 적용
            float multiplier = instance?.Context?.ValueMultiplier ?? 1f;
            float value = mod.statValue * multiplier;

            // 스탯 모디파이어를 적용하고, 이후 회수를 위해 토큰을 인스턴스에 기록한다.
            var token = target.Stats.ApplyModifier(mod.statId, value, mod.statValueType, mod.statOperation);
            if (instance != null) instance.AddStatToken(token);

            // 적용 결과를 즉시 반영하기 위해 스탯을 재계산한다.
            target.Stats.Recalculate();
        }

        /// <summary>
        /// 모디파이어가 Tick(주기 갱신)되는 시점의 처리를 수행합니다.
        /// </summary>
        /// <param name="target">스탯 모디파이어가 적용된 대상입니다.</param>
        /// <param name="instance">현재 적용 중인 Affect 인스턴스입니다.</param>
        /// <param name="mod">모디파이어 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">Status 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <remarks>
        /// Stat Modifier는 기본적으로 Tick에서 처리하지 않습니다.
        /// </remarks>
        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // Stat Modifier는 기본적으로 Tick에서 처리하지 않는다.
        }

        /// <summary>
        /// 모디파이어가 만료(Expire)되는 시점의 처리를 수행합니다.
        /// </summary>
        /// <param name="target">스탯 모디파이어가 적용되었던 대상입니다.</param>
        /// <param name="instance">만료되는 Affect 인스턴스입니다.</param>
        /// <param name="mod">모디파이어 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">Status 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <remarks>
        /// 토큰 회수는 AffectComponent의 CleanupTokens에서 일괄 처리합니다.
        /// </remarks>
        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // 토큰 회수는 AffectComponent의 CleanupTokens에서 일괄 처리한다.
        }
    }
}
