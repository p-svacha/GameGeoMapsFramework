using System.Collections.Generic;
using UnityEngine;

public static class MovementModeDefs
{
    public static List<MovementModeDef> Defs => new List<MovementModeDef>()
    {
        new MovementModeDef()
        {
            DefName = "Walk",
            Label = "walk",
            Verb = "walking",
            Description = "Slowest mode of movement that doesn't drain any stamina.",
            Color = new Color(0.33f, 0.72f, 0.69f),
            Char = 'W',
            SpeedModifier = 0.5f,
            StaminaDrainModifier = 0f,
        },

        new MovementModeDef()
        {
            DefName = "Pace",
            Label = "pace",
            Verb = "pacing",
            Description = "A sort of fast walk or slow jog to save some stamina.",
            Color = new Color(0.5f, 0.72f, 0.33f),
            Char = 'P',
            SpeedModifier = 0.7f,
            StaminaDrainModifier = 0.7f,
        },

        new MovementModeDef()
        {
            DefName = "Jog",
            Label = "jog",
            Verb = "jogging",
            Description = "Default running mode with balanced speed and stamina drain.",
            Color = new Color(0.72f, 0.71f, 0.33f),
            Char = 'J',
            SpeedModifier = 1f,
            StaminaDrainModifier = 1f,
        },

        new MovementModeDef()
        {
            DefName = "Run",
            Label = "run",
            Verb = "running",
            Description = "Move fast while draining a lot of stamina.",
            Color = new Color(0.72f, 0.5f, 0.33f),
            Char = 'R',
            SpeedModifier = 1.5f,
            StaminaDrainModifier = 2f,
        },

        new MovementModeDef()
        {
            DefName = "Sprint",
            Label = "sprint",
            Verb = "sprinting",
            Description = "Running the absolute highest possible speed at the cost of extreme stamina drain.",
            Color = new Color(0.72f, 0.33f, 0.33f),
            Char = 'S',
            SpeedModifier = 3f,
            StaminaDrainModifier = 5f,
        },
    };
}
