using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// <see cref="ModifierKind.Damage"/> Modifier를 실행하는 실행기입니다.
    /// </summary>
    /// <remarks>
    /// 데미지 계산식은 Tick/Expire에서 동일하게 재사용하며,
    /// 실행 시점(phase)은 호출 측(<see cref="AffectComponent"/>)에서 제어합니다.
    /// </remarks>
    internal sealed class DamageExecutor : IModifierExecutor
    {
        /// <summary>
        /// Damage Modifier의 OnApply 실행을 처리합니다.
        /// </summary>
        /// <param name="target">효과가 적용될 대상입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다.</param>
        /// <param name="mod">실행할 Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소입니다.</param>
        /// <remarks>
        /// 기본 정책상 Damage는 OnApply에서 즉시 적용하지 않습니다.
        /// </remarks>
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // 기본 정책: OnApply 즉시 데미지는 처리하지 않는다.
        }

        /// <summary>
        /// Damage Modifier의 OnTick 실행을 처리합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 대상입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다.</param>
        /// <param name="mod">실행할 Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소입니다.</param>
        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyDamageToTarget(target, instance, mod, affectRepo, statusRepo);
        }

        /// <summary>
        /// Damage Modifier의 OnExpire 실행을 처리합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 대상입니다.</param>
        /// <param name="instance">종료되는 Affect 인스턴스입니다.</param>
        /// <param name="mod">실행할 Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소입니다.</param>
        /// <remarks>
        /// 종료 시점 데미지 정책은 호출자(<see cref="AffectComponent"/>)에서 결정되며,
        /// 본 실행기는 실제 계산/적용만 담당합니다.
        /// </remarks>
        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyDamageToTarget(target, instance, mod, affectRepo, statusRepo);
        }

        /// <summary>
        /// Damage Modifier의 OnHit 실행을 처리합니다.
        /// </summary>
        /// <param name="attacker">공격자입니다.</param>
        /// <param name="hitTarget">피격자입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다.</param>
        /// <param name="mod">실행할 Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소입니다.</param>
        /// <remarks>
        /// ElementDamage처럼 OnHit 시점에 속성 데미지를 추가로 부여하는 Modifier에서 사용합니다.
        /// </remarks>
        public void ExecuteOnHit(
            IAffectTarget attacker,
            IAffectTarget hitTarget,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyDamageToTarget(hitTarget, instance, mod, affectRepo, statusRepo);
        }

        /// <summary>
        /// 즉시 데미지 Modifier를 실행합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 대상입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다.</param>
        /// <param name="mod">데미지 Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소입니다.</param>
        public void ExecuteImmediate(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            ApplyDamageToTarget(target, instance, mod, affectRepo, statusRepo);
        }

        /// <summary>
        /// Damage Modifier 정의를 기반으로 최종 데미지를 계산하여 대상에게 적용합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 대상입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다.</param>
        /// <param name="mod">데미지 Modifier 정의입니다.</param>
        /// <param name="affectRepo">사망 연출 정의를 조회할 Affect 저장소입니다.</param>
        /// <param name="statusRepo">저항 값을 조회할 저장소입니다.</param>
        /// <remarks>
        /// 계산 순서:
        /// 1) 기본 데미지 = <c>damageBaseValue * ValueMultiplier * stacks</c>
        /// 2) 스케일링 추가 = <c>targetStat * scalingCoefficient</c> (옵션)
        /// 3) 저항 반영 = <c>value * (1 - resist/100)</c>
        /// </remarks>
        private static void ApplyDamageToTarget(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            if (target == null || target.Damage == null || instance == null || mod == null)
                return;

            if (string.IsNullOrWhiteSpace(mod.damageTypeId))
                return;

            // 외부 컨텍스트(예: 스킬 레벨, 강화 계수)에서 전달된 배율
            float multiplier = instance.Context?.ValueMultiplier ?? 1f;

            // 잘못된 스택 값 방지: 최소 1 보장
            float stacks = Mathf.Max(1, instance.Stacks);
            float value = mod.damageBaseValue * multiplier * stacks;

            // 스케일링 스탯 적용(옵션)
            if (!string.IsNullOrWhiteSpace(mod.scalingStatId) && target.Stats != null)
            {
                float scaleStat = target.Stats.GetValue(mod.scalingStatId);
                value += scaleStat * mod.scalingCoefficient;
            }

            // 저항(%) 반영
            float resist = statusRepo?.GetResistancePercent(mod.damageTypeId, target) ?? 0f;
            value *= (1f - Mathf.Clamp(resist, 0f, 100f) / 100f);

            if (value <= 0f)
                return;

            target.Damage.ApplyDamage(
                mod.damageTypeId,
                value,
                mod.canCrit,
                mod.isDot,
                CreateDamageSourceContext(instance, mod, affectRepo));
        }

        /// <summary>
        /// Affect 데미지가 Core 데미지 파이프라인으로 전달될 때 사용할 원인 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="instance">현재 데미지를 발생시킨 Affect 인스턴스입니다.</param>
        /// <param name="mod">현재 실행 중인 Damage Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다.</param>
        /// <returns>Affect UID, 원천 객체, 사망 연출 후보를 담은 컨텍스트입니다.</returns>
        /// <remarks>
        /// 기존 Source 객체만 전달하면 Core에서 어떤 Affect가 Tick 데미지를 만들었는지 알 수 없으므로,
        /// 별도 컨텍스트로 감싸서 원천 객체와 Affect 원인을 동시에 보존합니다.
        /// </remarks>
        private static AffectDamageSourceContext CreateDamageSourceContext(
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo)
        {
            if (instance == null)
                return null;

            var context = new AffectDamageSourceContext
            {
                AffectUid = instance.Definition != null ? instance.Definition.uid : 0,
                Source = instance.Context?.Source,
                SuppressDamageReaction = mod != null && mod.suppressDamageReaction,
                SuppressHitEffect = mod != null && !mod.showHitEffect,
            };

            if (context.AffectUid > 0 &&
                affectRepo != null &&
                affectRepo.TryGetDeathPresentation(context.AffectUid, out var deathPresentation) &&
                deathPresentation != null &&
                deathPresentation.IsConfigured)
            {
                context.DeathPresentation = deathPresentation.ToRequest();
            }

            return context;
        }
    }
}
