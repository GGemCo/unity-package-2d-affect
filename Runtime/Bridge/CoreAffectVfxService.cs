using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 시스템의 Vfx 요청을 Core의 <see cref="VfxManager"/>로 연결하는 구현체.
    /// </summary>
    /// <remarks>
    /// - Affect는 Core에만 의존하며, Core는 Affect를 참조하지 않는다.
    /// - 실제 Vfx 생성/재생은 Core의 <see cref="SceneGame.VfxManager"/>를 통해 수행한다.
    /// </remarks>
    public sealed class CoreAffectVfxService : IAffectVfxService
    {
        /// <inheritdoc />
        public object Play(
            int vfxUid,
            IAffectTarget target,
            float scale,
            float offsetY,
            float duration,
            AffectVfxPlayMode playMode,
            AffectVfxPositionType positionType,
            AffectVfxFollowType followType,
            ConfigSortingLayer.Keys sortingLayerKey)
        {
            if (vfxUid <= 0 || target == null) return null;

            var scene = SceneGame.Instance;
            if (scene == null || scene.VfxManager == null) return null;

            // NOTE:
            // - LoopDuringAffectDuration: duration을 그대로 전달(0이면 기본 규칙, 음수는 무제한 loop)
            // - Once: duration을 0으로 취급하여 1회 재생
            float resolvedDuration = playMode == AffectVfxPlayMode.Once ? 0f : duration;

            // 중요:
            // VfxBehaviourBase.OnEnable() -> PlayOnSpawn() 에서 _duration 을 즉시 사용하므로,
            // duration/scale/sorting/follow 관련 값은 CreateVfx 이후 후처리로 넣으면 첫 재생에 반영되지 않는다.
            // 따라서 생성 전에 VfxSpawnRequest 로 전달해서 ApplyRequest 단계에서 먼저 세팅되도록 한다.
            var request = new VfxSpawnRequest
            {
                VfxUid = vfxUid,
                DurationOverride = resolvedDuration,
                ScaleOverride = scale > 0f ? scale : 0f,
                SortingLayerOverride = sortingLayerKey,
            };

            // 타겟 캐릭터(있으면 flip/height 계산에 활용)
            CharacterBase character = null;
            var tr = target.Transform;
            if (tr != null)
            {
                character = tr.GetComponent<CharacterBase>();
                if (character != null)
                {
                    request.Owner = character;
                }
                else
                {
                    request.WorldPosition = tr.position;
                }
            }

            // 위치/Follow
            bool isFollow = followType == AffectVfxFollowType.Follow;
            bool isHead = positionType == AffectVfxPositionType.Head;

            if (isFollow)
            {
                if (character != null)
                {
                    request.FollowTarget = character;
                    request.PositionY = offsetY;
                    request.PositionYType = isHead ? ConfigCommon.PositionYType.CharacterHeight : ConfigCommon.PositionYType.None;
                }
                else
                {
                    // 캐릭터가 없으면 Follow 불가: 1회 위치에만 표시
                    request.WorldPosition = ComputeOneShotPosition(tr, null, isHead, offsetY);
                }
            }
            else
            {
                request.WorldPosition = ComputeOneShotPosition(tr, character, isHead, offsetY);
            }

            var vfx = scene.VfxManager.CreateVfx(request);
            if (vfx == null) return null;

            return vfx;
        }

        /// <inheritdoc />
        public void Stop(object token)
        {
            if (token is VfxBehaviourBase vfx && vfx != null)
                vfx.DestroyForce();
        }

        private static Vector3 ComputeOneShotPosition(Transform targetTr, CharacterBase character, bool isHead, float offsetY)
        {
            var pos = targetTr != null ? targetTr.position : Vector3.zero;
            if (isHead && character != null)
                pos += new Vector3(0f, character.GetHeightByScale(), 0f);
            if (Mathf.Abs(offsetY) > 0f)
                pos += new Vector3(0f, offsetY, 0f);
            return pos;
        }
    }
}
