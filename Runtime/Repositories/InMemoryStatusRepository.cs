using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Stat / DamageType / State 정의를 분리하여 관리하는 간단한 인메모리 저장소 구현체입니다.
    /// </summary>
    /// <remarks>
    /// - 각 정의는 문자열 ID 기반으로 관리됩니다.
    /// - 저항(Resistance)은 기본적으로 Stat 규칙을 따르며,
    ///   "{ResistancePrefix}{DamageTypeId}" 형태의 스탯에서 조회됩니다.
    /// - 실제 데이터 소스(에셋, 서버 등)와 무관하게 테스트 및 런타임 초기화 용도로 사용됩니다.
    /// </remarks>
    public sealed class InMemoryStatusRepository : IStatusDefinitionRepository
    {
        /// <summary>
        /// 저항 스탯을 식별하기 위한 접두사입니다.
        /// </summary>
        /// <remarks>
        /// 기본 규칙: "RESIST_{DamageTypeId}"
        /// </remarks>
        public string ResistancePrefix = "RESIST_";

        /// <summary>
        /// 등록된 스탯 ID 집합입니다.
        /// </summary>
        private readonly HashSet<string> _stats = new();

        /// <summary>
        /// 등록된 데미지 타입 ID 집합입니다.
        /// </summary>
        private readonly HashSet<string> _damageTypes = new();

        /// <summary>
        /// 등록된 상태(State) ID 집합입니다.
        /// </summary>
        private readonly HashSet<string> _states = new();

        /// <summary>
        /// 저장소에 등록된 모든 정의를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _stats.Clear();
            _damageTypes.Clear();
            _states.Clear();
        }

        /// <summary>
        /// 유효한 스탯 ID를 등록합니다.
        /// </summary>
        /// <param name="statId">등록할 스탯 ID입니다.</param>
        public void RegisterStat(string statId)
        {
            if (!string.IsNullOrWhiteSpace(statId))
                _stats.Add(statId);
        }

        /// <summary>
        /// 유효한 데미지 타입 ID를 등록합니다.
        /// </summary>
        /// <param name="damageTypeId">등록할 데미지 타입 ID입니다.</param>
        public void RegisterDamageType(string damageTypeId)
        {
            if (!string.IsNullOrWhiteSpace(damageTypeId))
                _damageTypes.Add(damageTypeId);
        }

        /// <summary>
        /// 유효한 상태(State) ID를 등록합니다.
        /// </summary>
        /// <param name="stateId">등록할 상태 ID입니다.</param>
        public void RegisterState(string stateId)
        {
            if (!string.IsNullOrWhiteSpace(stateId))
                _states.Add(stateId);
        }

        /// <summary>
        /// 스탯 ID가 유효한지 여부를 반환합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>등록된 스탯이면 true, 아니면 false를 반환합니다.</returns>
        public bool IsValidStat(string statId) =>
            !string.IsNullOrWhiteSpace(statId) && _stats.Contains(statId);

        /// <summary>
        /// 데미지 타입 ID가 유효한지 여부를 반환합니다.
        /// </summary>
        /// <param name="damageTypeId">확인할 데미지 타입 ID입니다.</param>
        /// <returns>등록된 데미지 타입이면 true, 아니면 false를 반환합니다.</returns>
        public bool IsValidDamageType(string damageTypeId) =>
            !string.IsNullOrWhiteSpace(damageTypeId) && _damageTypes.Contains(damageTypeId);

        /// <summary>
        /// 상태(State) ID가 유효한지 여부를 반환합니다.
        /// </summary>
        /// <param name="stateId">확인할 상태 ID입니다.</param>
        /// <returns>등록된 상태이면 true, 아니면 false를 반환합니다.</returns>
        public bool IsValidState(string stateId) =>
            !string.IsNullOrWhiteSpace(stateId) && _states.Contains(stateId);

        /// <summary>
        /// 대상의 특정 데미지 타입에 대한 저항 수치를 퍼센트(0~100)로 반환합니다.
        /// </summary>
        /// <param name="damageTypeId">저항을 조회할 데미지 타입 ID입니다.</param>
        /// <param name="target">저항 스탯을 보유한 Affect 대상입니다.</param>
        /// <returns>
        /// 저항 수치(퍼센트)이며, 대상이나 스탯이 없을 경우 0을 반환합니다.
        /// </returns>
        /// <remarks>
        /// 기본 규칙:
        /// - 저항 스탯 ID = "{ResistancePrefix}{DamageTypeId}"
        /// - 스탯 값은 0~100 범위를 퍼센트로 취급합니다.
        /// </remarks>
        public float GetResistancePercent(string damageTypeId, IAffectTarget target)
        {
            if (target == null || target.Stats == null)
                return 0f;

            if (string.IsNullOrWhiteSpace(damageTypeId))
                return 0f;

            // 기본 규칙: RESIST_{DamageTypeId}
            var statId = ResistancePrefix + damageTypeId;
            float v = target.Stats.GetValue(statId);

            // 0~100 범위를 퍼센트로 취급
            return Mathf.Clamp(v, 0f, 100f);
        }
    }
}
