using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 도메인에서 사용하는 Addressables 테이블 리소스 정의를 중앙에서 관리한다.
    /// </summary>
    /// <remarks>
    /// - 테이블 이름 문자열과 AddressableAssetInfo 생성을 단일 소스(Single Source of Truth)로 유지한다.
    /// - 로딩/초기화 단계에서 필요한 테이블 집합을 <see cref="All"/>로 일괄 참조할 수 있다.
    /// </remarks>
    public static class ConfigAddressableTableAffect
    {
        /// <summary>
        /// Affect 기본 정의 테이블의 논리적 이름.
        /// </summary>
        public const string Affect = "affect";

        /// <summary>
        /// Affect Modifier(효과 수식/보정) 정의 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifier = "affect_modifier";

        /// <summary>
        /// Affect 기본 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffect =
            ConfigAddressableTable.Make(Affect);

        /// <summary>
        /// Affect Modifier 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifier =
            ConfigAddressableTable.Make(AffectModifier);

        /// <summary>
        /// Affect 도메인에서 사용하는 모든 테이블 Addressables 자산 목록.
        /// </summary>
        /// <remarks>
        /// - 초기 로딩 단계에서 일괄 로드/검증 용도로 사용한다.
        /// - 테이블이 추가되면 반드시 이 목록에 함께 등록한다.
        /// </remarks>
        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableAffect,
            TableAffectModifier,
        };
    }
}