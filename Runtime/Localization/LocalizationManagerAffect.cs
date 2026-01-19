using System.Collections;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 패키지 전용 Localization 매니저입니다.
    /// </summary>
    /// <remarks>
    /// - 런타임에서 단일 인스턴스로 유지(DontDestroyOnLoad)됩니다.
    /// - 기본(Base) 테이블 및 사용자(User) 테이블의 존재 여부를 점검/캐시합니다.
    /// - Affect에서 자주 사용하는 테이블 접근 헬퍼 메서드를 제공합니다.
    /// </remarks>
    public class LocalizationManagerAffect : LocalizationManagerBase
    {
        /// <summary>
        /// 현재 활성화된 <see cref="LocalizationManagerAffect"/> 인스턴스입니다.
        /// </summary>
        public static LocalizationManagerAffect Instance { get; private set; }

        /// <summary>
        /// 사용자(User) 테이블 존재 여부 캐시입니다.
        /// </summary>
        /// <remarks>
        /// Key: 베이스 테이블 이름(예: "GGemCo_Affect_Description") / Value: "{BaseTable}_User" 테이블 존재 여부
        /// </remarks>
        private readonly Dictionary<string, bool> _userTableExistsMap = new();

        /// <summary>
        /// 싱글톤 인스턴스를 설정하고, 씬 전환 시에도 유지되도록 합니다.
        /// </summary>
        /// <remarks>
        /// 이미 인스턴스가 존재하는 경우 중복 인스턴스를 제거합니다.
        /// </remarks>
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
        /// - 존재 여부를 <see cref="_userTableExistsMap"/>에 캐시합니다. (키는 baseTable 기준)
        ///
        /// NOTE:
        /// - 여기서는 테이블을 실제로 "사용"하는 것이 아니라 존재 여부만 확인합니다.
        /// - GetTableAsync 핸들은 Addressables로 로드될 수 있으므로, 유효한 경우 Release 합니다.
        /// </remarks>
        /// <returns>테이블 존재 여부 검사를 수행하는 코루틴 열거자입니다.</returns>
        protected override IEnumerator CheckUserTablesExist()
        {
            foreach (string baseTable in LocalizationConstantsAffect.Tables.All)
            {
                string userTableName = $"{baseTable}_User";

                // 선택된 로케일에 대해 사용자 테이블을 비동기로 조회합니다.
                AsyncOperationHandle<StringTable> handle = stringDatabase.GetTableAsync(userTableName, LocalizationSettings.SelectedLocale);
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

        /// <summary>
        /// Affect 이름 테이블에서 키에 해당하는 문자열을 조회합니다.
        /// </summary>
        /// <param name="key">Localization Key 입니다.</param>
        /// <returns>키에 해당하는 로컬라이즈된 문자열이며, 미존재 시 구현체의 기본 동작을 따릅니다.</returns>
        public string GetAffectNameByKey(string key) => GetString(LocalizationConstantsAffect.Tables.AffectName, key);

        /// <summary>
        /// Affect 설명 테이블에서 키에 해당하는 문자열을 조회합니다.
        /// </summary>
        /// <param name="key">Localization Key 입니다.</param>
        /// <returns>키에 해당하는 로컬라이즈된 문자열이며, 미존재 시 구현체의 기본 동작을 따릅니다.</returns>
        public string GetAffectDescriptionByKey(string key) =>
            GetString(LocalizationConstantsAffect.Tables.AffectDescription, key);

        /// <summary>
        /// Affect 설명 테이블에서 Smart String(서식/변수 치환)을 사용하여 문자열을 조회합니다.
        /// </summary>
        /// <param name="key">Localization Key 입니다.</param>
        /// <param name="args">Smart String에 바인딩할 인자 목록입니다.</param>
        /// <returns>Smart String 규칙에 따라 포맷된 로컬라이즈 문자열입니다.</returns>
        public string GetAffectDescriptionSmart(string key, params object[] args) =>
            GetSmartString(LocalizationConstantsAffect.Tables.AffectDescription, key, args);

        /// <summary>
        /// Affect 설명 테이블에 지정한 키가 존재하는지 여부를 반환합니다.
        /// </summary>
        /// <param name="key">존재 여부를 확인할 Localization Key 입니다.</param>
        /// <returns>키가 존재하면 true, 아니면 false를 반환합니다.</returns>
        public bool HasAffectDescriptionLocalizationKey(string key)
        {
            return HasLocalizationKey(LocalizationConstantsAffect.Tables.AffectDescription, key);
        }

        /// <summary>
        /// 스택 정책(<see cref="StackPolicy"/>)에 해당하는 표시 이름을 반환합니다.
        /// </summary>
        /// <param name="policy">표시 이름으로 변환할 스택 정책입니다.</param>
        /// <returns>
        /// 정책이 <see cref="StackPolicy.None"/>이면 빈 문자열을 반환하고,
        /// 그 외에는 정책 이름(열거형 ToString)을 키로 하여 로컬라이즈 문자열을 반환합니다.
        /// </returns>
        public string GetStackPolicyName(StackPolicy policy)
        {
            if (policy == StackPolicy.None)
                return string.Empty;

            var key = policy.ToString();
            return GetString(LocalizationConstantsAffect.Tables.AffectStackPolicy, key);
        }
    }
}
