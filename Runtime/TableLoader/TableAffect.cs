using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

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

        /// <summary>
        /// HUD 공용 UI 연출 상태 키. 비어 있으면 HUD 상태를 변경하지 않는다.
        /// </summary>
        public string UiHudVisualStateKey;

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
        /// Affect의 시간 기반 생명주기 정책입니다.
        /// </summary>
        /// <remarks>
        /// Session으로 설정하면 Duration이 0이어도 자연 만료하지 않고,
        /// 게임 실행 세션 동안만 유지되는 런타임 전용 효과로 처리합니다.
        /// </remarks>
        public AffectLifetimePolicy LifetimePolicy;

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
        /// Source(시전자/원인) 생존 상태를 Affect 유지에 반영하는 정책입니다.
        /// </summary>
        public SourceLifePolicy SourceLifePolicy;

        /// <summary>
        /// 어펙트 태그 문자열(구분자 기반).
        /// </summary>
        public string Tags;

        /// <summary>
        /// 어펙트 적용 확률(0~1 범위 기대).
        /// </summary>
        public float ApplyChance;

        /// <summary>
        /// 이 어펙트에 대해 타이머 UI 표시를 강제할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// <see cref="HasUseTimerUiOverride"/>가 true일 때만 유효합니다.
        /// </remarks>
        public bool UseTimerUi;

        /// <summary>
        /// 테이블에서 UseTimerUi 컬럼이 명시적으로 지정되었는지 여부입니다.
        /// </summary>
        /// <remarks>
        /// - true: <see cref="UseTimerUi"/> 값을 그대로 사용합니다.
        /// - false: 레거시 전역 타입 필터 정책으로 폴백합니다.
        /// </remarks>
        public bool HasUseTimerUiOverride;

        /// <summary>
        /// 어펙트 적용 시간 동안 캐릭터 외곽선(Outline)을 표시할지 여부.
        /// </summary>
        /// <remarks>
        /// - 기존 테이블과의 호환을 위해 컬럼이 없으면 false로 처리된다.
        /// - true 이면서 <see cref="OutlinePixelSize"/>가 0 이하인 경우, 런타임에서는 1로 보정하여 사용한다.
        /// </remarks>
        public bool UseOutline;

        /// <summary>
        /// Outline 두께(픽셀).
        /// </summary>
        /// <remarks>
        /// - 스프라이트의 pixelsPerUnit, Transform scale에 따라 월드 단위로 변환되어 적용된다.
        /// </remarks>
        public int OutlinePixelSize;
        public Color OutlineColor;

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
            if (LocalizationManagerAffect.Instance != null)
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
            TableRowReader reader = ReadRow(data);
            bool hasUseTimerUiOverride = TryParseYesNoOverride(data, "UseTimerUi", out bool useTimerUi);

            return new StruckTableAffect
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Memo"),
                Memo = reader.String("Memo"),
                IconKey = reader.String("IconKey"),
                UiHudVisualStateKey = reader.String("UiHudVisualStateKey"),
                DispelType = reader.Enum<DispelType>("DispelType"),
                GroupId = reader.String("GroupId"),
                BaseDuration = reader.Float("BaseDuration"),
                LifetimePolicy = reader.Enum<AffectLifetimePolicy>("LifetimePolicy"),
                TickInterval = reader.Float("TickInterval"),
                StackPolicy = reader.Enum<StackPolicy>("StackPolicy"),
                MaxStacks = reader.Int("MaxStacks"),
                RefreshPolicy = reader.Enum<RefreshPolicy>("RefreshPolicy"),
                SourceLifePolicy = reader.Enum<SourceLifePolicy>("SourceLifePolicy"),
                Tags = reader.String("Tags"),
                ApplyChance = reader.Float("ApplyChance"),
                UseTimerUi = useTimerUi,
                HasUseTimerUiOverride = hasUseTimerUiOverride,
                UseOutline = reader.BoolYN("UseOutline"),
                OutlinePixelSize = reader.Int("OutlinePixelSize"),
                OutlineColor = ColorHelper.HexToColor(reader.String("OutlineColor"), UnityEngine.Color.black)
            };
        }

        /// <summary>
        /// 테이블의 Y/N 형태 토글 컬럼을 파싱합니다.
        /// </summary>
        /// <param name="data">테이블 1행 데이터입니다.</param>
        /// <param name="columnName">파싱할 컬럼명입니다.</param>
        /// <param name="value">파싱된 토글 값입니다. 명시되지 않았으면 false를 반환합니다.</param>
        /// <returns>
        /// 컬럼에 Y 또는 N이 명시되어 있으면 true, 비어 있거나 컬럼이 없으면 false를 반환합니다.
        /// </returns>
        private static bool TryParseYesNoOverride(
            IReadOnlyDictionary<string, string> data,
            string columnName,
            out bool value)
        {
            value = false;
            if (data == null || string.IsNullOrWhiteSpace(columnName))
            {
                return false;
            }

            if (!data.TryGetValue(columnName, out string raw) || string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim().ToUpperInvariant();
            if (normalized == "Y")
            {
                value = true;
                return true;
            }

            if (normalized == "N")
            {
                value = false;
                return true;
            }

            return false;
        }
    }
}
