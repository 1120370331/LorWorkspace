using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using HarmonyLib;
using UnityEngine;

namespace Steria
{
    /// <summary>
    /// Harmony patch让游戏能够加载自定义特效类
    /// </summary>
    [HarmonyPatch(typeof(DiceEffectManager))]
    [HarmonyPatch("CreateBehaviourEffect")]
    public static class DiceEffectManagerPatch
    {
        // 自定义特效类型注册表
        private static Dictionary<string, Type> _customEffects = new Dictionary<string, Type>();

        static DiceEffectManagerPatch()
        {
            // 注册自定义特效
            RegisterEffect("VeliaThorn_F", typeof(DiceAttackEffect_VeliaThorn_F));
        }

        public static void RegisterEffect(string name, Type effectType)
        {
            _customEffects[name] = effectType;
            SteriaLogger.Log($"Registered custom effect: {name}");
        }

        [HarmonyPrefix]
        public static bool Prefix(string resource, float scaleFactor, BattleUnitView self, BattleUnitView target, float time, ref DiceAttackEffect __result)
        {
            if (string.IsNullOrEmpty(resource))
            {
                __result = null;
                return false;
            }

            // 检查是否是自定义特效
            if (_customEffects.TryGetValue(resource, out Type effectType))
            {
                try
                {
                    SteriaLogger.Log($"Creating custom effect: {resource}");

                    // 创建GameObject并添加特效组件
                    GameObject go = new GameObject("CustomEffect_" + resource);
                    DiceAttackEffect effect = (DiceAttackEffect)go.AddComponent(effectType);

                    effect.Initialize(self, target, time);
                    effect.SetScale(scaleFactor);

                    __result = effect;
                    SteriaLogger.Log($"Custom effect created: {resource}");
                    return false; // 跳过原方法
                }
                catch (Exception ex)
                {
                    SteriaLogger.Log($"Error creating custom effect {resource}: {ex}");
                }
            }

            // 不是自定义特效，让原方法处理
            return true;
        }
    }
}
