// Managed adapter-boundary spy. Does not verify Shuriken, materials or GPU rendering.
using UnityEngine;
namespace Steria.Vfx
{
    public enum SlazeyaWindKind { Slash, Hit, Pierce, Guard, Projectile }
    public sealed class SlazeyaWindVisual
    {
        public float Elapsed, Duration;
        public int ArrivalCount, StopCount;
        public bool IsComplete;
        public SlazeyaWindVisual(GameObject root, SlazeyaWindKind kind, uint seed, float duration = 0)
        { Duration = kind == SlazeyaWindKind.Slash ? .57f : kind == SlazeyaWindKind.Projectile ? .53f : kind == SlazeyaWindKind.Guard ? .5f : .48f; }
        public void Advance(float dt) { Elapsed += dt; }
        public void ApplyAt(float seconds) { Elapsed = seconds; }
        public void Arrive() { ArrivalCount++; }
        public void StopAndClear() { StopCount++; IsComplete = true; }
    }
}
