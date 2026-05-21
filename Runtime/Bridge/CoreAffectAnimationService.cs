using System.Collections;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 애니메이션 요청을 Core의 <see cref="CharacterBase"/> 및
    /// <see cref="ICharacterAnimationController"/>로 연결하는 구현체입니다.
    /// </summary>
    /// <remarks>
    /// - 대상별로 내부 조정자(<see cref="AffectAnimationPlayer"/>)를 두고,
    ///   우선순위/최신 요청 기준으로 하나의 Affect 애니메이션만 활성화합니다.
    /// - 낮은 우선순위 요청은 등록만 유지하고, 현재 활성 요청이 사라지면 자동 승격됩니다.
    /// </remarks>
    public sealed class CoreAffectAnimationService : IAffectAnimationService
    {
        /// <inheritdoc />
        public object Play(IAffectTarget target, AffectAnimationDefinition definition)
        {
            if (target?.Transform == null || definition == null || !definition.IsConfigured)
                return null;

            var go = target.Transform.gameObject;
            var character = go.GetComponent<CharacterBase>();
            if (character == null || character.CharacterAnimationController == null)
                return null;

            var player = go.GetComponent<AffectAnimationPlayer>();
            if (player == null)
                player = go.AddComponent<AffectAnimationPlayer>();

            return player.Register(character, definition);
        }

        /// <inheritdoc />
        public void Stop(object token)
        {
            if (token is AffectAnimationPlayer.Handle handle)
                handle.Release();
        }

        /// <summary>
        /// 캐릭터 단위 Affect 애니메이션 우선순위/전환을 조정하는 내부 플레이어입니다.
        /// </summary>
        [DisallowMultipleComponent]
        private sealed class AffectAnimationPlayer : MonoBehaviour
        {
            private readonly List<Handle> _handles = new();
            private Handle _current;
            private Coroutine _transitionCoroutine;
            private int _sequenceSeed;

            /// <summary>
            /// 개별 Affect 인스턴스가 보유하는 등록 핸들입니다.
            /// </summary>
            internal sealed class Handle
            {
                private readonly AffectAnimationPlayer _owner;

                internal readonly CharacterBase Character;
                internal readonly AffectAnimationDefinition Definition;
                internal readonly int Sequence;

                internal bool IsReleased;

                internal Handle(AffectAnimationPlayer owner, CharacterBase character, AffectAnimationDefinition definition, int sequence)
                {
                    _owner = owner;
                    Character = character;
                    Definition = definition;
                    Sequence = sequence;
                }

                internal void Release()
                {
                    if (IsReleased)
                        return;

                    IsReleased = true;
                    _owner.Unregister(this);
                }
            }

            internal Handle Register(CharacterBase character, AffectAnimationDefinition definition)
            {
                var handle = new Handle(this, character, definition, ++_sequenceSeed);
                _handles.Add(handle);
                RefreshCurrent();
                return handle;
            }

            private void Unregister(Handle handle)
            {
                if (handle == null)
                    return;

                bool wasCurrent = ReferenceEquals(handle, _current);
                _handles.Remove(handle);

                if (wasCurrent)
                {
                    PlayEndThenResume(handle);
                    return;
                }

                if (_current == null)
                    RefreshCurrent();
            }

            private void RefreshCurrent()
            {
                Handle best = ResolveBestHandle();
                if (ReferenceEquals(best, _current))
                    return;

                StopTransition();

                _current = best;
                if (_current != null)
                    Activate(_current);
            }

            private Handle ResolveBestHandle()
            {
                Handle best = null;
                for (int i = 0; i < _handles.Count; i++)
                {
                    Handle candidate = _handles[i];
                    if (candidate == null || candidate.IsReleased || candidate.Character == null)
                        continue;

                    if (best == null
                        || candidate.Definition.priority > best.Definition.priority
                        || (candidate.Definition.priority == best.Definition.priority && candidate.Sequence > best.Sequence))
                    {
                        best = candidate;
                    }
                }

                return best;
            }

            private void Activate(Handle handle)
            {
                if (handle?.Character == null || handle.Character.IsStatusDead())
                    return;

                ICharacterAnimationController animation = handle.Character.CharacterAnimationController;
                if (animation == null)
                    return;

                if (handle.Definition.stopCharacterOnApply)
                    handle.Character.Stop(true);

                string start = handle.Definition.startAnimationName;
                string loop = handle.Definition.loopAnimationName;

                bool hasStart = HasAnimation(animation, start);
                bool hasLoop = HasAnimation(animation, loop);

                if (hasStart)
                {
                    animation.PlayCharacterAnimation(start, loop: false);
                    if (hasLoop)
                    {
                        float duration = ResolveDuration(animation, start);
                        if (duration > 0f)
                            _transitionCoroutine = StartCoroutine(CoPlayLoopAfterStart(handle, duration));
                        else
                            animation.PlayCharacterAnimation(loop, loop: true);
                    }

                    return;
                }

                if (hasLoop)
                    animation.PlayCharacterAnimation(loop, loop: true);
            }

            private IEnumerator CoPlayLoopAfterStart(Handle handle, float duration)
            {
                yield return new WaitForSeconds(duration);

                _transitionCoroutine = null;

                if (!ReferenceEquals(_current, handle) || handle == null || handle.IsReleased || handle.Character == null)
                    yield break;

                ICharacterAnimationController animation = handle.Character.CharacterAnimationController;
                if (animation == null || handle.Character.IsStatusDead())
                    yield break;

                string loop = handle.Definition.loopAnimationName;
                if (HasAnimation(animation, loop))
                    animation.PlayCharacterAnimation(loop, loop: true);
            }

            /// <summary>
            /// 현재 Affect가 종료될 때 End 애니메이션을 재생하고, 필요한 경우 기본 Wait 상태로 복귀시킵니다.
            /// </summary>
            /// <param name="endedHandle">종료된 Affect 핸들입니다.</param>
            private void PlayEndThenResume(Handle endedHandle)
            {
                StopTransition();
                _current = null;

                if (ShouldSkipEndThenResume(endedHandle?.Character))
                {
                    RefreshCurrent();
                    return;
                }

                ICharacterAnimationController animation = endedHandle.Character.CharacterAnimationController;
                if (animation == null)
                {
                    RefreshCurrent();
                    return;
                }

                string endAnimation = endedHandle.Definition.endAnimationName;
                if (!HasAnimation(animation, endAnimation))
                {
                    if (ResolveBestHandle() == null
                        && endedHandle.Definition.restoreWaitOnEnd
                        && ShouldRestoreWaitAnimation(endedHandle.Character))
                    {
                        animation.PlayWaitAnimation();
                    }

                    RefreshCurrent();
                    return;
                }

                animation.PlayCharacterAnimation(endAnimation, loop: false);
                float duration = ResolveDuration(animation, endAnimation);
                if (duration > 0f)
                {
                    _transitionCoroutine = StartCoroutine(CoWaitEndAndResume(endedHandle, duration));
                    return;
                }

                if (ResolveBestHandle() == null
                    && endedHandle.Definition.restoreWaitOnEnd
                    && ShouldRestoreWaitAnimation(endedHandle.Character))
                {
                    animation.PlayWaitAnimation();
                }

                RefreshCurrent();
            }

            /// <summary>
            /// Affect 종료 시 End/Resume 처리를 건너뛰어야 하는지 판단합니다.
            /// </summary>
            /// <remarks>
            /// 사망 상태, 사망 보류 상태, Brain/Control 잠금 상태에서는 상위 시스템(사망/페이즈/컷신)의
            /// 연출 우선순위를 보장하기 위해 End 연출 진입 자체를 막습니다.
            /// </remarks>
            /// <param name="character">종료 대상 캐릭터입니다.</param>
            /// <returns>End/Resume 처리를 생략해야 하면 <see langword="true"/>를 반환합니다.</returns>
            private static bool ShouldSkipEndThenResume(CharacterBase character)
            {
                return character == null
                       || character.IsStatusDead()
                       || character.IsDeathPending
                       || character.IsBrainLocked()
                       || character.IsDontControl();
            }

            /// <summary>
            /// End 애니메이션이 끝난 뒤 기본 Wait 상태 복귀가 가능한지 확인합니다.
            /// </summary>
            /// <remarks>
            /// 페이즈 전환처럼 제어가 잠긴 상태(Brain/Control Lock)나 사망 보류 상태에서는
            /// Wait 애니메이션 복귀를 막아 상위 연출 흐름을 보존합니다.
            /// </remarks>
            /// <param name="character">복귀 대상 캐릭터입니다.</param>
            /// <returns>Wait 애니메이션 복귀가 가능하면 <see langword="true"/>를 반환합니다.</returns>
            private static bool ShouldRestoreWaitAnimation(CharacterBase character)
            {
                return character != null
                       && !character.IsStatusDead()
                       && !character.IsDeathPending
                       && !character.IsBrainLocked()
                       && !character.IsDontControl();
            }

            /// <summary>
            /// End 애니메이션 재생 후 대기 시간을 지난 다음 Wait 복귀 및 다음 Affect 재평가를 수행합니다.
            /// </summary>
            /// <param name="endedHandle">종료된 Affect 핸들입니다.</param>
            /// <param name="duration">End 애니메이션 대기 시간(초)입니다.</param>
            private IEnumerator CoWaitEndAndResume(Handle endedHandle, float duration)
            {
                yield return new WaitForSeconds(duration);

                _transitionCoroutine = null;

                if (endedHandle?.Character != null
                    && ResolveBestHandle() == null
                    && endedHandle.Definition.restoreWaitOnEnd
                    && ShouldRestoreWaitAnimation(endedHandle.Character))
                {
                    endedHandle.Character.CharacterAnimationController?.PlayWaitAnimation();
                }

                RefreshCurrent();
            }

            private void StopTransition()
            {
                if (_transitionCoroutine == null)
                    return;

                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            private static bool HasAnimation(ICharacterAnimationController animation, string animationName)
            {
                return animation != null
                       && !string.IsNullOrWhiteSpace(animationName)
                       && animation.HasAnimation(animationName);
            }

            private static float ResolveDuration(ICharacterAnimationController animation, string animationName)
            {
                if (!HasAnimation(animation, animationName))
                    return 0f;

                float duration = animation.GetCharacterAnimationDuration(animationName, isMilliseconds: false);
                return duration > 0f ? duration : 0f;
            }

            private void OnDisable()
            {
                StopTransition();
                _current = null;
                _handles.Clear();
            }

            private void OnDestroy()
            {
                StopTransition();
                _current = null;
                _handles.Clear();
            }
        }
    }
}
