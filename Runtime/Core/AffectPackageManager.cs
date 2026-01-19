using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect(Control) 패키지의 메인 매니저로, 게임 씬 단위에서 Affect 시스템의 수명을 관리한다.
    /// </summary>
    /// <remarks>
    /// - 개념적으로 <c>SceneGame</c>과 유사한 역할을 하며, Affect 패키지의 런타임 진입점이다.
    /// - 게임 씬이 유효할 때만 활성화되며, 씬 종료 시 함께 정리된다.
    /// - 싱글톤 패턴을 사용하되, 중복 생성 시 기존 인스턴스를 유지한다.
    /// </remarks>
    [DefaultExecutionOrder((int)ConfigCommon.ExecutionOrdering.Affect)]
    public class AffectPackageManager : MonoBehaviour
    {
        /// <summary>
        /// Affect 패키지 매니저의 전역 싱글톤 인스턴스.
        /// </summary>
        public static AffectPackageManager Instance { get; private set; }

        /// <summary>
        /// 초기화 단계에서 게임 씬 유효성 검사 및 싱글톤 설정을 수행한다.
        /// </summary>
        /// <remarks>
        /// - <see cref="TableLoaderManager"/>가 없으면 게임 씬이 아니라고 판단하고 초기화를 중단한다.
        /// - 최초 생성된 인스턴스는 씬 전환 시 유지되도록 설정한다.
        /// - 중복 인스턴스가 생성되면 즉시 파괴한다.
        /// </remarks>
        private void Awake()
        {
            // 게임 씬이 로드되지 않았다면 초기화하지 않는다.
            if (TableLoaderManager.Instance == null)
            {
                return;
            }

            // 게임 씬 싱글톤으로 사용한다.
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 게임 씬 라이프사이클 이벤트를 구독한다.
        /// </summary>
        /// <remarks>
        /// SceneGame이 파괴될 때 Affect 패키지도 함께 정리되도록 연결한다.
        /// </remarks>
        private void Start()
        {
            if (SceneGame.Instance)
                SceneGame.Instance.OnSceneGameDestroyed += OnDestroyBySceneGame;
        }

        /// <summary>
        /// SceneGame 종료 이벤트에 의해 호출되며, 자신을 파괴한다.
        /// </summary>
        private void OnDestroyBySceneGame()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// 오브젝트 파괴 시 SceneGame 이벤트 구독을 해제한다.
        /// </summary>
        /// <remarks>
        /// 이벤트 누수 및 중복 호출을 방지하기 위한 정리 로직이다.
        /// </remarks>
        private void OnDestroy()
        {
            if (SceneGame.Instance)
                SceneGame.Instance.OnSceneGameDestroyed -= OnDestroyBySceneGame;
        }
    }
}
