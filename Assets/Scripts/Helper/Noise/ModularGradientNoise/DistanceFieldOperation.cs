using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    /// <summary>
    /// Which function to use for converting distance into [0..1] output.
    /// </summary>
    public enum DistanceFalloff
    {
        /// <summary>
        /// Output = 1 - (distance / maxDistance).
        /// </summary>
        Linear,

        /// <summary>
        /// Output = (1 - (distance / maxDistance))^2.
        /// </summary>
        Quadratic,

        /// <summary>
        /// A "smoothstep" curve. Output = t^2 * (3 - 2t).
        /// Where t = 1 - (distance / maxDistance).
        /// </summary>
        Smooth
    }

    /// <summary>
    /// A NoiseOperation that takes exactly one binary input (0 or 1),
    /// and produces a distance-field-like output. The closer a 0-value pixel
    /// is to a 1-value pixel, the higher the output. If no 1 is found 
    /// within 'Distance', output is 0.
    /// 
    /// 'DistanceFalloff' lets you pick how the distance is mapped 
    /// to the [0..1] range (linear, quadratic, or smooth).
    /// </summary>
    public class DistanceFieldOperation : NoiseOperation
    {
        /// <summary>
        /// Maximum distance to search for a '1' pixel.
        /// If no 1 is found within this distance, the output is 0.
        /// </summary>
        public float Distance = 10f;

        /// <summary>
        /// Falloff mode: linear, quadratic, or smoothstep.
        /// </summary>
        public DistanceFalloff Falloff = DistanceFalloff.Linear;

        /// <summary>
        /// Because this operation only needs a single input (binary 0/1),
        /// we override NumInputs to 1.
        /// </summary>
        public override int NumInputs => 1;

        public DistanceFieldOperation(float distance, DistanceFalloff falloffFunction)
        {
            Distance = distance;
            Falloff = falloffFunction;
        }

        /// <summary>
        /// For each (x, y):
        ///  1) If input is 1, output is 1.
        ///  2) If input is 0, we look up to 'Distance' away for any '1'.
        ///     The closest distance => we map to [0..1] based on the selected falloff.
        /// </summary>
        public override float DoOperation(GradientNoise[] inputs, float x, float y)
        {
            if (inputs == null || inputs.Length == 0 || inputs[0] == null)
            {
                // Fallback: no input => return 0
                return 0f;
            }

            float valueAtPoint = inputs[0].GetValue(x, y);
            // If the current point is already 1 => distance field is 1.
            if (valueAtPoint >= 0.5f)
            {
                return 1f;
            }

            // If input is 0, do a naive search in the range [-Distance..Distance].
            int maxRadius = Mathf.CeilToInt(Distance);
            float closest = float.MaxValue;

            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (int dy = -maxRadius; dy <= maxRadius; dy++)
                {
                    float neighborVal = inputs[0].GetValue(x + dx, y + dy);
                    if (neighborVal >= 0.5f)
                    {
                        // Euclidean distance
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < closest)
                        {
                            closest = dist;
                            if (closest <= 0f)
                                break;
                        }
                    }
                }
            }

            if (closest == float.MaxValue)
            {
                // Found no '1' in the entire search radius
                return 0f;
            }

            // Convert to a normalized [0..1] factor, where 1 => distance=0, 0 => distance >= Distance
            float t = 1f - (closest / Distance);
            if (t < 0f) t = 0f;

            // Apply the chosen falloff function
            switch (Falloff)
            {
                case DistanceFalloff.Linear:
                    // just keep t as-is
                    break;

                case DistanceFalloff.Quadratic:
                    t = t * t;
                    break;

                case DistanceFalloff.Smooth:
                    // A standard smoothstep in [0..1] => t^2 * (3 - 2t)
                    // (Ensures a smoother transition)
                    t = t * t * (3f - 2f * t);
                    break;
            }

            return t;
        }
    }
}
