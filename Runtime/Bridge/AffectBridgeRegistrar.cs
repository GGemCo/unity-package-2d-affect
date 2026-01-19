using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지가 로드될 때 Core의 브리지(AffectBridge)에 Provider를 자동 등록합니다.
    /// </summary>
    /// <remarks>
    /// Affect 미설치 시 이 클래스 자체가 포함되지 않으므로, Core는 기본(Null Provider) 구현으로 동작합니다.
    /// 또한 RuntimeInitializeOnLoadMethod를 사용하여 씬 로드 이전(BeforeSceneLoad)에 등록을 보장합니다.
    /// </remarks>
    internal static class AffectBridgeRegistrar
    {
        /// <summary>
        /// 플레이/빌드 환경에서 씬 로드 전에 AffectDescriptionProvider를 Core 브리지에 주입합니다.
        /// </summary>
        /// <remarks>
        /// Core 측에서 Affect 기능을 직접 참조하지 않도록(의존성 역전) 브리지 패턴을 사용하며,
        /// 이 메서드는 Affect 패키지가 존재하는 경우에만 실행됩니다.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            // 플레이/빌드 모두에서 안전하게 등록
            AffectBridge.SetProvider(new AffectDescriptionProvider());
        }
    }
}