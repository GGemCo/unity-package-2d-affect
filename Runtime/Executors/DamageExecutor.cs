using UnityEngine;

namespace GGemCo2DAffect
{
    internal sealed class DamageExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            // 기본적으로 데미지는 Tick에서 처리한다.
        }

        public void ExecuteOnTick(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            if (target == null || target.Damage == null || instance == null || mod == null) return;
            if (string.IsNullOrWhiteSpace(mod.damageTypeId)) return;

            float multiplier = instance.Context?.ValueMultiplier ?? 1f;
            float stacks = Mathf.Max(1, instance.Stacks);

            float value = mod.damageBaseValue * multiplier * stacks;

            if (!string.IsNullOrWhiteSpace(mod.scalingStatId) && target.Stats != null)
            {
                float scaleStat = target.Stats.GetValue(mod.scalingStatId);
                value += scaleStat * mod.scalingCoefficient;
            }

            // 저항(%) 적용
            float resist = statusRepo?.GetResistancePercent(mod.damageTypeId, target) ?? 0f;
            value *= (1f - Mathf.Clamp(resist, 0f, 100f) / 100f);

            if (value <= 0f) return;

            target.Damage.ApplyDamage(mod.damageTypeId, value, mod.canCrit, mod.isDot, instance.Context?.Source);
        }

        public void ExecuteOnExpire(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            // 만료 시 데미지 처리 없음.
        }
    }
}
