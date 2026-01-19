using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Control 패키지의 메인 메니저
    /// SceneGame 과 같은 개념
    /// </summary>
    [DefaultExecutionOrder((int)ConfigCommon.ExecutionOrdering.Affect)]
    public class AffectPackageManager : MonoBehaviour
    {
        public static AffectPackageManager Instance { get; private set; }
        
        private void Awake()
        {
            // 게임씬이 로드 되지 않았다면 return;
            if (TableLoaderManager.Instance == null)
            {
                return;
            }
            // 게임 신 싱글톤으로 사용하기.
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

        private void Start()
        {
            if (SceneGame.Instance)
                SceneGame.Instance.OnSceneGameDestroyed += OnDestroyBySceneGame;
        }

        private void OnDestroyBySceneGame()
        {
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if (SceneGame.Instance) 
                SceneGame.Instance.OnSceneGameDestroyed -= OnDestroyBySceneGame;
        }
    }
}