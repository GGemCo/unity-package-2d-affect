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

            var vfx = scene.VfxManager.CreateVfx(vfxUid);
            if (vfx == null) return null;

            // 기본 파라미터
            // NOTE:
            // - LoopDuringAffectDuration: duration을 그대로 전달(0이면 기본 규칙, 음수는 무제한 loop)
            // - Once: duration을 0으로 취급하여 1회 재생
            float resolvedDuration = playMode == AffectVfxPlayMode.Once ? 0f : duration;
            if (resolvedDuration != 0f) vfx.SetDuration(resolvedDuration);
            if (scale > 0f) vfx.SetScale(scale);

            // SortingLayer
            vfx.SetSortingLayer(sortingLayerKey);

            // 타겟 캐릭터(있으면 flip/height 계산에 활용)
            CharacterBase character = null;
            var tr = target.Transform;
            if (tr != null)
            {
                character = tr.GetComponent<CharacterBase>();
                if (character != null)
                    vfx.SetCreateCharacter(character);
                else
                    vfx.transform.position = tr.position;
            }

            // 위치/Follow
            bool isFollow = followType == AffectVfxFollowType.Follow;
            bool isHead = positionType == AffectVfxPositionType.Head;

            if (isFollow)
            {
                if (character != null)
                {
                    vfx.SetFollowCharacter(character);
                    vfx.SetPositionY(offsetY);
                    vfx.SetPositionYType(isHead ? ConfigCommon.PositionYType.CharacterHeight : ConfigCommon.PositionYType.None);
                }
                else
                {
                    // 캐릭터가 없으면 Follow 불가: 1회 위치에만 표시
                    vfx.transform.position = ComputeOneShotPosition(tr, null, isHead, offsetY);
                }
            }
            else
            {
                vfx.transform.position = ComputeOneShotPosition(tr, character, isHead, offsetY);
            }

            return vfx;
        }

        /// <inheritdoc />
        public void Stop(object token)
        {
            if (token is DefaultVfx eff && eff != null)
            {
                eff.DestroyForce();
            }
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
