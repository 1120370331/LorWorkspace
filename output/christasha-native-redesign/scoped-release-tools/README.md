# 克丽丝塔夏原生粒子版：限定范围的发布准备工具

本目录是发布工具与安装基线快照。当前 `release-config.json` 为 **frozen=false**，新适配器及原创笔触输入未冻结。此次只执行了 `plan`，没有提取构建候选、编译、部署、暂存或提交；没有桌面、Unity、游戏或 Steam 操作。

## 已核对且主线程接受的安装基线

当前安装并非最初的 f76433e0 版本。同期音效工作提交了 `312fcc18d83f7346239a71d6ed46ac4dcc875fbd`，其提交 DLL 与安装 DLL 均为 `4ba01608ce09fc598e553f2b41a8d0d919b2c64b497620c4ec19706fc1ae7762`。现安装 XML 为 `ab091603d7aa8739e456dd731d01fcf3efa1b1af1c65f30231825b171fcfaeb5`，见 `output/memory-flame-delivery/deployment.json`。

音效 DLL 是完整工作区构建，仅用提交源码还不足以重建其已安装行为。已用匹配该 DLL 的 Portable PDB 核对 84 份源码文档，精确冻结三份额外已安装源码：

- `HarmonyPatches.cs`
- `DiceAttackEffect_Steria_AnhierTextureCombat.cs`
- `BehaviourAction_Steria_MemoryConcerto.cs`

它们的冻结副本在 `baseline-installed/`，SHA256 与 PDB 文档校验和一致。原 PDB 的第四份提交外源码是本任务拥有、后续整体替换的 `ChristashaNativeParticleLibrary.cs`。详细证据见 `installed-pdb-sources.json`、`baseline-resolution.json`。因此这些补充是保存已安装成果，不是盲目发布当前工作区。

`baseline-installed/CardInfo.xml` 是安装文件的逐字节副本；相对 312 提交 XML，它只保留音效发布已记录的 9001004 三个 `ActionScript`。配置明确列出这三项，工具会验证语义差异，不能悄悄扩大。工作区其余卡数据不进入候选；本次 dry-run 相对当前已安装基线识别出 **30** 项额外工作区字段变化（最初的 33 项中，三项已由音效工作正式部署）。

## 用法

```powershell
python output/christasha-native-redesign/scoped-release-tools/scoped_release.py plan
```

该命令只读安装、提交和显式输入，写 `dry-run-plan.json`。所有其他命令在没有 `--execute` 时同样只生成计划。

主线程接受最终 library、三个 adapter、Mass/Pierce recipe 文件和原创笔触后，填写 `release-config.json` 中各输入的 SHA256、精确资产路径，清空 `pending_inputs` 并设置 `frozen=true`。默认保留 XML、Harmony 注册和项目引用；只有确有必要时才填写限定的属性/行补丁与 Unity 引用。**不要把完整工作区 csproj、CardInfo.xml 或 HarmonyPatches.cs 改为 overlay。**

以下为后续执行命令，本次没有运行：

```powershell
python output/christasha-native-redesign/scoped-release-tools/scoped_release.py prepare --execute
python output/christasha-native-redesign/scoped-release-tools/scoped_release.py build --execute
python output/christasha-native-redesign/scoped-release-tools/scoped_release.py deploy --execute
```

可用 `--config <path>` 指定另一个审查后的配置。`candidate_directory` 必须位于 `output/christasha-native-redesign/`；同配置、无漂移候选可复用。需要新候选时指定新目录，不递归清空旧候选。

`assets` 每项格式为：

```json
{"source":"SteriaBuild/VFXSource/ChristashaNative/Assets/实际冻结文件.png","destination":"Resource/Effects/ChristashaNative/实际运行时文件.png","sha256":"64位已验收SHA256"}
```

这只是格式示例，不是已确认的资源文件名。仅允许该原创资产面下显式 PNG/JSON；不允许导出的原版参考包、工具 bootstrap、旧 V2 AB 或 evidence 目录。当前已记录六个显式资产及其 hash：一张原创 brush、Slash、Hit、Penetrate、Slash2、S1；这些条目已通过当前 workspace hash 核对，仍受 C# overlay 与 frozen=false 门禁约束。

## 候选与部署保证

准备从 **提交 blob** 提取代码、项目文件、Libs 引用和嵌入资源，再覆盖已安装源码快照及本任务四个显式 C# 输入。生成的候选单独包含 package XML/原创资产。工作区其他文件不参与；共享 Harmony 只从冻结的已安装快照起步。旧 `ChristashaParticleVisual.cs` 从候选移除，准备门禁会拒绝剩余旧 V2 类或 AB 依赖。

`prepared.json` 固定全部源码、资源、项目及外部原生 DLL hash；后续检查也拒绝未列入的新候选文件。Release 只对该隔离项目编译，并产生 `build.json`。本工具不会构建当前工作区项目。

部署只写配置中的真实游戏 Mod 根，不改仓库包的工作区 XML。它会重新校验安装 DLL/XML、所有将写入/退役文件的先前 hash，以及冻结候选，完整备份后逐文件采用同目录临时文件、flush/fsync、`os.replace` 原子换入，最后复核全部文件。这是**逐文件原子操作加失败回滚**，不是跨多文件的文件系统事务。新资源/XML 在 DLL 之前换入；旧 V2 AB 按精确四个路径退役。

每一步更新 `deployments/<timestamp>/receipt.json`，记录 before/after/hash/backup。失败会尝试恢复已处理文件；发现第三方后续修改会保留并明确记录 `ROLLBACK_BLOCKED`。再次部署同一已部署且无漂移候选直接报告匹配。

手动回滚（本次未运行）：

```powershell
python output/christasha-native-redesign/scoped-release-tools/scoped_release.py rollback --receipt "实际候选目录/deployments/时间戳/receipt.json" --execute
```

不会修改 git index 或提交；原旧脚本的递归 `git add` 不沿用。主线程后续应按本任务准确文件/共享行差异收口，不能把本目录的已安装基线补充当成本次 VFX 改动一并暂存。
