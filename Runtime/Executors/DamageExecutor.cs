using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Damage 타입 Modifier를 실행하는 실행기.
    /// </summary>
    /// <remarks>
    /// - 기본 정책은 "Apply 시 즉시 피해 없음"이며, 피해는 <see cref="ExecuteOnTick"/>에서 처리한다.
    /// - 최종 피해량은 (기본값 × 컨텍스트 배수 × 스택) + (스케일링 스탯 × 계수)로 계산한 뒤,
    ///   저항(%)을 반영해 대상에게 적용한다.
    /// </remarks>
    internal sealed class DamageExecutor : IModifierExecutor
    {
        /// <summary>
        /// Affect 적용 시점에 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용될 대상.</param>
        /// <param name="instance">현재 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소(현재 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        /// <remarks>
        /// 데미지는 기본적으로 Tick에서 처리하므로, Apply 단계에서는 아무 작업도 하지 않는다.
        /// </remarks>
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // 기본적으로 데미지는 Tick에서 처리한다.
        }

        /// <summary>
        /// Affect 틱 시점에 피해를 계산하여 적용한다.
        /// </summary>
        /// <param name="target">피해를 받을 대상.</param>
        /// <param name="instance">현재 Affect 인스턴스(스택/컨텍스트 포함).</param>
        /// <param name="mod">피해 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소(현재 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">저항(%) 조회에 사용되는 저장소.</param>
        /// <remarks>
        /// 계산 순서:
        /// 1) 기본 피해량: <c>damageBaseValue × ValueMultiplier × stacks</c>
        /// 2) 스케일링: <c>GetValue(scalingStatId) × scalingCoefficient</c> (옵션)
        /// 3) 저항 적용: <c>value × (1 - resist%)</c>
        /// </remarks>
        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            if (target == null || target.Damage == null || instance == null || mod == null) return;
            if (string.IsNullOrWhiteSpace(mod.damageTypeId)) return;

            // 외부 컨텍스트(예: 스킬 레벨, 난이도, 버프 증폭 등)에 의한 배수.
            float multiplier = instance.Context?.ValueMultiplier ?? 1f;

            // 스택은 최소 1로 보정한다(정의/데이터 오류에 대비).
            float stacks = Mathf.Max(1, instance.Stacks);

            float value = mod.damageBaseValue * multiplier * stacks;

            // 스케일링 스탯(옵션): value += (스탯 값 × 계수)
            if (!string.IsNullOrWhiteSpace(mod.scalingStatId) && target.Stats != null)
            {
                float scaleStat = target.Stats.GetValue(mod.scalingStatId);
                value += scaleStat * mod.scalingCoefficient;
            }

            // 저항(%) 적용: 최종값 = value × (1 - resist/100)
            float resist = statusRepo?.GetResistancePercent(mod.damageTypeId, target) ?? 0f;
            value *= (1f - Mathf.Clamp(resist, 0f, 100f) / 100f);

            if (value <= 0f) return;

            target.Damage.ApplyDamage(
                mod.damageTypeId,
                value,
                mod.canCrit,
                mod.isDot,
                instance.Context?.Source);
        }

        /// <summary>
        /// Affect 만료 시점에 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용되었던 대상.</param>
        /// <param name="instance">만료되는 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소(현재 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">상태/저항 정의 저장소(현재 실행기에서는 사용하지 않음).</param>
        /// <remarks>
        /// 현재 정책에서는 만료 시 추가 피해를 적용하지 않는다.
        /// </remarks>
        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // 만료 시 데미지 처리 없음.
        }
    }
}
