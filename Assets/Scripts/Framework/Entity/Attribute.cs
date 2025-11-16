using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An attribute represents an entitys attribute of something or proficiency at something, often used for influencing stats.
/// <br/>The value of an attribute is set manually for an entity and can be changed at any time.
/// </summary>
public class Attribute
{
    public AttributeDef Def { get; private set; }

    /// <summary>
    /// The entity this attribute is attached to.
    /// </summary>
    public Entity Entity { get; private set; }

    /// <summary>
    /// The value the entity has for this attribute.
    /// </summary>
    public float Value { get; set; }

    public Attribute(AttributeDef def, Entity entity, float value)
    {
        Def = def;
        Entity = entity;
        Value = value;
    }
}
