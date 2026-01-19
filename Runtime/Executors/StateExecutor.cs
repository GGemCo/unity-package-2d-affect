using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// AffectModifierDefinition에 정의된 State(상태)를 대상에게 적용하는 실행기(Executor)입니다.
    /// </summary>
    /// <remarks>
    /// 면역 여부, 확률(StateChance), 지속 시간(Duration) 계산 후 상태를 부여하고,
    /// 반환된 토큰을 AffectInstance에 기록하여 이후 정리 단계에서 회수할 수 있도록 합니다.
    /// </remarks>
    internal sealed class StateExecutor : IModifierExecutor
    {
        /// <summary>
        /// 모디파이어가 적용(Apply)되는 시점에 상태(State)를 부여합니다.
        /// </summary>
        /// <param name="target">상태를 적용할 대상입니다.</param>
        /// <param name="instance">현재 적용 중인 Affect 인스턴스입니다(토큰 기록에 사용).</param>
        /// <param name="mod">상태 ID/확률/지속 시간 등의 설정을 담은 모디파이어 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">Status 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            TryApplyState(target, instance, mod);
        }

        /// <summary>
        /// 모디파이어가 Tick(주기 갱신)되는 시점에 필요할 경우 상태(State)를 갱신/추가합니다.
        /// </summary>
        /// <param name="target">상태를 적용할 대상입니다.</param>
        /// <param name="instance">현재 적용 중인 Affect 인스턴스입니다(토큰 기록에 사용).</param>
        /// <param name="mod">상태 ID/확률/지속 시간 등의 설정을 담은 모디파이어 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">Status 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <remarks>
        /// Tick 시 State 갱신/추가가 필요한 경우에만 사용합니다.
        /// </remarks>
        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // Tick 시 State 갱신/추가가 필요한 경우에만 사용한다.
            TryApplyState(target, instance, mod);
        }

        /// <summary>
        /// 모디파이어가 만료(Expire)되는 시점의 처리를 수행합니다.
        /// </summary>
        /// <param name="target">상태가 적용되었던 대상입니다.</param>
        /// <param name="instance">만료되는 Affect 인스턴스입니다.</param>
        /// <param name="mod">모디파이어 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <param name="statusRepo">Status 정의 조회를 위한 저장소입니다(이 실행기에서는 사용하지 않음).</param>
        /// <remarks>
        /// 토큰 회수는 AffectSystem 정리 단계에서 일괄 처리합니다.
        /// </remarks>
        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            // 토큰 회수는 AffectSystem 정리 단계에서 일괄 처리한다.
        }

        /// <summary>
        /// 설정에 따라 대상에게 상태(State)를 적용하고, 생성된 토큰을 인스턴스에 기록합니다.
        /// </summary>
        /// <param name="target">상태를 적용할 대상입니다.</param>
        /// <param name="instance">토큰을 누적 기록할 Affect 인스턴스입니다.</param>
        /// <param name="mod">상태 적용에 필요한 파라미터(상태 ID/확률/지속 시간)를 포함한 모디파이어 정의입니다.</param>
        private static void TryApplyState(IAffectTarget target, AffectInstance instance, AffectModifierDefinition mod)
        {
            // 필수 참조/데이터가 없으면 아무 것도 하지 않는다.
            if (target == null || target.States == null || instance == null || mod == null) return;

            // 적용할 State ID가 없으면 종료한다.
            if (string.IsNullOrWhiteSpace(mod.stateId)) return;

            // 대상이 해당 State에 면역이면 적용하지 않는다.
            if (target.States.IsImmune(mod.stateId)) return;

            // stateChance가 0 이하이면 100%로 간주하고, 그 외는 [0,1]로 클램프한다.
            float chance = Mathf.Clamp01(mod.stateChance <= 0f ? 1f : mod.stateChance);

            // 확률 적용: chance 미만으로 성공, 초과면 실패(적용 안 함).
            if (chance < 1f && Random.value > chance) return;

            // 지속 시간: override가 있으면 사용하고, 없으면 Affect 정의의 기본 지속 시간을 사용한다.
            float duration = mod.stateDurationOverride > 0f ? mod.stateDurationOverride : instance.Definition.baseDuration;

            // State 적용 후 토큰을 받아, 이후 정리/회수 가능하도록 인스턴스에 기록한다.
            var token = target.States.ApplyState(mod.stateId, duration);
            instance.AddStateToken(token);
        }
    }
}
