using System.Collections.Generic;
using UnityEngine;

public static class AttributeDefs
{
    public static List<AttributeDef> Defs => new List<AttributeDef>()
    {
        new AttributeDef()
        {
            DefName = "Speed",
            Label = "speed",
            Description = "Skill how fast this racer is generally. Acts as a multiplicative modifier to movement speed.",
        },

        new AttributeDef()
        {
            DefName = "Stamina",
            Label = "stamina",
            Description = "How much stamina this racer has when fully rested.",
        },

        new AttributeDef()
        {
            DefName = "Endurance",
            Label = "endurance",
            Description = "How much stamina this racer uses when moving. Acts as a multiplicative modifier on stamina drain.",
        },

        new AttributeDef()
        {
            DefName = "RecoveryRate",
            Label = "recovery rate",
            Description = "How fast this racer recovers stamina at a rest stop or between races. Acts as a multiplicative modifier on stamina recovery.",
        },
    };
}
