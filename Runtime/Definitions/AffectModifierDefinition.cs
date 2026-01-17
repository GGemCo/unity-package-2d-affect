using System;

namespace GGemCo2DAffect
{
    [Serializable]
    public sealed class AffectModifierDefinition
    {
        public int AffectUid;
        public int ModifierId;

        public AffectPhase Phase;
        public ModifierKind Kind;

        // --- Kind=Stat ---
        public string StatId;
        public float StatValue;
        public ValueType StatValueType;
        public StatOperation StatOperation;

        // --- Kind=Damage ---
        public string DamageTypeId;
        public float DamageBaseValue;
        public string ScalingStatId;
        public float ScalingCoefficient;
        public bool CanCrit;
        public bool IsDot;

        // --- Kind=State ---
        public string StateId;
        public float StateChance;
        public float StateDurationOverride;

        // --- Common ---
        public string ConditionId; // 확장용(예: TargetHasState:STUN 등)

        public override string ToString()
        {
            return $"AffectUid={AffectUid}, ModifierId={ModifierId}, Phase={Phase}, Kind={Kind}";
        }
    }
}
