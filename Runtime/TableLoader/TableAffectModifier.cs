using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 Modifier 공통 메타 테이블을 파싱하는 파서입니다.
    /// </summary>
    /// <remarks>
    /// <c>affect_modifier</c>는 AffectUid, ModifierId, Phase, Kind, ConditionId 같은
    /// 실행 공통 메타만 보관합니다. Stat/Damage/Heal/State 같은 Kind별 상세 값은
    /// <c>affect_modifier_*</c> 상세 테이블에서 로드한 Payload로 구성합니다.
    /// </remarks>
    public sealed class TableAffectModifier : ITableParser
    {
        /// <summary>
        /// 테이블 시스템에서 사용하는 키 값입니다.
        /// </summary>
        public string Key => ConfigAddressableTableAffect.AffectModifier;

        /// <summary>
        /// AffectUid → Modifier 정의 목록 매핑입니다.
        /// </summary>
        private readonly Dictionary<int, List<AffectModifierDefinition>> _byAffectUid = new();

        /// <summary>
        /// 탭 구분 텍스트를 파싱하여 Modifier 공통 메타 데이터를 로드합니다.
        /// </summary>
        /// <param name="content">테이블 원문입니다.</param>
        /// <remarks>
        /// 컬럼 수가 부족한 행은 빈 문자열로 패딩하여 파싱 오류를 방지합니다.
        /// AffectUid가 0 이하인 행은 실제 어펙트에 연결할 수 없으므로 무시합니다.
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

                if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var values = rawLine.Split('\t');
                if (values.Length < headers.Length)
                    Array.Resize(ref values, headers.Length);

                var row = new Dictionary<string, string>(headers.Length);
                for (int j = 0; j < headers.Length; j++)
                {
                    var v = values[j] ?? string.Empty;
                    row[headers[j].Trim()] = v.Trim();
                }

                TableRowReader reader = new TableRowReader(row, nameof(TableAffectModifier));
                int affectUid = reader.Int("AffectUid");
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
        /// 지정한 어펙트 UID에 연결된 Modifier 정의 목록을 반환합니다.
        /// </summary>
        /// <param name="affectUid">조회할 어펙트 UID입니다.</param>
        /// <returns>Modifier 정의 목록입니다. 존재하지 않으면 빈 배열을 반환합니다.</returns>
        public IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid)
        {
            if (_byAffectUid.TryGetValue(affectUid, out var list))
                return list;
            return Array.Empty<AffectModifierDefinition>();
        }

        /// <summary>
        /// 파싱된 한 행을 AffectModifierDefinition 공통 메타로 변환합니다.
        /// </summary>
        /// <param name="row">컬럼명 → 값 딕셔너리입니다.</param>
        /// <returns>공통 메타만 채워진 Modifier 정의 객체입니다.</returns>
        /// <remarks>
        /// 여기서는 상세 Payload를 만들지 않습니다.
        /// Payload는 <see cref="TableLoaderManagerAffect.ApplyModifierDetailPayloads"/>에서 Kind별 상세 테이블을 통해 주입합니다.
        /// </remarks>
        private static AffectModifierDefinition BuildModifier(Dictionary<string, string> row)
        {
            TableRowReader reader = new TableRowReader(row, nameof(TableAffectModifier));

            return new AffectModifierDefinition
            {
                affectUid = reader.Int("AffectUid"),
                modifierId = reader.Int("ModifierId"),
                phase = reader.Enum<AffectPhase>("Phase"),
                kind = reader.Enum<ModifierKind>("Kind"),
                conditionId = reader.String("ConditionId"),
            };
        }
    }
}
