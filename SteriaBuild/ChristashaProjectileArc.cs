using System;
using UnityEngine;

namespace Steria.Vfx
{
    /// <summary>One upward arc, shared by live attachment and GPU previews.</summary>
    public static class ChristashaProjectileArc
    {
        public const float FlightTime = .38f;
        public const float VisualDuration = FlightTime + .48f;
        public const float LaunchTime = .10f;
        public const float HorizontalEndTime = .18f;
        public const float PortalFadeEnd = .40f;
        public const float TravelTime = FlightTime - LaunchTime;
        public const float HorizontalFraction = (HorizontalEndTime - LaunchTime) / TravelTime;

        public static Vector3 PortalAnchor(Vector3 casterWorldPosition, Transform facing)
        {
            Vector3 behind = facing == null ? new Vector3(2.2f, 5.6f, 0f) :
                facing.TransformPoint(new Vector3(2.2f, 5.6f, 0f)) - facing.position;
            return new Vector3(casterWorldPosition.x + behind.x, casterWorldPosition.y + behind.y,
                casterWorldPosition.z + .2f);
        }

        public static float PortalRadius(float actorScale)
        {
            return Math.Min(2.7f, Math.Max(1.9f, .8f * Math.Abs(actorScale)));
        }

        public static Vector3 PortalLaunchPoint(Vector3 center, Vector3 target, float radius)
        {
            Vector3 delta=target-center;
            if(delta.x*delta.x+delta.y*delta.y<.0001f)return center;
            Vector3 direction=PortalTangent(center,target,0f);
            float planar=(float)Math.Sqrt(direction.x*direction.x+direction.y*direction.y);
            if(planar<.0001f)return center;
            float offset=Math.Min(1.1f,Math.Max(0f,radius)*.45f);
            return center+new Vector3(direction.x/planar*offset,direction.y/planar*offset,0f);
        }

        static Vector3 HorizontalEnd(Vector3 start, Vector3 end)
        {
            Vector3 delta = end - start;
            float along = Math.Min(Math.Abs(delta.x) * .25f, 9f);
            float x = start.x + (delta.x < 0f ? -along : along);
            float ratio = Math.Abs(delta.x) < .0001f ? 0f : along / Math.Abs(delta.x);
            return new Vector3(x, start.y, start.z + delta.z * ratio);
        }

        static void DescentControls(Vector3 start, Vector3 legEnd, Vector3 end,
            out Vector3 first, out Vector3 second)
        {
            Vector3 leg = legEnd - start, remaining = end - legEnd;
            // Match the horizontal leg's derivative at the join.
            first = legEnd + leg * ((1f - HorizontalFraction) / (3f * HorizontalFraction));
            second = new Vector3(legEnd.x + remaining.x * .67f,
                legEnd.y + remaining.y * .55f, legEnd.z + remaining.z * .67f);
        }

        public static Vector3 PortalPosition(Vector3 start, Vector3 end, float progress)
        {
            float clock = Mathf.Clamp01(progress);
            Vector3 delta = end - start;
            if (delta.x * delta.x + delta.y * delta.y < .0001f) return Vector3.Lerp(start, end, clock);
            Vector3 legEnd = HorizontalEnd(start, end);
            if (clock <= HorizontalFraction) return Vector3.Lerp(start, legEnd, clock / HorizontalFraction);
            float t = (clock - HorizontalFraction) / (1f - HorizontalFraction), u = 1f - t;
            Vector3 first, second; DescentControls(start, legEnd, end, out first, out second);
            return legEnd * (u*u*u) + first * (3*u*u*t) + second * (3*u*t*t) + end * (t*t*t);
        }

        public static Vector3 PortalTangent(Vector3 start, Vector3 end, float progress)
        {
            Vector3 delta = end - start;
            if (delta.x * delta.x + delta.y * delta.y < .0001f)
                return delta.sqrMagnitude > .000001f ? delta : new Vector3(-1f, 0f, 0f);
            float clock = Mathf.Clamp01(progress);
            Vector3 legEnd = HorizontalEnd(start, end);
            if (clock <= HorizontalFraction) return (legEnd - start) * (1f / HorizontalFraction);
            float t = (clock - HorizontalFraction) / (1f - HorizontalFraction), u = 1f - t;
            Vector3 first, second; DescentControls(start, legEnd, end, out first, out second);
            return ((first - legEnd) * (3*u*u) + (second - first) * (6*u*t) +
                (end - second) * (3*t*t)) * (1f / (1f - HorizontalFraction));
        }

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
