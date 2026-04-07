using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// OnHit 시 대상에게 속성 게이지를 누적합니다.
    /// </summary>
    internal sealed class ElementGaugeExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
        }

        public void ExecuteOnTick(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
        }

        public void ExecuteOnExpire(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
        }

        public void ExecuteOnHit(IAffectTarget attacker, IAffectTarget hitTarget, AffectInstance instance, AffectModifierDefinition mod, IAffectDefinitionRepository affectRepo, IStatusDefinitionRepository statusRepo)
        {
            if (hitTarget == null || mod == null || instance == null)
                return;

            if (string.IsNullOrWhiteSpace(mod.damageTypeId))
                return;

            if (mod.elementGaugeValue <= 0f)
                return;

            var transform = hitTarget.Transform;
            if (transform == null)
                return;

            var controller = transform.GetComponent<CharacterElementGaugeController>();
            if (controller == null)
                return;

            float multiplier = instance.Context?.ValueMultiplier ?? 1f;
            float stacks = Mathf.Max(1, instance.Stacks);
            float gaugeValue = mod.elementGaugeValue * multiplier * stacks;
            if (gaugeValue <= 0f)
                return;

            var damageType = MapDamageType(mod.damageTypeId);
            if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic)
                return;

            var source = instance.Context?.Source as GameObject;
            if (source == null && attacker?.Transform != null)
                source = attacker.Transform.gameObject;

            controller.ApplyGauge(new ElementGaugeApplication(damageType, gaugeValue), source);
        }

        private static ConfigCommon.DamageType MapDamageType(string damageTypeId)
        {
            if (string.IsNullOrWhiteSpace(damageTypeId)) return ConfigCommon.DamageType.None;

            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Fire, StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Fire;
            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Cold, StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Cold;
            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Lightning, StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Lightning;
            if (string.Equals(damageTypeId, ConfigCommon.DamageTypeString.Poison, StringComparison.OrdinalIgnoreCase))
                return ConfigCommon.DamageType.Poison;

            return ConfigCommon.DamageType.None;
        }
    }
}
