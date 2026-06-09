using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect Modifier 값을 캐릭터 스탯이 아닌 데미지 공식 변수로만 등록하는 실행기입니다.
    /// </summary>
    /// <remarks>
    /// 이 실행기는 Base*/Stat* 계산을 변경하지 않습니다.
    /// 공식 계산 시점에는 <see cref="AffectFormulaVariableProvider"/>가 Core의 공식 변수 제공자 역할을 합니다.
    /// </remarks>
    internal sealed class FormulaVariableModifierExecutor : IModifierExecutor
    {
        /// <summary>
        /// OnApply 시점에 공식 변수 값을 대상 캐릭터에 등록합니다.
        /// </summary>
        /// <param name="target">공식 변수 값을 보유할 대상입니다.</param>
        /// <param name="instance">현재 Affect 인스턴스입니다. 해제 토큰 저장에 사용합니다.</param>
        /// <param name="mod">공식 변수 ID와 값을 담은 Modifier 정의입니다.</param>
        /// <param name="affectRepo">Affect 정의 저장소입니다. 이 실행기에서는 사용하지 않습니다.</param>
        /// <param name="statusRepo">상태 정의 저장소입니다. 이 실행기에서는 사용하지 않습니다.</param>
        public void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
            if (target == null || target.Transform == null || mod == null || string.IsNullOrWhiteSpace(mod.formulaVariableId))
                return;

            GameObject gameObject = target.Transform.gameObject;
            AffectFormulaVariableProvider provider = gameObject.GetComponent<AffectFormulaVariableProvider>();
            if (provider == null)
                provider = gameObject.AddComponent<AffectFormulaVariableProvider>();

            float multiplier = instance?.Context?.ValueMultiplier ?? 1f;
            float value = mod.formulaVariableValue * multiplier;
            object token = provider.AddVariable(
                mod.formulaVariableId,
                value,
                mod.formulaVariableValueType,
                mod.formulaVariableOperation);

            instance?.AddFormulaVariableToken(token);
        }

        /// <summary>
        /// 공식 변수 Modifier는 Tick에서 별도 동작하지 않습니다.
        /// </summary>
        public void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
        }

        /// <summary>
        /// 공식 변수 토큰 해제는 AffectComponent의 정리 단계에서 처리합니다.
        /// </summary>
        public void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
        }

        /// <summary>
        /// 공식 변수 Modifier는 OnHit에서 별도 동작하지 않습니다.
        /// </summary>
        public void ExecuteOnHit(
            IAffectTarget attacker,
            IAffectTarget hitTarget,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo)
        {
        }
    }
}
