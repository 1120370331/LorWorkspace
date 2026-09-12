# 克丽丝塔夏粒子特效 V2：交付约定

## 用户目标

制作原创、可变、风格接近《废墟图书馆》的真正粒子特效。以克丽丝塔夏新版斩击、打击、近/远突刺、汐音群攻完成从研究、制作、预览到书页部署的完整流程，弃用旧版，并留下能够覆盖挥舞、突刺、打击、防御、范围、特殊、点位的制作指南。

## 已核实边界

- 源代码基线 HEAD：`440d8ba3d24c005ced2de81bf9c4bdd5a3de6652`；开始时工作区已有其他角色/机制改动，必须保留。相关文件的任务开始快照在 `output/christasha-particle-v2/baseline/`。
- 旧 `Steria_ChristashaDawn*` 共有 46 个 `EffectRes` 绑定，跨 23 张书页：SlashH 19，SlashV 9，Hit 2，PierceNear 8，PierceFar 8。
- `9009006 汐音：光耀万海` 是 FarAreaEach，当前 ActionScript 为空；需要独立动作入口。复制骰子的能力、伤害、威力、音符/乐章机制不变。
- 其余五张含 Xiao_610005 / Xiao_610001 的特殊群攻保留其已有 ActionScript。普通 EffectRes 迁移不能宣称这些动作的全部特效已经重制。
- 旧特效只供查明迁移入口，不作为新版资产或视觉基线。原版及寒昼资源只供研究，不进入发布包。
- 技能生产预设搜索 `gold particle sweeping slash` 无可用 production；用户已明确要求自定义制作，新版按原创制作路线进行，不修改技能资产库。

## 目标环境

- 精确 Unity 2019.3.15f1 / Built-in / Gamma；`C:/Program Files/Unity/Editor/Unity.exe`。
- 本轮隔离空项目已成功完成导入并 exit 0，日志 `output/christasha-particle-v2/environment/unity2019-create.log`。进程级 `OPENSSL_ia32cap=:~0x20000000`，未修改许可证。
- 实际游戏模组：`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。C 盘 Steam 入口可能是同一路径别名，部署按解析后真实路径去重。

## 验收矩阵

| 结果 | 必须证据 | 拒绝条件 |
|---|---|---|
| 原版参考 | 实际动作/资源映射、粒子结构与材质时序 | 仅凭名称或静态 PNG 猜测罗兰实现 |
| 新攻击 | 真实 ParticleSystem，斩/击/刺大形可区分，两面向及目标挂点正确 | 把单图淡出称为粒子；沿用旧版；亮团遮挡主形 |
| 汐音群攻 | 起势/范围/命中点职责独立，真实 manager 契约检查，连续骰子可完成 | 自行结算伤害、无命中不结束、重复回调重复生成、等到 manager 超时 |
| 可复用性 | 七类配方/参数/预算、原创输入与一键重建和预览命令 | 仅记录本次调参，没有新增效果方法 |
| 视觉 | 原生 Unity GPU 早/中/峰/退场、正常速度、独立 PASS | 编译或 CPU 示意图替代视觉接受 |
| 部署 | 新 Release DLL、AB/资源、XML 与候选哈希相同，回滚备份 | 仅复制 C 盘旧路径、漏 DLL 或新资源、覆盖无关未提交工作 |

## 责任与阶段

1. 专职研究设计代理提交设计，主线程审查并冻结接口。
2. 独立实现代理负责新版资产/粒子驱动与 Unity 构建预览；游戏接入由另一独占文件面的实现代理完成。
3. 第三位代理独立预览判断 PASS / REVISE / BLOCK；缺陷回到原 owner。
4. 主线程亲自读差异、核对候选、运行最终验证与部署，并记录精确候选及已验证/未验证边界。

本文件是交付约定，不是已通过的验证报告。
