using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 공격자에게 걸린 "코팅/부여형" 버프가, 타격 성공 시 피격자에게 다른 Affect를 적용하는 실행기.
    /// 예) PoisonCoating(버프) -> OnHit 시 POISON_DOT(디버프) 적용
    /// </summary>
    internal sealed class ApplyAffectToTargetExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // OnApply에서는 동작하지 않는다.
        }

        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // OnTick에서는 동작하지 않는다.
        }

        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // OnExpire에서는 동작하지 않는다.
        }

        public void ExecuteOnHit(
            IAffectTarget attacker,
            IAffectTarget hitTarget,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            if (attacker == null || hitTarget == null || mod == null) return;

            int applyUid = mod.applyAffectUid;
            if (applyUid <= 0) return;

            // chance: 미기재(0)이면 1.0으로 간주
            float chance = Mathf.Clamp01(mod.applyAffectChance <= 0f ? 1f : mod.applyAffectChance);
            if (chance < 1f && Random.value > chance) return;

            var hitGo = hitTarget.Transform.gameObject;
            if (hitGo == null) return;

            // 피격자에게 Affect 시스템이 없으면 최소 구성으로 부착한다.
            // (IAffectTarget 구현은 CoreAffectTargetAdapter가 담당)
            if (hitGo.GetComponent<CoreAffectTargetAdapter>() == null)
                hitGo.AddComponent<CoreAffectTargetAdapter>();

            var comp = hitGo.GetComponent<AffectComponent>();
            if (comp == null)
                comp = hitGo.AddComponent<AffectComponent>();

            // Source는 공격자 GameObject를 전달한다.
            var ctx = new AffectApplyContext
            {
                Source = attacker.Transform.gameObject,
                DurationOverride = mod.applyAffectDurationOverride
            };

            comp.ApplyAffect(applyUid, ctx);

            // consumeOnProc 정책은 이후 확장(인스턴스 runtimeId 기반 안전 제거)로 처리한다.
        }
    }
}
