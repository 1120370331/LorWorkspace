using UnityEngine;

namespace Steria
{
    /// <summary>
    /// 震动配置
    /// </summary>
    public struct ShakeConfig
    {
        public float X;
        public float Y;
        public float Speed;
        public float Duration;

        public static ShakeConfig None => new ShakeConfig { X = 0, Y = 0, Speed = 0, Duration = 0 };
        public static ShakeConfig Light => new ShakeConfig { X = 0.008f, Y = 0.008f, Speed = 40f, Duration = 0.2f };
        public static ShakeConfig Medium => new ShakeConfig { X = 0.012f, Y = 0.012f, Speed = 50f, Duration = 0.25f };
        public static ShakeConfig Heavy => new ShakeConfig { X = 0.018f, Y = 0.015f, Speed = 60f, Duration = 0.3f };
        public static ShakeConfig Horizontal => new ShakeConfig { X = 0.02f, Y = 0.005f, Speed = 80f, Duration = 0.2f };
        public static ShakeConfig Vertical => new ShakeConfig { X = 0.005f, Y = 0.02f, Speed = 60f, Duration = 0.25f };

        public ShakeConfig(float x, float y, float speed, float duration)
        {
            X = x;
            Y = y;
            Speed = speed;
            Duration = duration;
        }

        public bool IsValid => X > 0 || Y > 0;
    }

    /// <summary>
    /// 特效挂载目标
    /// </summary>
    public enum EffectTarget
    {
        Self,       // 挂载到攻击者
        Target,     // 挂载到目标
        World       // 世界坐标
    }

    /// <summary>
    /// 攻击类型（用于选择正确的pivot）
    /// </summary>
    public enum EffectActionType
    {
        None,       // 使用默认atkEffectRoot
        Slash,      // 斩击 (J)
        Penetrate,  // 突刺 (Z)
        Hit         // 打击 (H)
    }

    /// <summary>
    /// 单个Quad特效的配置
    /// </summary>
    public class QuadEffectConfig
    {
        public string TextureName;
        public Vector3 LocalPosition;
        public float RotationZ;
        public float BaseScale;
        public float ScaleMultiplierStart = 0.3f;
        public float ScaleMultiplierEnd = 1.1f;
        public float AspectRatio = 16f / 9f;
        public bool AnimateScale = true;
        public bool AnimatePosition = false;
        public Vector3 PositionOffset = Vector3.zero;
        public Color Tint = Color.white;
        public float BrightnessThreshold = 0.15f;

        public Vector3 GetStartScale()
        {
            return new Vector3(BaseScale * AspectRatio, BaseScale, 1f) * ScaleMultiplierStart;
        }

        public Vector3 GetEndScale()
        {
            return new Vector3(BaseScale * AspectRatio, BaseScale, 1f) * ScaleMultiplierEnd;
        }
    }

    /// <summary>
    /// 完整特效配置
    /// </summary>
    public class SteriaEffectConfig
    {
        public string EffectName;
        public float Duration = 1f;
        public float FadeInEnd = 0.15f;
        public float FadeOutStart = 0.75f;
        public float MaxAlpha = 1f;
        public EffectTarget Target = EffectTarget.Self;
        public EffectActionType ActionType = EffectActionType.None;  // 攻击类型，用于选择pivot
        public Vector3 RootOffset = new Vector3(0f, 0f, -0.5f);  // 改为0,0,-0.5，让pivot决定位置
        public ShakeConfig Shake = ShakeConfig.Medium;
        public QuadEffectConfig[] Quads;

        #region 预设配置

