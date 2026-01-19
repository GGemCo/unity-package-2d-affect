using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지의 런타임 부트스트랩(테이블/로컬라이징/리소스/정의 로딩)을
    /// 로딩 씬 단계에 등록하는 씬 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// - Addressables 설정이 준비되지 않은 경우 PreIntro 씬으로 되돌립니다.
    /// - 로딩 씬에서 GameLoaderManager의 훅 이벤트를 통해 로딩 스텝을 등록합니다.
    /// - 필요한 매니저/로더가 씬에 없으면 런타임에 생성하여 사용합니다.
    /// </remarks>
    public class SceneLoadingAffect : DefaultScene
    {
        // NOTE: 현재 코드에서는 사용되지 않지만, 향후 확장(참조 보관/제어)을 위해 남겨둔 필드일 수 있습니다.
        private GameLoaderManager _gameLoaderManager;

        /// <summary>
        /// Addressables 로더 설정이 존재하지 않으면 PreIntro 씬으로 강제 이동합니다.
        /// </summary>
        private void Awake()
        {
            if (!AddressableLoaderSettings.Instance)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNamePreIntro);
                return;
            }
        }

        /// <summary>
        /// 오브젝트 활성화 시 로딩 시작 직전 이벤트 훅을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            // PreIntro 씬/Loading 씬에서 로딩 시작 직전 훅
            GameLoaderManager.BeforeLoadStartInLoadingScene += OnBeforeLoadStartInLoadingScene;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            GameLoaderManager.BeforeLoadStartInLoadingScene -= OnBeforeLoadStartInLoadingScene;
        }

        /// <summary>
        /// 로딩 씬에서 실제 로딩이 시작되기 직전에 호출되며,
        /// Affect 관련 로딩 스텝(테이블/로컬라이징/리소스/정의)을 등록합니다.
        /// </summary>
        /// <param name="sender">로딩 스텝을 등록할 <see cref="GameLoaderManager"/>입니다.</param>
        /// <param name="e">로딩 시작 직전 이벤트 인자입니다.</param>
        private void OnBeforeLoadStartInLoadingScene(
            GameLoaderManager sender,
            GameLoaderManager.EventArgsBeforeLoadStart e)
        {
            // 테이블 로더 준비 및 테이블 로딩 스텝 등록
            var tableLoader =
                FindFirstObjectByType<TableLoaderManagerAffect>() ??
                new GameObject("TableLoaderManagerAffect").AddComponent<TableLoaderManagerAffect>();

            var targetTables = ConfigAddressableTableAffect.All;
            var stepTable = new TableLoadStep(
                id: "core.table.affect",
                order: 246,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeTables(),
                tableLoader: tableLoader,
                tables: targetTables
            );
            sender.Register(stepTable);

            // 로컬라이징 매니저 준비 및 로컬라이징 로딩 스텝 등록
            var loc =
                Object.FindFirstObjectByType<LocalizationManagerAffect>() ??
                new GameObject("LocalizationManagerAffect").AddComponent<LocalizationManagerAffect>();

            var stepLocalization = new LocalizationLoadStep(
                "core.localization.affect",
                order: 221,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeLocalization(),
                localizationManager: loc,
                localeCode: PlayerPrefsManager.LoadLocalizationLocaleCode()
            );
            sender.Register(stepLocalization);

            // 어펙트 이미지(아틀라스 등) 로딩 스텝 등록
            var addrAffect = Object.FindFirstObjectByType<AddressableLoaderAffect>() ??
                             new GameObject("AddressableLoaderAffect").AddComponent<AddressableLoaderAffect>();

            var stepAffect = new AddressableTaskStep(
                id: "core.image.icon.affect",
                order: 340,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeAffect(),
                startTask: () => addrAffect.LoadAtlasesAsync(),
                getProgress: () => addrAffect.GetPrefabLoadProgress()
            );
            sender.Register(stepAffect);

            // Affect Definition 런타임 부트스트랩(정의/레지스트리 준비 등) 스텝 등록
            var stepAffectDefinition = new AffectRuntimeBootstrapStep(
                id: "core.definition.affect",
                order: 341,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeAffect()
            );
            sender.Register(stepAffectDefinition);
        }
    }
}
