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
            if (vfxUid <= 0 || target == null)
                return null;

            var scene = SceneGame.Instance;
            if (scene == null || scene.VfxManager == null)
                return null;

            var tr = target.Transform;
            CharacterBase character = tr != null ? tr.GetComponent<CharacterBase>() : null;

            bool isOnce = playMode == AffectVfxPlayMode.Once;
            bool isFollow = followType == AffectVfxFollowType.Follow;
            bool isHead = positionType == AffectVfxPositionType.Head;

            float resolvedDuration = isOnce ? 0f : duration;

            var request = new VfxSpawnRequest
            {
                VfxUid = vfxUid,
                DurationOverride = resolvedDuration,
                ForceOneShot = isOnce,
                ScaleOverride = scale,
                SortingLayerOverride = sortingLayerKey,
                PositionY = offsetY,
                PositionYType = isHead ? ConfigCommon.PositionYType.CharacterHeight : ConfigCommon.PositionYType.None,
            };

            if (isOnce)
                request.LifecycleTypeOverride = VfxConstants.LifecycleType.AutoRelease;

            if (isFollow && character != null)
            {
                request.FollowTarget = character;
            }
            else
            {
                request.WorldPosition = ComputeOneShotPosition(tr, character, isHead, offsetY);
                request.PositionY = 0f;
                request.PositionYType = ConfigCommon.PositionYType.None;
            }

            return scene.VfxManager.CreateVfx(request);
        }

        public void Stop(object token)
        {
            if (token is not VfxBehaviourBase vfx || vfx == null)
                return;

            var go = vfx.gameObject;
            if (go == null)
                return;

            if (!go.activeInHierarchy)
                return;

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
