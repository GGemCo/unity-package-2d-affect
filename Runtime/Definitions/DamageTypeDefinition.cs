using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 피해 타입(Damage Type)의 메타데이터 정의.
    /// </summary>
    /// <remarks>
    /// - 테이블에서 로드된 피해 타입 정보를 런타임에서 참조하기 위한 순수 데이터 객체(DTO)이다.
    /// - 주로 UI 표시(이름/아이콘) 및 로그, 분류 용도로 사용된다.
    /// - 실제 피해 계산 로직은 별도의 시스템에서 처리된다.
    /// </remarks>
    [Serializable]
    public sealed class DamageTypeDefinition
    {
        /// <summary>
        /// 피해 타입의 고유 식별자.
        /// </summary>
        /// <remarks>
        /// 예: Fire, Cold, Lightning 등
        /// </remarks>
        public string id;

        /// <summary>
        /// 로컬라이즈된 피해 타입 이름을 조회하기 위한 키.
        /// </summary>
        public string nameKey;

        /// <summary>
        /// UI 표시용 아이콘 키(Addressables Key 등).
        /// </summary>
        public string iconKey;
    }
}