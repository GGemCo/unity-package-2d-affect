using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 1건에 연결되는 애니메이션 제어 정의입니다.
    /// </summary>
    /// <remarks>
    /// - Affect 본문/Modifier와 분리된 1:1 보조 정의로 사용합니다.
    /// - Start/Loop/End 애니메이션과 Stop 호출 여부를 함께 보관합니다.
    /// - 실제 재생 충돌 조정은 <see cref="IAffectAnimationService"/> 구현체가 담당합니다.
    /// </remarks>
    [Serializable]
    public sealed class AffectAnimationDefinition
    {
        /// <summary>
        /// 보조 정의의 고유 식별자입니다.
        /// </summary>
        public int uid;

        /// <summary>
        /// 연결 대상 Affect UID입니다.
        /// </summary>
        public int affectUid;

        /// <summary>
        /// 애니메이션이 실제로 활성화될 때 <c>CharacterBase.Stop(true)</c>를 호출할지 여부입니다.
        /// </summary>
        public bool stopCharacterOnApply;

        /// <summary>
        /// 적용 시작 시 1회 재생할 애니메이션 이름입니다.
        /// </summary>
        public string startAnimationName;

        /// <summary>
        /// 적용 유지 중 반복 재생할 애니메이션 이름입니다.
        /// </summary>
        public string loopAnimationName;

        /// <summary>
        /// 제거 시 1회 재생할 종료 애니메이션 이름입니다.
        /// </summary>
        public string endAnimationName;

        /// <summary>
        /// 여러 Affect 애니메이션이 동시에 존재할 때 우선순위를 결정합니다.
        /// 값이 높을수록 우선합니다.
        /// </summary>
        public int priority;

        /// <summary>
        /// 종료 애니메이션 이후 기본 Wait 애니메이션으로 복귀할지 여부입니다.
        /// </summary>
        public bool restoreWaitOnEnd;

        /// <summary>
        /// Start/Loop/End 중 하나라도 설정되어 있으면 유효한 정의로 간주합니다.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(startAnimationName) ||
            !string.IsNullOrWhiteSpace(loopAnimationName) ||
            !string.IsNullOrWhiteSpace(endAnimationName);
    }
}
