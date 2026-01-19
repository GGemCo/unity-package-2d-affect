using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 Modifier 서브테이블을 파싱하는 파서.
    /// </summary>
    /// <remarks>
    /// 포맷 규칙:
    /// - key: affect_modifier
    /// - 1행은 헤더(컬럼명)이며, 이후 행은 탭(\t)으로 구분된다.
    /// - AffectUid 기준으로 여러 Modifier가 존재할 수 있으므로 내부적으로 List로 보관한다.
    /// - 빈 줄/주석(#으로 시작하는 줄)은 무시한다.
    /// </remarks>
    public sealed class TableAffectModifier : ITableParser
    {
        /// <summary>
        /// 테이블 시스템에서 사용하는 키 값.
        /// </summary>
        public string Key => ConfigAddressableTableAffect.AffectModifier;

        /// <summary>
        /// AffectUid → Modifier 정의 목록 매핑.
        /// </summary>
        private readonly Dictionary<int, List<AffectModifierDefinition>> _byAffectUid = new();

        /// <summary>
        /// 탭 구분 텍스트를 파싱하여 Modifier 데이터를 로드한다.
        /// </summary>
        /// <param name="content">테이블 원문(헤더 포함).</param>
        /// <remarks>
        /// - headers 길이보다 values가 짧으면 누락 컬럼을 빈 문자열로 보정한다.
        /// - AffectUid가 0 이하인 행은 무시한다.
        /// </remarks>
        public void LoadData(string content)
        {
            _byAffectUid.Clear();

            if (string.IsNullOrWhiteSpace(content))
                return;

            var lines = content.Split('\n');
            if (lines.Length <= 1) return;

            var headers = lines[0].Trim().Split('\t');
            if (headers.Length == 0) return;

            for (int i = 1; i < lines.Length; i++)
            {
                var rawLine = lines[i];

                // 공백 라인 및 주석 라인은 스킵한다.
                if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var values = rawLine.Split('\t');

                // 컬럼 수가 부족한 경우 빈 칸으로 패딩하여 인덱스 예외를 방지한다.
                if (values.Length < headers.Length)
                    Array.Resize(ref values, headers.Length);

                // "컬럼명 → 값" 딕셔너리로 변환(앞뒤 공백 제거)
                var row = new Dictionary<string, string>(headers.Length);
                for (int j = 0; j < headers.Length; j++)
                {
                    var v = values[j] ?? string.Empty;
                    row[headers[j].Trim()] = v.Trim();
                }

                int affectUid = MathHelper.ParseInt(row.GetValueOrDefault("AffectUid"));
                if (affectUid <= 0) continue;

                var def = BuildModifier(row);
                if (!_byAffectUid.TryGetValue(affectUid, out var list))
                {
                    list = new List<AffectModifierDefinition>(4);
                    _byAffectUid.Add(affectUid, list);
                }
                list.Add(def);
            }
        }

        /// <summary>
        /// 지정한 어펙트 UID에 연결된 Modifier 정의 목록을 반환한다.
        /// </summary>
        /// <param name="affectUid">조회할 어펙트 UID.</param>
        /// <returns>Modifier 정의 목록(없으면 빈 배열).</returns>
        public IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid)
        {
            if (_byAffectUid.TryGetValue(affectUid, out var list))
                return list;
            return Array.Empty<AffectModifierDefinition>();
        }

        /// <summary>
        /// 파싱된 한 행(row)을 AffectModifierDefinition으로 변환한다.
        /// </summary>
        /// <param name="row">컬럼명 → 값 딕셔너리.</param>
        /// <returns>구성된 Modifier 정의 객체.</returns>
        /// <remarks>
        /// 필드 의미(요약):
        /// - Stat 계열: statId/statValue/statValueType/statOperation
        /// - Damage 계열: damageTypeId/damageBaseValue/scalingStatId/scalingCoefficient/canCrit/isDot
        /// - State 계열: stateId/stateChance/stateDurationOverride
        /// </remarks>
        private static AffectModifierDefinition BuildModifier(Dictionary<string, string> row)
        {
            var mod = new AffectModifierDefinition
            {
                modifierId = MathHelper.ParseInt(row.GetValueOrDefault("ModifierId")),
                phase = EnumHelper.ConvertEnum<AffectPhase>(row.GetValueOrDefault("Phase")),
                kind = EnumHelper.ConvertEnum<ModifierKind>(row.GetValueOrDefault("Kind")),

                statId = row.GetValueOrDefault("StatId"),
                statValue = MathHelper.ParseFloat(row.GetValueOrDefault("StatValue")),
                statValueType = EnumHelper.ConvertEnum<StatValueType>(row.GetValueOrDefault("StatValueType")),
                statOperation = EnumHelper.ConvertEnum<StatOperation>(row.GetValueOrDefault("StatOperation")),

                damageTypeId = row.GetValueOrDefault("DamageTypeId"),
                damageBaseValue = MathHelper.ParseFloat(row.GetValueOrDefault("DamageBaseValue")),
                scalingStatId = row.GetValueOrDefault("ScalingStatId"),
                scalingCoefficient = MathHelper.ParseFloat(row.GetValueOrDefault("ScalingCoefficient")),
                canCrit = MathHelper.ParseInt(row.GetValueOrDefault("CanCrit")) != 0,
                isDot = MathHelper.ParseInt(row.GetValueOrDefault("IsDot")) != 0,

                stateId = row.GetValueOrDefault("StateId"),
                stateChance = MathHelper.ParseFloat(row.GetValueOrDefault("StateChance")),
                stateDurationOverride = MathHelper.ParseFloat(row.GetValueOrDefault("StateDurationOverride")),
            };

            return mod;
        }
    }
}
