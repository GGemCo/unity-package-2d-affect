using System;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 특정 Affect가 사망 원인이 되었을 때 사용할 사망 연출 정의입니다.
    /// </summary>
    /// <remarks>
    /// Affect 패키지는 이 정의를 Core의 <see cref="DeathPresentationRequest"/>로 변환하여 전달합니다.
    /// Core는 Affect UID를 직접 해석하지 않고 범용 사망 연출 요청만 실행합니다.
    /// </remarks>
    [Serializable]
    public sealed class AffectDeathPresentationDefinition
    {
        /// <summary>
        /// 사망 연출 정의의 고유 식별자입니다.
        /// </summary>
        public int uid;

        /// <summary>
        /// 연결 대상 Affect UID입니다.
        /// </summary>
        public int affectUid;

        /// <summary>
        /// 여러 사망 연출 후보 중 우선 적용할 순서를 결정합니다.
        /// 값이 높을수록 우선합니다.
        /// </summary>
        public int priority;

        /// <summary>
        /// 사망 시 재생할 캐릭터 애니메이션 이름입니다.
        /// </summary>
        public string deathAnimationName;

        /// <summary>
        /// 사망 시 재생할 VFX UID입니다.
        /// </summary>
        public int deathVfxUid;

        /// <summary>
        /// 사망 VFX 스케일 오버라이드입니다.
        /// </summary>
        public float deathVfxScale;

        /// <summary>
        /// 사망 VFX Y축 오프셋입니다.
        /// </summary>
        public float deathVfxOffsetY;

        /// <summary>
        /// 사망 VFX 위치 기준입니다.
        /// </summary>
        public AffectVfxPositionType deathVfxPositionType;

        /// <summary>
        /// 사망 VFX 추적 정책입니다.
        /// </summary>
        public AffectVfxFollowType deathVfxFollowType;

        /// <summary>
        /// 사망 VFX 정렬 레이어 키입니다.
        /// </summary>
        public ConfigSortingLayer.Keys deathVfxSortingLayerKey;

        /// <summary>
        /// 사망 VFX 정렬 레이어 오버라이드 사용 여부입니다.
        /// </summary>
        public bool useDeathVfxSortingLayer;

        /// <summary>
        /// 사망 VFX 지속 시간 오버라이드입니다.
        /// </summary>
        public float deathVfxDurationOverride;

        /// <summary>
        /// 사망 시 재생할 컷씬 UID입니다.
        /// </summary>
        public int deathCutsceneUid;

        /// <summary>
        /// 전용 애니메이션이 없을 때 기본 사망 애니메이션을 막을지 여부입니다.
        /// </summary>
        public bool suppressDefaultDeathAnimation;

        /// <summary>
        /// 사망 애니메이션 종료 후 마지막 프레임에 고정할지 여부입니다.
        /// </summary>
        public bool freezeLastFrame;

        /// <summary>
        /// 사망 연출로 실행 가능한 데이터가 하나라도 있는지 확인합니다.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(deathAnimationName) ||
            deathVfxUid > 0 ||
            deathCutsceneUid > 0 ||
            suppressDefaultDeathAnimation ||
            freezeLastFrame;

        /// <summary>
        /// Affect 사망 연출 정의를 Core 범용 사망 연출 요청으로 변환합니다.
        /// </summary>
        /// <returns>Core에서 실행 가능한 사망 연출 요청입니다.</returns>
        public DeathPresentationRequest ToRequest()
        {
            return new DeathPresentationRequest
            {
                AnimationName = deathAnimationName,
                VfxUid = deathVfxUid,
                VfxScale = deathVfxScale,
                VfxOffsetY = deathVfxOffsetY,
                VfxPositionYType = deathVfxPositionType == AffectVfxPositionType.Head
                    ? ConfigCommon.PositionYType.CharacterHeight
                    : ConfigCommon.PositionYType.None,
                FollowVfxTarget = deathVfxFollowType == AffectVfxFollowType.Follow,
                HasVfxSortingLayerOverride = useDeathVfxSortingLayer,
                VfxSortingLayerKey = deathVfxSortingLayerKey,
                VfxDurationOverride = deathVfxDurationOverride,
                CutsceneUid = deathCutsceneUid,
                SuppressDefaultDeathAnimation = suppressDefaultDeathAnimation,
                FreezeLastFrame = freezeLastFrame,
                Priority = priority,
            };
        }
    }
}
