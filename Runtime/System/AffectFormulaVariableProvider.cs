using System;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect로 증가한 값을 Base*/Stat* 계산과 분리하여 Poly 데미지 공식 변수로만 제공하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// - 이 컴포넌트는 <see cref="IDamageFormulaVariableProvider"/>를 구현하여 Core의 공식 계산 직전에만 값을 주입합니다.
    /// - 등록된 값은 캐릭터의 Base*, Stat*, TotalBase*, TotalStat* 항목에 영향을 주지 않습니다.
    /// - 공식 변수 이름은 등록한 원본 ID와, 공격자/피격자 접두어가 붙은 ID를 함께 제공합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AffectFormulaVariableProvider : MonoBehaviour, IDamageFormulaVariableProvider
    {
        private readonly Dictionary<string, List<FormulaVariableToken>> _tokensById = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Affect 공식 변수 값을 등록하고, 해제 시 사용할 토큰을 반환합니다.
        /// </summary>
        /// <param name="variableId">공식에서 사용할 변수 ID입니다.</param>
        /// <param name="value">공식 변수에 반영할 값입니다.</param>
        /// <param name="valueType">값의 의미입니다. 실제 계산 변환은 공식 수식에서 처리합니다.</param>
        /// <param name="operation">동일 변수 ID의 누적 연산 방식입니다.</param>
        /// <returns>해제 시 사용할 토큰입니다. 입력이 유효하지 않으면 <c>null</c>을 반환합니다.</returns>
        public object AddVariable(string variableId, float value, StatValueType valueType, StatOperation operation)
        {
            string normalizedId = NormalizeVariableId(variableId);
            if (string.IsNullOrWhiteSpace(normalizedId))
                return null;

            var token = new FormulaVariableToken(normalizedId, Sanitize(value), valueType, operation);
            if (!_tokensById.TryGetValue(normalizedId, out List<FormulaVariableToken> tokens))
            {
                tokens = new List<FormulaVariableToken>(2);
                _tokensById.Add(normalizedId, tokens);
            }

            tokens.Add(token);
            return token;
        }

        /// <summary>
        /// <see cref="AddVariable"/>에서 반환한 토큰을 제거합니다.
        /// </summary>
        /// <param name="token">제거할 공식 변수 토큰입니다.</param>
        public void RemoveVariable(object token)
        {
            if (token is not FormulaVariableToken formulaToken)
                return;

            if (!_tokensById.TryGetValue(formulaToken.VariableId, out List<FormulaVariableToken> tokens))
                return;

            tokens.Remove(formulaToken);
            if (tokens.Count == 0)
                _tokensById.Remove(formulaToken.VariableId);
        }

        /// <summary>
        /// 현재 캐릭터가 보유한 Affect 공식 변수를 데미지 공식 변수 컨테이너에 등록합니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="variables">변수를 등록할 컨테이너입니다.</param>
        /// <remarks>
        /// 같은 컴포넌트가 공격자에게 붙어 있으면 <c>Attacker*</c>, 피격자에게 붙어 있으면 <c>Target*</c> 접두어 변수를 함께 등록합니다.
        /// 원본 변수 ID도 등록하므로, 단일 캐릭터 테스트 공식에서도 바로 사용할 수 있습니다.
        /// </remarks>
        public void FillDamageFormulaVariables(CharacterBase attacker, CharacterBase target, DamageFormulaVariableBag variables)
        {
            if (variables == null || _tokensById.Count == 0)
                return;

            CharacterBase owner = GetComponent<CharacterBase>();
            string rolePrefix = ResolveRolePrefix(owner, attacker, target);

            foreach (KeyValuePair<string, List<FormulaVariableToken>> pair in _tokensById)
            {
                double resolvedValue = ResolveValue(pair.Value);
                variables.Set(pair.Key, resolvedValue);

                string pascalName = ToPascalVariableName(pair.Key);
                if (!string.Equals(pair.Key, pascalName, StringComparison.OrdinalIgnoreCase))
                    variables.Set(pascalName, resolvedValue);

                if (!string.IsNullOrEmpty(rolePrefix))
                {
                    variables.Set(rolePrefix + pair.Key, resolvedValue);
                    variables.Set(rolePrefix + pascalName, resolvedValue);
                }
            }
        }

        /// <summary>
        /// 등록된 토큰 목록을 하나의 공식 변수 값으로 계산합니다.
        /// </summary>
        /// <param name="tokens">동일 변수 ID에 등록된 토큰 목록입니다.</param>
        /// <returns>공식에 주입할 최종 변수 값입니다.</returns>
        /// <remarks>
        /// Add는 합산, Multiply는 현재 값을 배율처럼 곱하고, Override는 마지막 Override 값을 우선합니다.
        /// 일반적인 버프 누적값은 Add 사용을 권장합니다.
        /// </remarks>
        private static double ResolveValue(List<FormulaVariableToken> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return 0d;

            bool hasOverride = false;
            double overrideValue = 0d;
            double addValue = 0d;
            double multiplyValue = 1d;
            bool hasMultiply = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                FormulaVariableToken token = tokens[i];
                if (token == null)
                    continue;

                switch (token.Operation)
                {
                    case StatOperation.Override:
                        hasOverride = true;
                        overrideValue = token.Value;
                        break;
                    case StatOperation.Multiply:
                        hasMultiply = true;
                        multiplyValue *= token.Value;
                        break;
                    case StatOperation.None:
                    case StatOperation.Add:
                    default:
                        addValue += token.Value;
                        break;
                }
            }

            if (hasOverride)
                return overrideValue;

            return hasMultiply ? addValue * multiplyValue : addValue;
        }

        /// <summary>
        /// 공식 변수 ID의 앞뒤 공백을 제거합니다.
        /// </summary>
        private static string NormalizeVariableId(string variableId)
        {
            return string.IsNullOrWhiteSpace(variableId) ? string.Empty : variableId.Trim();
        }

        /// <summary>
        /// 공식 계산을 방해하지 않도록 NaN/Infinity 값을 0으로 보정합니다.
        /// </summary>
        private static float Sanitize(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        /// <summary>
        /// 변수 제공 컴포넌트가 공격자/피격자 중 어디에 속하는지 판정합니다.
        /// </summary>
        private static string ResolveRolePrefix(CharacterBase owner, CharacterBase attacker, CharacterBase target)
        {
            if (owner == null)
                return string.Empty;

            if (attacker != null && ReferenceEquals(owner, attacker))
                return "Attacker";

            if (target != null && ReferenceEquals(owner, target))
                return "Target";

            return string.Empty;
        }

        /// <summary>
        /// <c>FORMULA_FINAL_DAMAGE_BUFF</c> 같은 ID를 <c>FormulaFinalDamageBuff</c> 형식으로도 사용할 수 있게 변환합니다.
        /// </summary>
        private static string ToPascalVariableName(string variableId)
        {
            if (string.IsNullOrWhiteSpace(variableId))
                return string.Empty;

            string[] parts = variableId.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
                return variableId;

            var builder = new StringBuilder(variableId.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].ToLowerInvariant();
                if (part.Length == 0)
                    continue;

                builder.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    builder.Append(part.Substring(1));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Affect 공식 변수 1건의 적용 토큰입니다.
        /// </summary>
        private sealed class FormulaVariableToken
        {
            public readonly string VariableId;
            public readonly double Value;
            public readonly StatValueType ValueType;
            public readonly StatOperation Operation;

            public FormulaVariableToken(string variableId, float value, StatValueType valueType, StatOperation operation)
            {
                VariableId = variableId;
                Value = value;
                ValueType = valueType;
                Operation = operation == StatOperation.None ? StatOperation.Add : operation;
            }
        }
    }
}
