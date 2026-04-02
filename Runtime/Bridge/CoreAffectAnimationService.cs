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

            private void PlayEndThenResume(Handle endedHandle)
            {
                StopTransition();
                _current = null;

                if (endedHandle?.Character == null || endedHandle.Character.IsStatusDead())
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
                    if (ResolveBestHandle() == null && endedHandle.Definition.restoreWaitOnEnd && !endedHandle.Character.IsStatusDead())
                        animation.PlayWaitAnimation();

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

                if (ResolveBestHandle() == null && endedHandle.Definition.restoreWaitOnEnd && !endedHandle.Character.IsStatusDead())
                    animation.PlayWaitAnimation();

                RefreshCurrent();
            }

            private IEnumerator CoWaitEndAndResume(Handle endedHandle, float duration)
            {
                yield return new WaitForSeconds(duration);

                _transitionCoroutine = null;

                if (endedHandle?.Character != null
                    && !endedHandle.Character.IsStatusDead()
                    && ResolveBestHandle() == null
                    && endedHandle.Definition.restoreWaitOnEnd)
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
