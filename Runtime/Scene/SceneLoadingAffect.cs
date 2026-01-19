using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    public class SceneLoadingAffect : DefaultScene
    {
        private GameLoaderManager _gameLoaderManager;

        private void Awake()
        {
            if (!AddressableLoaderSettings.Instance)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNamePreIntro);
                return;
            }
        }

        private void OnEnable()
        {
            // PreIntro 씬/Loading 씬에서 로딩 시작 직전 훅
            GameLoaderManager.BeforeLoadStartInLoadingScene += OnBeforeLoadStartInLoadingScene;
        }

        /// <summary>
        /// 오브젝트 비활성화 시, 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            GameLoaderManager.BeforeLoadStartInLoadingScene -= OnBeforeLoadStartInLoadingScene;
        }

        private void OnBeforeLoadStartInLoadingScene(GameLoaderManager sender, GameLoaderManager.EventArgsBeforeLoadStart e)
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

            // 어펙트 이미지
            var addrAffect = Object.FindFirstObjectByType<AddressableLoaderAffect>() ??
                             new GameObject("AddressableLoaderAffect").AddComponent<AddressableLoaderAffect>();
            var stepAffect = new AddressableTaskStep(
                id: "core.image.icon.affect",
                order: 340,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeAffect(),
                startTask: () => addrAffect.LoadPrefabsAsync(),
                getProgress: () => addrAffect.GetPrefabLoadProgress()
            );
            sender.Register(stepAffect);
            
            // Affect Definition
            var stepAffectDefinition = new AffectRuntimeBootstrapStep(
                id: "core.definition.affect",
                order: 341,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeAffect()
            );
            sender.Register(stepAffectDefinition);
        }
    }
}
