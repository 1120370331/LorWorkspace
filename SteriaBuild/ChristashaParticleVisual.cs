using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steria.Vfx
{
    /// <summary>
    /// Deterministic, manually advanced ParticleSystem recipes. The caller owns the root,
    /// placement, facing and lifetime. No component is attached and no shared material is mutated.
    /// </summary>
    public sealed class ChristashaParticleVisual
    {
        public const string BundleName = "steria_christasha_particle_v2";
        private const float Step = 1f / 120f;
        private readonly ParticleSystem[] systems;
        private readonly Dictionary<string, ParticleSystem> roles = new Dictionary<string, ParticleSystem>();
        private readonly List<Birth> births = new List<Birth>(1024);
        private readonly List<ParticleSystem> emittedSystems = new List<ParticleSystem>(5);
        private readonly System.Random random;
        private readonly float speed;
        private readonly string recipe;
        private readonly ParticleSystem.Particle[] bodyParticles = new ParticleSystem.Particle[8];
        private float clock, accumulated, recipeDuration;
        private int cursor;
        private bool stopped;

        public float Duration { get { return recipeDuration / speed; } }
        public float Elapsed { get { return clock / speed; } }
        private Color tint = Color.white;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        public Color Tint
        {
            get { return tint; }
            set
            {
                tint = value;
                if (systems == null) return;
                properties.SetColor("_Tint", value);
                foreach (var system in systems)
                    if (system != null) system.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(properties);
            }
        }
        public int LiveParticleCount
        {
            get { int count = 0; foreach (var system in systems) if (system != null) count += system.particleCount; return count; }
        }

        private struct Birth
        {
            public float time, life, rotation;
            public Vector3 position, velocity, size;
            public Color color;
            public string role;
            public uint seed;
        }

        public static ChristashaParticleVisual Play(GameObject instance, uint seed, float playbackSpeed = 1f)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (float.IsNaN(playbackSpeed) || float.IsInfinity(playbackSpeed) || playbackSpeed <= 0f)
                throw new ArgumentOutOfRangeException("playbackSpeed");
            return new ChristashaParticleVisual(instance, seed, playbackSpeed);
        }

        private ChristashaParticleVisual(GameObject instance, uint seed, float playbackSpeed)
        {
            speed = playbackSpeed;
            Tint = Color.white;
            random = new System.Random(unchecked((int)seed));
            systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var system in systems)
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpeed = 1f;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                system.useAutoRandomSeed = false;
                system.randomSeed = seed == 0 ? 1u : seed;
                var emission = system.emission;
                emission.enabled = false;
                system.Pause(false);
                system.GetComponent<ParticleSystemRenderer>().sortingLayerName = "Particle";
                roles[system.gameObject.name] = system;
            }
            Tint = Color.white;
            foreach (string role in new[] { "Body", "Edge", "Spray", "Mist", "Glint" })
                if (!roles.ContainsKey(role)) throw new ArgumentException("Missing V2 particle role: " + role);
            string name = instance.name.Replace("(Clone)", "");
            string kind = name.Substring(name.LastIndexOf('_') + 1);
            recipe = kind;
            BuildRecipe(kind);
            births.Sort((a, b) => a.time.CompareTo(b.time));
            EmitDue();
            AnimateBody();
        }

        /// <summary>Exactly one clock. Pause prevents Editor/game automatic particle advancement.</summary>
        public void Advance(float deltaTime)
        {
            if (stopped || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
            accumulated += Mathf.Min(deltaTime * speed, recipeDuration + Step);
            while (accumulated + 0.000001f >= Step && !stopped)
            {
                accumulated -= Step;
                foreach (var system in systems)
                {
                    if (system == null) continue;
                    system.Simulate(Step, false, false, false);
                    system.Pause(false);
                }
                clock += Step;
                EmitDue();
                AnimateBody();
                if (clock + 0.00001f >= recipeDuration) StopAndClear();
            }
        }

        public void StopAndClear()
        {
            foreach (var system in systems)
                if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            stopped = true;
            accumulated = 0f;
            cursor = births.Count;
        }

        private void EmitDue()
        {
            emittedSystems.Clear();
            while (cursor < births.Count && births[cursor].time <= clock + 0.00001f)
            {
                Birth b = births[cursor++];
                var emission = new ParticleSystem.EmitParams();
                emission.position = b.position;
                emission.velocity = b.velocity;
                emission.startLifetime = b.life;
                emission.startSize3D = b.size;
                emission.rotation = b.rotation;
                emission.startColor = b.color;
                emission.randomSeed = b.seed;
                roles[b.role].Emit(emission, 1);
                roles[b.role].Pause(false);
                if (!emittedSystems.Contains(roles[b.role])) emittedSystems.Add(roles[b.role]);
            }
            // Unity 2019 may leave newborn billboard render data stale after Emit + Pause.
            // Materialize its derived data before a camera can consume it, without advancing age.
            foreach (var system in emittedSystems)
            {
                system.Simulate(0f, false, false, false);
                system.Pause(false);
            }
        }

        private float R(float min, float max) { return Mathf.Lerp(min, max, (float)random.NextDouble()); }
        private void Add(string role, float time, Vector2 point, Vector2 velocity, float length, float width, float rotation, float life, Color color)
        {
            // Native melee inherits map scale 3/2.25 and the attack multiplier 2 exactly once.
            float unit = recipe == "Slash" || recipe == "Hit" || recipe == "Pierce" || recipe == "Guard" ? 1f / 3f : 1f;
            point *= unit;
            velocity *= unit;
            // Mesh geometry is authored in the same reference units as billboard sizes.
            length *= unit;
            width *= unit;
            births.Add(new Birth { role = role, time = time, position = new Vector3(point.x, point.y, 0),
                velocity = new Vector3(velocity.x, velocity.y, 0), size = new Vector3(length, width, 1),
                rotation = rotation, life = life, color = color, seed = (uint)random.Next(1, int.MaxValue) });
        }

        private void BuildRecipe(string kind)
        {
            // Recipe definitions below are the only effect-specific authoring surface.
            switch (kind)
            {
                case "Slash": recipeDuration = 0.65f; Slash(); break;
                case "Hit": recipeDuration = 0.62f; Hit(); break;
                case "Pierce": recipeDuration = 0.62f; Pierce(false); break;
                case "Cast": recipeDuration = 0.72f; Cast(); break;
                case "Area": recipeDuration = 1.16f; Area(); break;
                case "Point": recipeDuration = 0.62f; Point(); break;
                case "Projectile": recipeDuration = 0.55f; Pierce(true); break;
                // Runnable guide example only; not a root of the seven-prefab shipping bundle.
                case "Guard": recipeDuration = 0.62f; Guard(); break;
                default: throw new ArgumentException("Unknown V2 particle recipe: " + kind);
            }
        }

        // Authored source meshes carry the large form. Independent spray births carry motion and variation.
        private static readonly Color Gold = new Color(1f, 0.81f, 0.39f, 0.9f);
        private static readonly Color Ivory = new Color(1f, 0.957f, 0.769f, 0.9f);
        private static readonly Color Shadow = new Color(0.32f, 0.28f, 0.19f, 0.18f);
        private void Main(float life, Vector2 point, Vector2 velocity)
        { Add("Body", 0, point, velocity, 1, 1, 0, life, Color.white); }
        private void Slash()
        {
            Main(0.43f, Vector2.zero, new Vector2(-0.08f, -0.12f));
            for (int i = 0; i < 16; i++)
            {
                float u = i / 15f, theta = Mathf.Lerp(115, 260, u) * Mathf.Deg2Rad;
                Vector2 p = new Vector2(Mathf.Cos(theta) * 0.81f, Mathf.Sin(theta) * 0.9f);
                Vector2 tangent = new Vector2(-Mathf.Sin(theta), Mathf.Cos(theta));
                Add("Spray", 0.025f + u * 0.12f, p, tangent * R(0.7f, 1.4f), R(0.09f, 0.22f), R(0.025f, 0.055f), theta * Mathf.Rad2Deg + 90, R(0.25f, 0.43f), Gold);
            }
            Add("Edge", 0.065f, new Vector2(-0.64f, -0.67f), new Vector2(-0.32f, -0.18f), 0.48f, 0.055f, 46, 0.13f, Ivory);
            Mist(4, 0.035f, new Vector2(-0.4f, -0.12f), 0.4f, 0.65f);
        }
        private void Hit()
        {
            Main(0.31f, new Vector2(0.17f, 0), new Vector2(-0.75f, 0));
            Add("Edge", 0.025f, new Vector2(-0.8f, 0), new Vector2(-0.4f, 0), 0.25f, 0.18f, 0, 0.095f, Ivory);
            for (int i = 0; i < 8; i++)
            {
                float a = R(135, 225) * Mathf.Deg2Rad;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Add("Spray", R(0.035f, 0.075f), new Vector2(-0.72f, R(-0.2f, 0.2f)), v * R(1.2f, 2.3f), R(0.16f, 0.3f), R(0.035f, 0.08f), a * Mathf.Rad2Deg, R(0.22f, 0.41f), Gold);
            }
            Mist(5, 0.02f, new Vector2(-0.55f, 0), 0.28f, 0.75f);
        }
        private void Pierce(bool projectile)
        {
            Main(projectile ? 0.39f : 0.35f, new Vector2(0.1f, 0), new Vector2(projectile ? 0 : -0.55f, 0));
            for (int i = 0; i < 10; i++)
            {
                float y = R(-0.12f, 0.12f);
                Add("Spray", R(0.015f, 0.09f), new Vector2(R(-0.4f, 0.5f), y), new Vector2(R(0.5f, 1.5f), y * 0.7f), R(0.19f, 0.37f), R(0.013f, 0.033f), R(-4, 4), R(0.23f, 0.36f), Gold);
            }
            Add("Edge", 0.01f, new Vector2(-0.89f, 0), new Vector2(-0.35f, 0), 0.43f, 0.036f, 0, 0.115f, Ivory);
            Mist(3, 0.03f, new Vector2(0.14f, 0), 0.2f, 0.38f);
        }
        private void Cast()
        {
            Main(0.59f, Vector2.zero, Vector2.zero);
            // Three independently travelling strings; no complete scene plate or full-screen overlay.
            for (int lane = 0; lane < 3; lane++)
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                float x = (lane - 1) * 0.55f + Mathf.Sin(t * 5f + lane) * 0.13f;
                Add("Spray", 0.025f + t * 0.23f, new Vector2(x, 0.1f + t * 2.1f), new Vector2(0.05f * (lane - 1), 0.6f), 0.27f, 0.026f, 90 + R(-5, 5), 0.24f, Gold);
            }
            for (int i = 0; i < 8; i++) Add("Glint", R(0.09f, 0.3f), new Vector2(R(-1.1f, 1.1f), R(0.2f, 1.6f)), new Vector2(0, 0.65f), R(0.06f, 0.11f), R(0.025f, 0.05f), R(-25, 25), 0.29f, Ivory);
        }
        private void Area()
        {
            Main(0.92f, new Vector2(3.8f, 0), new Vector2(-8.2f, -0.06f));
            for (int i = 0; i < 26; i++)
            {
                float t = i / 25f;
                Add("Spray", 0.035f + t * 0.48f, new Vector2(3.8f - t * 7.8f, R(0.0f, 0.6f)), new Vector2(R(-3.4f, -1.6f), R(0.2f, 0.8f)), R(0.24f, 0.58f), R(0.035f, 0.1f), R(-15, 25), R(0.28f, 0.45f), Gold);
            }
            Mist(7, 0.13f, new Vector2(-0.2f, 0.08f), 3.4f, 1.2f);
        }
        private void Point()
        {
            Main(0.23f, Vector2.zero, new Vector2(-0.18f, 0.14f));
            Add("Edge", 0, Vector2.zero, Vector2.zero, 0.3f, 0.045f, 16, 0.095f, Ivory);
            Add("Spray", 0, Vector2.zero, new Vector2(-0.9f, 0.85f), 0.54f, 0.105f, -45, 0.22f, Gold);
            Add("Spray", 0.008f, Vector2.zero, new Vector2(0.65f, -0.35f), 0.44f, 0.065f, 26, 0.21f, Gold);
            Add("Spray", 0.008f, Vector2.zero, new Vector2(0.15f, 0.95f), 0.37f, 0.065f, 82, 0.20f, Gold);
            for (int i = 0; i < 9; i++)
            {
                float a = R(25, 305) * Mathf.Deg2Rad;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Add("Spray", R(0.015f, 0.04f), v * 0.08f, v * R(1f, 2.1f), R(0.11f, 0.23f), R(0.025f, 0.055f), a * Mathf.Rad2Deg, R(0.25f, 0.41f), Gold);
            }
            Mist(3, 0.02f, Vector2.zero, 0.15f, 0.42f);
        }
        private void Mist(int count, float time, Vector2 origin, float radius, float size)
        {
            for (int i = 0; i < count; i++)
                Add("Mist", time + i * 0.01f, origin + new Vector2(R(-radius, radius), R(-radius * 0.3f, radius * 0.3f)), new Vector2(R(-0.12f, 0.22f), R(0.08f, 0.25f)), R(0.8f, 1.2f) * size, size * R(0.4f, 0.65f), R(-25, 25), R(0.36f, 0.45f), Shadow);
        }
        private void Guard()
        {
            Main(0.43f, Vector2.zero, Vector2.zero);
            Add("Edge", 0.04f, new Vector2(-0.92f, 0), new Vector2(0.18f, 0), 0.38f, 0.085f, 90, 0.14f, Ivory);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.12f + i * 0.18f;
                for (int side = -1; side <= 1; side += 2)
                    Add("Spray", 0.05f + i * 0.018f, new Vector2(-0.82f, side * y), new Vector2(-0.62f, side * 0.75f), 0.18f, 0.04f, side * 45, 0.32f, Gold);
            }
            Mist(3, 0.04f, new Vector2(-0.62f, 0), 0.24f, 0.50f);
        }
        private void AnimateBody()
        {
            ParticleSystem body = roles["Body"];
            int count = body.GetParticles(bodyParticles);
            for (int i = 0; i < count; i++)
            {
                float age = bodyParticles[i].startLifetime - bodyParticles[i].remainingLifetime;
                float sweep = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / 0.14f), 3f);
                if (recipe == "Slash") bodyParticles[i].rotation3D = new Vector3(0, 0, Mathf.Lerp(82f, 0f, sweep));
                else if (recipe == "Cast") bodyParticles[i].rotation3D = new Vector3(0, 0, Mathf.Lerp(-8f, 6f, age / 0.6f));
            }
            body.SetParticles(bodyParticles, count);
        }
    }
}
