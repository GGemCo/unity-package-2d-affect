using UnityEngine;

namespace GGemCo2DAffect
{
    internal sealed class StateExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            TryApplyState(target, instance, mod);
        }

        public void ExecuteOnTick(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            // Tick 시 State 갱신/추가가 필요한 경우에만 사용한다.
            TryApplyState(target, instance, mod);
        }

        public void ExecuteOnExpire(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            // 토큰 회수는 AffectSystem 정리 단계에서 일괄 처리한다.
        }

        private static void TryApplyState(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod)
        {
            if (target == null || target.States == null || instance == null || mod == null) return;
            if (string.IsNullOrWhiteSpace(mod.stateId)) return;

            if (target.States.IsImmune(mod.stateId)) return;

            float chance = Mathf.Clamp01(mod.stateChance <= 0f ? 1f : mod.stateChance);
            if (chance < 1f && Random.value > chance) return;

            float duration = mod.stateDurationOverride > 0f ? mod.stateDurationOverride : instance.Definition.BaseDuration;
            var token = target.States.ApplyState(mod.stateId, duration);
            instance.AddStateToken(token);
        }
    }
}