        /// <summary>
        /// 水系斩击配置
        /// </summary>
        public static SteriaEffectConfig WaterSlash => new SteriaEffectConfig
        {
            EffectName = "WaterSlash",
            Duration = 0.45f,
            MaxAlpha = 1.0f,
            Target = EffectTarget.Self,
            ActionType = EffectActionType.Slash,  // 使用斩击pivot
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.01f, 0.012f, 40f, 0.3f),
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_slash",
                    LocalPosition = new Vector3(0f, 0f, 0f),  // pivot已经在正确位置
                    RotationZ = 0f,
                    BaseScale = 1.875f,
                    ScaleMultiplierStart = 0.5f,
                    ScaleMultiplierEnd = 1.3f,
                    AnimateScale = false,
                    AnimatePosition = true,
                    PositionOffset = new Vector3(0f, -0.3f, 0f),
                    Tint = new Color(0.9f, 1f, 1.1f)
                }
            }
        };

        /// <summary>
        /// 水系打击配置
        /// </summary>
        public static SteriaEffectConfig WaterHit => new SteriaEffectConfig
        {
            EffectName = "WaterHit",
            Duration = 0.45f,
            MaxAlpha = 1.2f,
            Target = EffectTarget.Self,
            ActionType = EffectActionType.Hit,  // 使用打击pivot
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.012f, 0.015f, 50f, 0.25f),
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_slash",
                    LocalPosition = new Vector3(0f, 0f, 0f),  // pivot已经在正确位置
                    RotationZ = -90f,
                    BaseScale = 3.6f,
                    ScaleMultiplierStart = 0.3f,
                    ScaleMultiplierEnd = 1.1f,
                    AnimateScale = false,
                    Tint = new Color(0.9f, 1f, 1.1f)
                }
            }
        };

        /// <summary>
        /// 水系突刺配置
        /// </summary>
        public static SteriaEffectConfig WaterPenetrate => new SteriaEffectConfig
        {
            EffectName = "WaterPenetrate",
            Duration = 0.4f,
            MaxAlpha = 1.3f,
            Target = EffectTarget.Self,
            ActionType = EffectActionType.Penetrate,  // 使用突刺pivot
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.018f, 0.005f, 80f, 0.2f),
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_slash",
                    LocalPosition = new Vector3(0f, 0f, 0f),  // pivot已经在正确位置
                    RotationZ = 5f,
                    BaseScale = 3.96f,
                    AspectRatio = 16f / 9f,
                    ScaleMultiplierStart = 0.4f,
                    ScaleMultiplierEnd = 1.2f,
                    AnimateScale = false,
                    AnimatePosition = true,
                    PositionOffset = new Vector3(0f, 0f, 0f),  // 不需要位置偏移了
                    Tint = new Color(0.9f, 1f, 1.1f)
                }
            }
        };

        /// <summary>
        /// 水系远程打击配置（拙劣控流用）
        /// </summary>
        public static SteriaEffectConfig WaterFarHit => new SteriaEffectConfig
        {
            EffectName = "WaterFarHit",
            Duration = 0.45f,
            MaxAlpha = 1.2f,
            Target = EffectTarget.Target,  // 挂载到目标身上
            ActionType = EffectActionType.Hit,
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.01f, 0.01f, 40f, 0.2f),
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_far_hit",
                    LocalPosition = new Vector3(0f, 0.3f, 0f),
                    RotationZ = 0f,
                    BaseScale = 2.5f,
                    ScaleMultiplierStart = 0.4f,
                    ScaleMultiplierEnd = 1.2f,
                    AnimateScale = true,
                    Tint = new Color(0.9f, 1f, 1.1f)
                }
            }
        };

        /// <summary>
        /// 水系突刺配置（使用water_pierce图片）
        /// 从人物向外部渐入渐出
        /// </summary>
        public static SteriaEffectConfig WaterPierce => new SteriaEffectConfig
        {
            EffectName = "WaterPierce",
            Duration = 0.4f,
            FadeInEnd = 0.2f,
            FadeOutStart = 0.6f,
            MaxAlpha = 1.3f,
            Target = EffectTarget.Self,
            ActionType = EffectActionType.Penetrate,
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.018f, 0.005f, 80f, 0.2f),
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_pierce",
                    LocalPosition = new Vector3(-1.5f, 0f, 0f),  // 起始位置在人物身上
                    RotationZ = 180f,  // 翻转方向
                    BaseScale = 3.5f,
                    AspectRatio = 16f / 9f,
                    ScaleMultiplierStart = 1f,
                    ScaleMultiplierEnd = 1f,
                    AnimateScale = false,
                    AnimatePosition = true,
                    PositionOffset = new Vector3(2.5f, 0f, 0f),  // 向外移动
                    Tint = new Color(0.9f, 1f, 1.1f)
                }
            }
        };

        /// <summary>
        /// 水系环绕特效（自我之流用）
        /// 渐入渐出，不放大
        /// </summary>
        public static SteriaEffectConfig WaterSurround => new SteriaEffectConfig
        {
            EffectName = "WaterSurround",
            Duration = 0.5f,
            FadeInEnd = 0.2f,
            FadeOutStart = 0.7f,
            MaxAlpha = 1.5f,
            Target = EffectTarget.Self,
            ActionType = EffectActionType.None,
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.015f, 0.015f, 50f, 0.3f),
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_surround",
                    LocalPosition = new Vector3(0f, 0.5f, 0f),
                    RotationZ = 0f,
                    BaseScale = 4f,
                    ScaleMultiplierStart = 1f,
                    ScaleMultiplierEnd = 1f,
                    AnimateScale = false,
                    Tint = new Color(0.8f, 0.95f, 1.2f)
                }
            }
        };

        /// <summary>
        /// 风系斩击配置
        /// </summary>
        public static SteriaEffectConfig WindSlash => new SteriaEffectConfig
        {
            EffectName = "WindSlash",
            Duration = 0.5f,
            FadeInEnd = 0f,
            FadeOutStart = 0.3f,
            MaxAlpha = 1.5f,
            Target = EffectTarget.Self, // 会在代码中特殊处理
            RootOffset = Vector3.zero,
            Shake = new ShakeConfig(0.02f, 0.01f, 70f, 0.3f),
            Quads = new[]
            {
                // 攻击者身边的纵向斩击
                new QuadEffectConfig
                {
                    TextureName = "wind_slash",
                    LocalPosition = new Vector3(0.5f, 0.5f, -0.3f),
                    RotationZ = 90f,
                    BaseScale = 1.8f,
                    ScaleMultiplierStart = 0.3f,
                    ScaleMultiplierEnd = 1.1f,
                    AnimateScale = true
                }
            }
        };

        /// <summary>
        /// 暗紫色斩击配置（忘却之梦用）
        /// 持续时间较长，帅气的暗紫色特效
        /// </summary>
        public static SteriaEffectConfig DarkPurpleSlash => new SteriaEffectConfig
        {
            EffectName = "DarkPurpleSlash",
            Duration = 0.7f,  // 较长持续时间
            FadeInEnd = 0.1f,
            FadeOutStart = 0.5f,
            MaxAlpha = 1.8f,
            Target = EffectTarget.Self,
            ActionType = EffectActionType.Slash,
            RootOffset = new Vector3(0f, 0f, -0.5f),
            Shake = new ShakeConfig(0.025f, 0.02f, 60f, 0.4f),  // 较强震动
            Quads = new[]
            {
                new QuadEffectConfig
                {
                    TextureName = "water_slash",  // 复用水系斩击图片
                    LocalPosition = new Vector3(0f, 0f, 0f),
                    RotationZ = 0f,
                    BaseScale = 2.5f,  // 较大尺寸
                    ScaleMultiplierStart = 0.4f,
                    ScaleMultiplierEnd = 1.5f,
                    AnimateScale = true,
                    AnimatePosition = true,
                    PositionOffset = new Vector3(0f, -0.4f, 0f),
                    Tint = new Color(0.6f, 0.3f, 0.9f)  // 暗紫色
                }
            }
        };

        #endregion
    }
}
