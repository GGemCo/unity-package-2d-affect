using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// <see cref="ModifierKind.ElementGauge"/> Modifier를 실행하는 실행기입니다.
    /// </summary>
    /// <remarks>
    /// 이 실행기는 HP 데미지를 발생시키지 않고, <see cref="IElementGaugeReceiver"/>를 통해 속성 게이지 수치만 누적합니다.
    /// 따라서 데미지 공식, 피격 애니메이션, Hit VFX, 사망 처리와 독립적으로 동작합니다.
    /// </remarks>
    internal sealed class ElementGaugeExecutor : IModifierExecutor
    {
        /// <inheritdoc />
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyGauge(target, null, instance, mod);
        }

        /// <inheritdoc />
        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyGauge(target, null, instance, mod);
        }

        /// <inheritdoc />
        public void ExecuteOnHit(
            IAffectTarget attacker,
            IAffectTarget hitTarget,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyGauge(attacker, hitTarget, instance, mod);
        }

        /// <inheritdoc />
        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyGauge(target, null, instance, mod);
        }

        /// <summary>
        /// Modifier 설정을 기준으로 최종 누적 대상과 게이지 값을 계산해 적용합니다.
        /// </summary>
        /// <param name="ownerTarget">Affect를 보유한 대상입니다.</param>
        /// <param name="hitTarget">OnHit에서 전달된 피격 대상입니다. 다른 페이즈에서는 null입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다.</param>
        /// <param name="mod">속성 게이지 Modifier 정의입니다.</param>
        private static void ApplyGauge(
            IAffectTarget ownerTarget,
            IAffectTarget hitTarget,
            AffectInstance instance,
            AffectModifierDefinition mod)
        {
            if (ownerTarget == null || instance == null || mod == null)
                return;

            IAffectTarget target = ResolveTarget(ownerTarget, hitTarget, mod.elementGaugeTargetPolicy);
            if (target == null)
                return;

            if (mod.elementGaugeRequireAliveTarget && !target.IsAlive)
                return;

            if (target is not IElementGaugeReceiver receiver)
                return;

            if (string.IsNullOrWhiteSpace(mod.elementGaugeTypeId))
                return;

            float value = mod.elementGaugeValue;
            if (mod.elementGaugeUseContextMultiplier)
                value *= instance.Context?.ValueMultiplier ?? 1f;

            if (mod.elementGaugeUseStackMultiplier)
                value *= Mathf.Max(1, instance.Stacks);

            if (value <= 0f)
                return;

            receiver.AccumulateElementGauge(
                mod.elementGaugeTypeId,
                value,
                instance.Context?.Source,
                instance.Definition != null ? instance.Definition.uid : 0);
        }

        /// <summary>
        /// 페이즈와 정책에 맞는 게이지 누적 대상을 선택합니다.
        /// </summary>
        /// <param name="ownerTarget">Affect를 보유한 대상입니다.</param>
        /// <param name="hitTarget">OnHit 피격 대상입니다.</param>
        /// <param name="policy">대상 선택 정책입니다.</param>
        /// <returns>게이지를 누적할 대상입니다.</returns>
        private static IAffectTarget ResolveTarget(
            IAffectTarget ownerTarget,
            IAffectTarget hitTarget,
            ElementGaugeTargetPolicy policy)
        {
            return policy switch
            {
                ElementGaugeTargetPolicy.HitTarget => hitTarget ?? ownerTarget,
                ElementGaugeTargetPolicy.Self => ownerTarget,
                _ => ownerTarget
            };
        }
    }
}
