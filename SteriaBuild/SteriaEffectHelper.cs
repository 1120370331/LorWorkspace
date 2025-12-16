using System;
using UnityEngine;

namespace Steria
{
    /// <summary>
    /// 特效工具类 - 提供通用的特效辅助方法
    /// </summary>
    public static class SteriaEffectHelper
    {
        #region 屏幕震动

        /// <summary>
        /// 添加屏幕震动效果
        /// </summary>
        public static void AddScreenShake(float x = 0.01f, float y = 0.01f, float speed = 50f, float duration = 0.3f)
        {
            try
            {
                BattleCamManager instance = SingletonBehavior<BattleCamManager>.Instance;
                if (instance != null && instance.EffectCam != null)
                {
                    var shake = instance.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>();
                    if (shake != null)
                    {
                        shake.X = x;
                        shake.Y = y;
                        shake.Speed = speed;

                        var autoDestroy = instance.EffectCam.gameObject.AddComponent<AutoScriptDestruct>();
                        if (autoDestroy != null)
                        {
                            autoDestroy.targetScript = shake;
                            autoDestroy.time = duration;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Steria] Could not add screen shake: {ex.Message}");
            }
        }

        /// <summary>
        /// 轻微震动 - 适用于普通攻击
        /// </summary>
        public static void AddLightShake()
        {
            AddScreenShake(0.008f, 0.008f, 40f, 0.2f);
        }

        /// <summary>
        /// 中等震动 - 适用于斩击/突刺
        /// </summary>
        public static void AddMediumShake()
        {
            AddScreenShake(0.012f, 0.012f, 50f, 0.25f);
        }

        /// <summary>
        /// 强烈震动 - 适用于打击/特殊攻击
        /// </summary>
        public static void AddHeavyShake()
        {
            AddScreenShake(0.018f, 0.015f, 60f, 0.3f);
        }

        /// <summary>
        /// 水平震动 - 适用于突刺类攻击
        /// </summary>
        public static void AddHorizontalShake()
        {
            AddScreenShake(0.02f, 0.005f, 80f, 0.2f);
        }

        /// <summary>
        /// 垂直震动 - 适用于打击类攻击
        /// </summary>
        public static void AddVerticalShake()
        {
            AddScreenShake(0.005f, 0.02f, 60f, 0.25f);
        }

        #endregion

        #region 缓动函数

        /// <summary>
        /// 二次缓出
        /// </summary>
        public static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// 二次缓入
        /// </summary>
        public static float EaseInQuad(float t)
        {
            return t * t;
        }

        /// <summary>
        /// 三次缓出
        /// </summary>
        public static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>
        /// 三次缓入
        /// </summary>
        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// 三次缓入缓出
        /// </summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>
        /// 弹性缓出
        /// </summary>
        public static float EaseOutElastic(float t)
        {
            if (t == 0f || t == 1f) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI) / 3f) + 1f;
        }

        /// <summary>
        /// 回弹缓出
        /// </summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        #endregion

        #region Quad创建

        /// <summary>
        /// 创建特效Quad并设置材质
        /// </summary>
        public static GameObject CreateEffectQuad(string name, Material material, Transform parent,
            Vector3 localPosition, float rotationZ, Vector3 scale, int sortingOrder = 100)
        {
            if (material == null) return null;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;

            // 移除碰撞体
            var collider = quad.GetComponent("MeshCollider") as Component;
            if (collider != null) UnityEngine.Object.Destroy(collider);

            // 设置渲染器
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.material = new Material(material);
            renderer.sortingOrder = sortingOrder;

            // 设置变换
            quad.transform.SetParent(parent);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            quad.transform.localScale = scale;

            return quad;
        }

        /// <summary>
        /// 计算带宽高比的缩放
        /// </summary>
        public static Vector3 CalculateScale(float baseScale, float aspectRatio = 16f / 9f)
        {
            return new Vector3(baseScale * aspectRatio, baseScale, 1f);
        }

        #endregion

        #region 透明度计算

        /// <summary>
        /// 计算渐入渐出透明度
        /// </summary>
        /// <param name="progress">当前进度 0-1</param>
        /// <param name="fadeInEnd">渐入结束点 (0-1)</param>
        /// <param name="fadeOutStart">渐出开始点 (0-1)</param>
        /// <param name="maxAlpha">最大透明度</param>
        public static float CalculateFadeAlpha(float progress, float fadeInEnd = 0.15f, float fadeOutStart = 0.75f, float maxAlpha = 1f)
        {
            if (progress < fadeInEnd)
            {
                return Mathf.Lerp(0f, maxAlpha, progress / fadeInEnd);
            }
            else if (progress > fadeOutStart)
            {
                return Mathf.Lerp(maxAlpha, 0f, (progress - fadeOutStart) / (1f - fadeOutStart));
            }
            return maxAlpha;
        }

        /// <summary>
        /// 设置Additive材质的颜色/透明度
        /// </summary>
        public static void SetAdditiveMaterialAlpha(Material material, float alpha, Color? tint = null)
        {
            if (material == null) return;

            Color baseColor = tint ?? Color.white;
            Color color = new Color(baseColor.r * alpha, baseColor.g * alpha, baseColor.b * alpha, alpha);
            material.SetColor("_TintColor", color * 0.5f);
        }

        #endregion
    }
}
