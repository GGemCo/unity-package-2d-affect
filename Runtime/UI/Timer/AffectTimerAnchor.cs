using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 타이머 UI가 따라다닐 월드 기준 위치를 제공합니다.
    /// </summary>
    /// <remarks>
    /// 캐릭터 피벗이 발밑, 중앙, 좌하단 등으로 다를 수 있으므로,
    /// 프리팹에서 머리 위 Transform을 명시하고 싶을 때 사용합니다.
    /// </remarks>
    public sealed class AffectTimerAnchor : MonoBehaviour
    {
        [Tooltip("타이머 UI가 따라다닐 기준 Transform입니다. 비어 있으면 현재 Transform을 사용합니다.")]
        [SerializeField] private Transform anchor;

        [Tooltip("기준 Transform에서 추가로 더할 월드 오프셋입니다.")]
        [SerializeField] private Vector3 worldOffset = new(0f, 1.6f, 0f);

        /// <summary>
        /// UI를 배치할 월드 좌표를 반환합니다.
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            Transform basis = anchor != null ? anchor : transform;
            return basis.position + worldOffset;
        }
    }
}
