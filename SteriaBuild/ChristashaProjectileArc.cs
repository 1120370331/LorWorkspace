using System;
using UnityEngine;

namespace Steria.Vfx
{
    /// <summary>One upward arc, shared by live attachment and GPU previews.</summary>
    public static class ChristashaProjectileArc
    {
        public const float FlightTime = .38f;
        public const float VisualDuration = FlightTime + .48f;

        static void Controls(Vector3 start, Vector3 end, out Vector3 first, out Vector3 second)
        {
            Vector3 delta = end - start;
            float distance = (float)Math.Sqrt(delta.x * delta.x + delta.y * delta.y);
            // World-up on either facing; cap the rise so long-range shots do
            // not leave the combat camera. Coincident endpoints have no loop.
            float rise = Math.Min(6f, distance * .30f);
            first = start + delta * .28f + new Vector3(0, rise, 0);
            second = start + delta * .72f + new Vector3(0, rise * .90f, 0);
        }

        public static Vector3 Position(Vector3 start, Vector3 end, float progress)
        {
            float clock = Mathf.Clamp01(progress);
            float t = clock * clock, u = 1f - t;
            Vector3 first, second; Controls(start, end, out first, out second);
            return start * (u*u*u) + first * (3*u*u*t) + second * (3*u*t*t) + end * (t*t*t);
        }

        public static Vector3 Tangent(Vector3 start, Vector3 end, float progress)
        {
            // Direction of the accelerating path. The positive time derivative
            // changes speed, not orientation; omit it to keep a valid launch aim.
            float clock = Mathf.Clamp01(progress);
            float t = clock * clock, u = 1f - t;
            Vector3 first, second; Controls(start, end, out first, out second);
            return (first-start) * (3*u*u) + (second-first) * (6*u*t) + (end-second) * (3*t*t);
        }
    }
}
