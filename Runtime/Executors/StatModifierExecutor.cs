namespace GGemCo2DAffect
{
    internal sealed class StatModifierExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            if (target == null || target.Stats == null) return;
            if (mod == null || string.IsNullOrWhiteSpace(mod.statId)) return;

            // 값 배율(스킬 레벨/강화 등) 적용
            float multiplier = instance?.Context?.ValueMultiplier ?? 1f;
            float value = mod.statValue * multiplier;

            var token = target.Stats.ApplyModifier(mod.statId, value, mod.statValueType, mod.statOperation);
            if (instance != null) instance.AddStatToken(token);
            target.Stats.Recalculate();
        }

        public void ExecuteOnTick(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            // Stat Modifier는 기본적으로 Tick에서 처리하지 않는다.
        }

        public void ExecuteOnExpire(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            // 토큰 회수는 AffectComponent의 CleanupTokens에서 일괄 처리한다.
        }
    }
}
