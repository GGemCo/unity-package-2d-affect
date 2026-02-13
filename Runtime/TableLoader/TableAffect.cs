using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 테이블의 한 행(Row)을 표현하는 구조체 클래스.
    /// </summary>
    /// <remarks>
    /// - 실제 적용 로직은 AffectComponent가 담당한다.
    /// - 이 구조는 Core 레이어에서 테이블 로드 및 UI/툴링 표시에 필요한
    ///   순수 데이터만을 보관하는 용도로 사용된다.
    /// </remarks>
    public sealed class StruckTableAffect
    {
        /// <summary>
        /// 어펙트 고유 식별자(Uid).
        /// </summary>
        public int Uid;

        // ----------------------
        // Display (UI/툴링)
        // ----------------------

        /// <summary>
        /// 표시용 이름(로컬라이징 결과가 주입된다).
        /// </summary>
        public string Name;

        /// <summary>
        /// 기획/디버그용 메모 문자열.
        /// </summary>
        public string Memo;

        /// <summary>
        /// 아이콘 리소스 키.
        /// </summary>
        public string IconKey;

        // ----------------------
        // Runtime (게임 로직)
        // ----------------------

        /// <summary>
        /// Dispel(해제) 시 사용되는 타입 분류.
        /// </summary>
        public DispelType DispelType;

        /// <summary>
        /// 그룹 ID. 동일 그룹 내에서는 단일 어펙트만 유지된다.
        /// </summary>
        public string GroupId;

        /// <summary>
        /// 기본 지속 시간(초).
        /// </summary>
        public float BaseDuration;

        /// <summary>
        /// Tick 간격(초). 0 이하이면 Tick을 사용하지 않는다.
        /// </summary>
        public float TickInterval;

        /// <summary>
        /// 동일 UID 재적용 시 스택 처리 정책.
        /// </summary>
        public StackPolicy StackPolicy;

        /// <summary>
        /// 허용되는 최대 스택 수.
        /// </summary>
        public int MaxStacks;

        /// <summary>
        /// 재적용 시 지속시간/값 갱신 정책.
        /// </summary>
        public RefreshPolicy RefreshPolicy;

        /// <summary>
        /// 어펙트 태그 문자열(구분자 기반).
        /// </summary>
        public string Tags;

        /// <summary>
        /// 재생할 Effect 식별자 UID.
        /// </summary>
        public int EffectUid;

        /// <summary>
        /// Effect 스케일 배율.
        /// </summary>
        public float EffectScale;

        /// <summary>
        /// Effect Y축 오프셋 값.
        /// </summary>
        public float EffectOffsetY;

        /// <summary>
        /// Effect 표시 기준 위치 타입.
        /// </summary>
        public AffectEffectPositionType EffectPositionType;

        /// <summary>
        /// Effect 추적(Follow) 타입.
        /// </summary>
        public AffectEffectFollowType EffectFollowType;

        /// <summary>
        /// Effect 정렬 레이어 키. Core의 <see cref="ConfigSortingLayer.Keys"/> 중 하나.
        /// </summary>
        public ConfigSortingLayer.Keys EffectSortingLayerKey;

        /// <summary>
        /// 어펙트 적용 확률(0~1 범위 기대).
        /// </summary>
        public float ApplyChance;

        /// <summary>
        /// Tick을 사용하는 어펙트인지 여부.
        /// </summary>
        public bool HasTick => TickInterval > 0f;
    }

    /// <summary>
    /// 어펙트 테이블 로더 클래스(A안/신규 포맷).
    /// </summary>
    /// <remarks>
    /// - DefaultTable을 상속하여 Addressable/CSV 기반 데이터를 로드한다.
    /// - 로드 후 로컬라이징 처리 및 데이터 파싱을 담당한다.
    /// </remarks>
    public sealed class TableAffect : DefaultTable<StruckTableAffect>
    {
        /// <summary>
        /// Addressable 또는 테이블 시스템에서 사용하는 키 값.
        /// </summary>
        public override string Key => ConfigAddressableTableAffect.Affect;

        /// <summary>
        /// 테이블 데이터 1행이 로드된 직후 호출된다.
        /// </summary>
        /// <param name="data">로드된 어펙트 데이터.</param>
        /// <remarks>
        /// 로컬라이징 시스템이 존재하면 UID 기반으로 이름을 치환한다.
        /// 기존 방식과의 호환을 위해 로컬라이징이 없을 경우 Memo를 이름으로 사용한다.
        /// </remarks>
        protected override void OnLoadedData(StruckTableAffect data)
        {
            if (data == null) return;

            // 기존 방식과의 호환: 로컬라이징 키가 비어있으면 uid 문자열을 사용한다.
            if (LocalizationManager.Instance != null)
            {
                data.Name = LocalizationManagerAffect.Instance.GetAffectNameByKey($"{data.Uid}");
            }
            else
            {
                data.Name = $"{data.Memo}";
            }
        }

        /// <summary>
        /// 문자열 기반 원시 데이터(Dictionary)를 StruckTableAffect 객체로 변환한다.
        /// </summary>
        /// <param name="data">컬럼명 → 문자열 값 형태의 테이블 데이터.</param>
        /// <returns>파싱된 어펙트 테이블 행 객체.</returns>
        /// <remarks>
        /// - 숫자/열거형 파싱은 Helper 유틸을 사용한다.
        /// - 컬럼 누락 시 GetValueOrDefault를 통해 빈 문자열을 허용한다.
        /// </remarks>
        protected override StruckTableAffect BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableAffect
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data.GetValueOrDefault("Memo"),
                Memo = data.GetValueOrDefault("Memo"),
                IconKey = data.GetValueOrDefault("IconKey"),
                DispelType = EnumHelper.ConvertEnum<DispelType>(data.GetValueOrDefault("DispelType")),
                GroupId = data.GetValueOrDefault("GroupId"),
                BaseDuration = MathHelper.ParseFloat(data.GetValueOrDefault("BaseDuration")),
                TickInterval = MathHelper.ParseFloat(data.GetValueOrDefault("TickInterval")),
                StackPolicy = EnumHelper.ConvertEnum<StackPolicy>(data.GetValueOrDefault("StackPolicy")),
                MaxStacks = MathHelper.ParseInt(data.GetValueOrDefault("MaxStacks")),
                RefreshPolicy = EnumHelper.ConvertEnum<RefreshPolicy>(data.GetValueOrDefault("RefreshPolicy")),
                Tags = data.GetValueOrDefault("Tags"),
                EffectUid = MathHelper.ParseInt(data.GetValueOrDefault("EffectUid")),
                EffectScale = MathHelper.ParseFloat(data.GetValueOrDefault("EffectScale")),
                EffectOffsetY = MathHelper.ParseFloat(data.GetValueOrDefault("EffectOffsetY")),
                EffectPositionType = EnumHelper.ConvertEnum<AffectEffectPositionType>(data.GetValueOrDefault("EffectPositionType")),
                EffectFollowType = EnumHelper.ConvertEnum<AffectEffectFollowType>(data.GetValueOrDefault("EffectFollowType")),
                EffectSortingLayerKey = EnumHelper.ConvertEnum<ConfigSortingLayer.Keys>(data.GetValueOrDefault("EffectSortingLayerKey")),
                ApplyChance = MathHelper.ParseFloat(data.GetValueOrDefault("ApplyChance"))
            };
        }
    }
}
