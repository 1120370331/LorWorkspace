# 原版特效参考导出

`native_reference_export.py` 将用户本地《废墟图书馆》原始序列化对象导出为一个仅供研究的 UnityFS bundle。保留原 PS、mesh、纹理、Animator/曲线和编译 shader；不重画，不替换 shader，不访问存档或启动游戏。

在仓库根运行：

```powershell
python SteriaBuild/VFXSource/ChristashaNative/tools/native_reference_export.py
```

默认使用已知本机 D 盘 `LibraryOfRuina_Data`，输出至 `output/christasha-native-redesign/native-reference-export/`。需要指定环境时：

```powershell
python SteriaBuild/VFXSource/ChristashaNative/tools/native_reference_export.py --game-data "D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data" --output-dir "output/christasha-native-redesign/native-reference-export" --python-deps "output/music-dice-ui/python-deps"
```

依赖固定为 **UnityPy 1.25.3**。可在自己的 Python 环境安装本目录 `requirements.txt`，也可传 `--python-deps`；仓库已知的依赖目录仅为可选本机快捷路径，不是必需 source。`--repo-root` 可覆盖自动向上寻找含 `SteriaBuild` 的根目录。路径、依赖版本或本地游戏资源版本不匹配时会明确报错。

稳定产物：

- `native_reference_christasha_sources`
- `sources-import-receipt.json`：七项 assetname、对象原始/输出 hash、纹理 streaming 区间、剥离的脚本、包 hash、是否与主线程已 GPU 验证的参考包逐字节相同。

七项为 Slash_Down、Dagger、Greatsword、GloveTrail_cross、Xiao_AreaTrail1、Orchestra_start、ShadowSlsah（保留原版拼写），均位于 `assets/native_reference/`。当前已核实本地资源版本为 Unity **2019.3.15f1**；工具按这一版本的确切对象 ID 校验名称，不猜测其他游戏版本的偏移。

## 随工具保留的结构模板

`unity2019_schema_only.bundle` 仅含 **AssetBundle pathID 1** 这一对象、9 个原生 TypeTree 和包头。没有 Texture、Mesh、ParticleSystem、Shader 对象或编译程序，没有旧特效像素，Container/Preload 均为空。`bootstrap-schema.json` 记录其结构和 hash。它由项目自有 Unity 2019.3.15f1 bundle 剪除全部视觉对象后得到，不再依赖旧版特效 AB、ignored UnityProject、活动游戏模组目录或 output 生成缓存。

导出时，共有原生类型采用模板中同类型 hash 的真实 TypeTree；其余类型采用 UnityPy 对该 Unity 版本的原生类数据库树。保持原始类型 hash、SerializedFile v21、Windows64 platform19。

**AssetBundle 主对象必须为 pathID 1。** 后台 Unity 的五组最小差分实验已确认：仅将此前主对象的其他 pathID 改为 1，完整原版参考包即由兼容性拒绝变为可加载。不能把该问题误归因于 shader、UnityPy 或许可证。

全部非零 PPtr 在包内闭合，纹理只按需读取用户本地 resS 字节区间并内联；不会整体复制巨大 resS。剥离 MonoBehaviour/MonoScript 及其组件槽，避免运行游戏逻辑。原始动作脚本没有回放，因此参考是 prefab 的隔离重放，并非完整战斗流程。

原游戏像素和参考 bundle 仅写入输出目录，禁止进入生产资产或发布包。运行只做导出与 UnityPy 回读；若输出 hash 与 receipt 中已 GPU 验证包一致，可复用该证据；不一致时不能仅凭序列化检查宣称 GPU 通过。
