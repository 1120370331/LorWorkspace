# 乐章小骰：获批 AfterIcon 材质作者流程 v1

生产使用 `SteriaBuild/VisualAssets/MusicDice/{Card,BlankFrame,Glyph}.png`，由既有程序集 manifest loader 解码缓存。运行时源码保持 `92036e71316bd7c19012999dc911c14bbae4155291f1ba13e2a9fff02a4e3176`；没有实时 SDF、网络生成或外部图片加载。

## 唯一默认再生入口

前置：现有隔离 Unity 2019 Gamma 项目，带 Unity UI 与 `Assets/Editor`。本工作区默认使用 `output/music-dice-ui/implementation/UnityProject`。执行：

```powershell
$env:OPENSSL_ia32cap=':~0x20000000'
& ./SteriaBuild/VFXSource/MusicDiceUI/regenerate.ps1
```

可传 `-ProjectPath`、`-OutputDirectory`、`-Unity`。默认只写 `output/music-dice-ui/authoring-v1-regenerated`，不覆盖生产资产。脚本复制唯一作者实现 `MusicDiceMaterialAuthorV1.cs` 到隔离项目，隐藏启动并 WaitForExit；最后逐一核对三 PNG 是否与已审 v1 的 SHA256 一致。使用同一 Unity 环境可复现获批像素。输出包含三背景接触表、组件 CSV 和原色/清理后的局部证据。

## 固定输入与真实来源

`Authoring/v1/` 是完整版本化输入：选定 `blank-rgb.png`、原提示词、拒绝 v2 的提示词/来源记录、原版 AfterIcon 对照，以及自有 80% `Glyph.png`。`provenance.json` 记录输入及获批成品哈希；`REVIEW.md` 保存独立素材 PASS。

内置 image_gen 以真实 AfterIcon Slash/Pierce/Hit/Counter 为风格参考生成空框。v1 被选用；v2 仍是画入棋盘格的 RGB，并使外侧墨点更重，未采用。**透明度不是 image_gen 原生输出**：C# 作者流程移除外连棋盘背景，按原色、块结构和局部证据清理封闭棋盘，重算去底色 matte，再做预乘 alpha 缩采样。审阅确认的 C186 修正锁定原图 SHA，仅移除对应 105 个 checker 像素，保留 16 个有色笔触像素。真实蓝白刷痕保留。

主亮框拟合约 154.3×170 / 256，不按黑色散点或原棋盘画布撑满。原创 glyph 保持 80% 占位、128 画布，在卡页按原布局合成。`MusicDiceGlyphAuthor.cs` 保留完整原创音头/曲线/连梁与 4×采样源码，供明确修改符号时使用；默认再生采用已审的冻结 glyph 输入。

## 接入验证

再生后，另运行 `output/music-dice-ui/implementation/run_preview.ps1`，验证真实嵌入资源、RGBA 像素、当前缩小尺度、同中心、普通/Guard/Standby、稀有度与复用状态。fixture 根据资源 SHA 改变编译输入，避免 Unity 沿用旧嵌入图片。两入口均由主线程运行；文档不把素材视觉 PASS 等同于完整游戏联调。替换嵌入 PNG 后须执行 Rebuild 并核对 DLL 内 manifest 资源哈希；本次曾出现增量 build 成功但仍嵌入旧图片，因此不能只信增量构建结果。

获批 SHA256：

- Card.png：`1843c2d6d283d1ab25097f21a46bfec5bfc61d2d0908d0afdb4092f8ccbec0a1`
- BlankFrame.png：`c8e966583d9f6d3dab0db48c3af7d6a417dc2c9b4c59b31876c1a55cdb347813`
- Glyph.png：`47e28c8779ed74d86208a9a0c4e9b3e03581d2c4c2f3ac6b9197947667d0522a`

旧平面框可从历史提交 `24872a0` 查阅；本地 `History/pre-aftericon-v1/` 是未交付的实验归档，不是默认输入或生成路线。

## 主线程交付验证（2026-09-08）

重新运行上述作者入口，三 PNG 及三背景接触表均与获批候选 SHA256 一致。随后运行 Unity 2019 fixture，44 项检查通过，实际加载后三图 RGBA 最大误差为 0；主亮框 154×170，中心 79×82。主线程查看混合骰、三种卡色的普通/高亮/禁用，以及动作/伤害 alpha 1.0、0.55、0.10 预览。独立素材 review PASS 与这些一致像素一起构成本次验收；未运行完整游戏交互。

Release 完整 Rebuild 通过（0 errors，7 项既有警告）。首次增量编译沿用旧嵌入图，因此交付前改用 Rebuild，再通过实际 Steria.dll 的 GetManifestResourceStream 核对全部三图 SHA256。最终 DLL `ECF1EBEC03B2B0A9D4D7607338A56E4747E88929832D53501A581ADD7E16AE19` 已同步仓库模组与 C/D 游戏路径，C 是 D 的 junction 别名。部署备份、构建日志和校验记录保存在 `output/music-dice-ui/delivery-aftericon-20260908-192037/`。DLL 包含工作区其他已存在改动，不纳入本次素材提交。
