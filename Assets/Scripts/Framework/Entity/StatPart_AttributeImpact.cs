using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Changes the value of the stat according to the value of a skill.
/// </summary>
public class StatPart_AttributeImpact : StatPart
{
    public AttributeDef AttributeDef { get; init; } = null;
    public SkillImpactType Type { get; init; } = SkillImpactType.Additive;
    public float AttributeModifier { get; init; } = 0f;

    public override void TransformValue(Entity entity, Stat stat, ref float value)
    {
        float transformationValue = GetTransformationValue(entity);

        if (Type == SkillImpactType.Additive) value += transformationValue;
        if (Type == SkillImpactType.Multiplicative) value *= transformationValue;
    }

    public override string ExplanationString(Entity entity, Stat stat)
    {
        float transformationValue = GetTransformationValue(entity);
        string sign = "";
        if (Type == SkillImpactType.Additive) sign = transformationValue > 0 ? "+" : "";
        if (Type == SkillImpactType.Multiplicative) sign = "x";
        return $"{AttributeDef.LabelCapWord}: {sign}{stat.GetValueText(transformationValue)}";
    }

    private float GetTransformationValue(Entity entity)
    {
        float attributeValue = entity.GetAttribute(AttributeDef);
        return attributeValue * AttributeModifier;
    }
}

public enum SkillImpactType
{
    Additive,
    Multiplicative,
}
