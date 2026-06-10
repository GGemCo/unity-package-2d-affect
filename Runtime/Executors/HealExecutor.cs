using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Heal 타입 Modifier를 실행하는 실행기.
    /// </summary>
    /// <remarks>
    /// - 기본 회복량은 (기본값 × 컨텍스트 배수 × 스택) + (스케일링 스탯 × 계수)로 계산한다.
    /// - 힐 전용 보정이 있으면 기본 회복량에 HealHpMultiplier를 곱하고 HealHpBonus를 더해 최종 회복량을 만든다.
    /// - 실제 "어떤 HP를 회복하는지(일반/임시)" 정책은 Core의 <see cref="IDamageReceiver.ApplyHeal"/> 구현체가 책임진다.
    ///   (현재 표준 정책: Heal은 일반 HP만 회복하며, 임시 HP는 회복하지 않는다.)
    /// </remarks>
    internal sealed class HealExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyHealToTarget(target, instance, mod);
        }

        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyHealToTarget(target, instance, mod);
        }

        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // 만료 시 회복 처리 없음.
        }

        public void ExecuteOnHit(
            IAffectTarget attacker,
            IAffectTarget hitTarget,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // Heal은 기본적으로 OnHit를 사용하지 않는다.
        }

        private static void ApplyHealToTarget(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod)
        {
            if (target == null || target.Damage == null || instance == null || mod == null) return;

            // 외부 컨텍스트(예: 스킬 레벨, 난이도, 버프 증폭 등)에 의한 배수.
            float multiplier = instance.Context?.ValueMultiplier ?? 1f;

            // 스택은 최소 1로 보정한다(정의/데이터 오류에 대비).
            float stacks = Mathf.Max(1, instance.Stacks);

            float value = mod.healBaseValue * multiplier * stacks;

            // 스케일링 스탯(옵션): value += (스탯 값 × 계수)
            if (!string.IsNullOrWhiteSpace(mod.healScalingStatId) && target.Stats != null)
            {
                float scaleStat = target.Stats.GetValue(mod.healScalingStatId);
                value += scaleStat * mod.healScalingCoefficient;
            }

            AffectApplyContext context = instance.Context;
            if (context != null)
            {
                float healMultiplier = context.HealHpMultiplier > 0f ? context.HealHpMultiplier : 1f;
                long healBonus = context.HealHpBonus > 0L ? context.HealHpBonus : 0L;
                value = value * healMultiplier + healBonus;
            }

            if (value <= 0f) return;

            target.Damage.ApplyHeal(value, context?.Source);
        }
    }
}
