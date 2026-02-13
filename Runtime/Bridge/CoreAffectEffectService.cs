using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 시스템의 Effect 요청을 Core의 <see cref="EffectManager"/>로 연결하는 구현체.
    /// </summary>
    /// <remarks>
    /// - Affect는 Core에만 의존하며, Core는 Affect를 참조하지 않는다.
    /// - 실제 Effect 생성/재생은 Core의 <see cref="SceneGame.EffectManager"/>를 통해 수행한다.
    /// </remarks>
    public sealed class CoreAffectEffectService : IAffectEffectService
    {
        /// <inheritdoc />
        public object Play(
            int vfxUid,
            IAffectTarget target,
            float scale,
            float offsetY,
            float duration,
            AffectEffectPositionType positionType,
            AffectEffectFollowType followType,
            ConfigSortingLayer.Keys sortingLayerKey)
        {
            if (vfxUid <= 0 || target == null) return null;

            var scene = SceneGame.Instance;
            if (scene == null || scene.EffectManager == null) return null;

            var effect = scene.EffectManager.CreateEffect(vfxUid);
            if (effect == null) return null;

            // 기본 파라미터
            if (duration > 0f) effect.SetDuration(duration);
            if (scale > 0f) effect.SetScale(scale);

            // SortingLayer
            effect.SetSortingLayer(sortingLayerKey);

            // 타겟 캐릭터(있으면 flip/height 계산에 활용)
            CharacterBase character = null;
            var tr = target.Transform;
            if (tr != null)
            {
                character = tr.GetComponent<CharacterBase>();
                if (character != null)
                    effect.SetCreateCharacter(character);
                else
                    effect.transform.position = tr.position;
            }

            // 위치/Follow
            bool isFollow = followType == AffectEffectFollowType.Follow;
            bool isHead = positionType == AffectEffectPositionType.Head;

            if (isFollow)
            {
                if (character != null)
                {
                    effect.SetFollowCharacter(character);
                    effect.SetPositionY(offsetY);
                    effect.SetPositionYType(isHead ? ConfigCommon.PositionYType.CharacterHeight : ConfigCommon.PositionYType.None);
                }
                else
                {
                    // 캐릭터가 없으면 Follow 불가: 1회 위치에만 표시
                    effect.transform.position = ComputeOneShotPosition(tr, null, isHead, offsetY);
                }
            }
            else
            {
                effect.transform.position = ComputeOneShotPosition(tr, character, isHead, offsetY);
            }

            return effect;
        }

        /// <inheritdoc />
        public void Stop(object token)
        {
            if (token is DefaultEffect eff && eff != null)
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
