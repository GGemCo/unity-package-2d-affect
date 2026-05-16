using TMPro;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// TMP_Text 기반 어펙트 남은 시간 표시 View입니다.
    /// </summary>
    /// <remarks>
    /// 실제 시간 계산과 필터링은 Presenter/Manager가 담당하고,
    /// 이 클래스는 텍스트와 위치 같은 화면 표시만 처리합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AffectTimerTextView : MonoBehaviour
    {
        [Tooltip("남은 시간 표시용 TMP_Text입니다. 비어 있으면 자식 또는 자기 자신에서 자동 탐색합니다.")]
        [SerializeField] private TMP_Text textTimer;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform ?? GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        /// <summary>
        /// View 내부 참조를 초기화합니다.
        /// </summary>
        private void Awake()
        {
            CacheComponents();
        }

        /// <summary>
        /// 풀링 생성 직후 외부에서 안전하게 참조를 준비할 수 있도록 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            CacheComponents();
            SetVisible(false);
        }

        /// <summary>
        /// 표시 문자열과 기본 스타일을 적용합니다.
        /// </summary>
        /// <param name="text">표시할 문자열입니다.</param>
        /// <param name="color">텍스트 색상입니다.</param>
        /// <param name="fontSize">폰트 크기입니다. 0 이하이면 기존 값을 유지합니다.</param>
        public void SetText(string text, Color color, float fontSize)
        {
            CacheComponents();
            if (textTimer == null)
                return;

            textTimer.text = text ?? string.Empty;
            textTimer.color = color;
            if (fontSize > 0f)
                textTimer.fontSize = fontSize;
        }

        /// <summary>
        /// Canvas 로컬 좌표 기준 위치를 적용합니다.
        /// </summary>
        /// <param name="anchoredPosition">Canvas 내부 anchoredPosition 값입니다.</param>
        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            var rect = RectTransform;
            if (rect != null)
                rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// View 표시 여부를 설정합니다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// 풀로 회수되기 전 표시 상태를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            if (textTimer != null)
                textTimer.text = string.Empty;
            SetVisible(false);
        }

        /// <summary>
        /// 필요한 컴포넌트 참조를 캐시합니다.
        /// </summary>
        private void CacheComponents()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform ?? GetComponent<RectTransform>();

            if (textTimer == null)
                textTimer = GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>(true);

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
