using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지가 로드될 때 Core의 브리지에 Provider를 자동 등록합니다.
    /// Affect 미설치 시 이 코드 자체가 존재하지 않으므로 Core는 Null Provider로 동작합니다.
    /// </summary>
    internal static class AffectBridgeRegistrar
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            // 플레이/빌드 모두에서 안전하게 등록
            AffectBridge.SetProvider(new AffectDescriptionProvider());
        }
    }
}