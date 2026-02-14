using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// AffectModifierDefinition에 정의된 CrowdControl을 대상에게 적용하는 실행기(Executor)입니다.
    /// </summary>
    internal sealed class CrowdControlExecutor : IModifierExecutor
    {
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            if (target == null) return;
            if (instance == null) return;
            if (mod == null) return;
            if (mod.crowdControlUid <= 0) return;

            var tableManager = TableLoaderManager.Instance;
            var tableCrowdControl = tableManager != null ? tableManager.TableCrowdControl : null;
            if (tableCrowdControl == null) return;

            var crowdControl = tableCrowdControl.GetDataByUid(mod.crowdControlUid);
            if (crowdControl == null) return;

            // Target GameObject
            var go = target.Transform != null ? target.Transform.gameObject : null;
            if (go == null) return;

            var controller = go.GetComponent<CharacterCrowdControlController>();
            if (controller == null)
                controller = go.AddComponent<CharacterCrowdControlController>();

            var sourceGo = instance.Context != null ? instance.Context.Source as GameObject : null;
            controller.ApplyCrowdControl(crowdControl, sourceGo);
        }

        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // CrowdControl은 기본적으로 OnApply 1회 실행 정책입니다.
        }

        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // CrowdControl은 만료 시 별도 처리가 필요하면 Core 쪽에서 확장합니다.
        }
    }
}
