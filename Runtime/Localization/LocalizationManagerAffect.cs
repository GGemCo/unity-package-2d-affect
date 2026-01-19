using System.Collections;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace GGemCo2DAffect
{
    /// <summary>
    /// TCG 패키지 전용 Localization 매니저입니다.
    /// <para>
    /// - 런타임에서 단일 인스턴스로 유지(DontDestroyOnLoad)되며,<br/>
    /// - 기본 테이블과 사용자(User) 테이블의 존재 여부를 점검/캐시하고,<br/>
    /// - TCG에서 자주 사용하는 테이블 접근 헬퍼를 제공합니다.
    /// </para>
    /// </summary>
    public class LocalizationManagerAffect : LocalizationManagerBase
    {
        /// <summary>
        /// 현재 활성화된 <see cref="LocalizationManagerAffect"/> 인스턴스입니다.
        /// </summary>
        public static LocalizationManagerAffect Instance;

        /// <summary>
        /// 사용자(User) 테이블 존재 여부 캐시입니다.
        /// Key: 베이스 테이블 이름(예: "SDK_UIWindowTcgCardInfo") / Value: "{BaseTable}_User" 테이블 존재 여부
        /// </summary>
        private readonly Dictionary<string, bool> _userTableExistsMap = new();

        /// <summary>
        /// 싱글톤 인스턴스를 설정하고 씬 전환 시 유지되도록 합니다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 중복 인스턴스 방지
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 선택된 로케일(<see cref="LocalizationSettings.SelectedLocale"/>) 기준으로,
        /// 각 베이스 테이블에 대응하는 사용자(User) 테이블이 존재하는지 검사합니다.
        /// </summary>
        /// <remarks>
        /// 규칙:
        /// - 베이스 테이블: <see cref="LocalizationConstantsAffect.Tables.All"/>에 정의된 테이블
        /// - 사용자 테이블: "{BaseTable}_User"
        ///
        /// 결과:
        /// - 존재 여부를 <see cref="_userTableExistsMap"/>에 캐시합니다.
        ///
        /// NOTE:
        /// - 여기서는 테이블을 실제로 "사용"하는 것이 아니라 존재 여부만 확인합니다.
        /// - GetTableAsync 핸들은 Addressables로 로드될 수 있으므로, 유효한 경우 Release 합니다.
        /// </remarks>
        /// <returns>코루틴 열거자입니다.</returns>
        protected override IEnumerator CheckUserTablesExist()
        {
            foreach (string baseTable in LocalizationConstantsAffect.Tables.All)
            {
                string userTableName = $"{baseTable}_User";

                // 선택된 로케일에 대해 사용자 테이블을 비동기로 조회합니다.
                var handle = stringDatabase.GetTableAsync(userTableName, LocalizationSettings.SelectedLocale);
                yield return handle;

                bool exists = false;

                if (handle.IsValid())
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    {
                        exists = true;
                        // GcLogger.Log($"table: {userTableName} / exist: true");
                    }
                    else
                    {
                        // GcLogger.Log($"table: {userTableName} / exist: false");
                    }
                }
                else
                {
                    // 핸들이 유효하지 않은 경우(로딩 실패/잘못된 참조 등)
                    GcLogger.LogWarning($"Invalid handle for table: {userTableName}");
                }

                // baseTable을 키로 캐시합니다. (userTableName이 아니라 baseTable 기준으로 관리)
                _userTableExistsMap[baseTable] = exists;

                // Addressables 기반 핸들인 경우 리소스 참조를 해제합니다.
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        public string GetAffectDescriptionByKey(string key) =>
            GetString(LocalizationConstants.Tables.AffectDescription, key);

        public string GetAffectDescriptionSmart(string key, params object[] args) =>
            GetSmartString(LocalizationConstants.Tables.AffectDescription, key, args);
        
        public bool HasAffectDescriptionLocalizationKey(string key)
        {
            return HasLocalizationKey(LocalizationConstants.Tables.AffectDescription, key);
        }
        public string GetStackPolicyName(StackPolicy policy)
        {
            if (policy == StackPolicy.None)
                return string.Empty;

            var key = policy.ToString();
            return GetString(LocalizationConstants.Tables.AffectStackPolicy, key);
        }
    }
}
