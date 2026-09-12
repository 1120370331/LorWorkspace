# 原版 MainTrail：已核对的纹理通道与颜色方程

范围仅为原版 `FX_APF_PC_RolRang_MainTrail`（`resources.assets:4894`）的 D3D11 `ps_4_0`、FORWARD pass、blob index 2。复用视觉 owner 已生成的 [pixel-0-2.txt](../../../../SteriaBuild/VFXSource/ChristashaNative/evidence/shader-semantics/pixel-0-2.txt)，并以原 Shader 的 `m_NameIndices`、纹理参数及 constant-buffer offset 交叉核对绑定；没有根据属性名猜公式，没有再次提取或启动渲染。

**结论：MainTex 的 R 控制覆盖，白锋来自 Noise 与 COLOR.r 的窄阈值。OutColor 控制窄锋，MainColor 控制较宽的填色；它们相加，不做二选一 lerp。MaskPower 是 alpha 阈值的减项，不是幂指数。**

## 1. 绑定证据

| 片元寄存器 | 原生绑定 | 实际使用 |
|---|---|---|
| `t0/s0` | `_Noise`，NameIndex 9 | 仅 R，坐标 `(u, 2v)` |
| `t1/s1` | `_ColorNoise`，NameIndex 10 | 仅 R，坐标 `(0.5u, 0.5v)` |
| `t2/s2` | `_MainTex`，NameIndex 11 | 仅 R，坐标 `uv * _MainTex_ST.xy + _MainTex_ST.zw` |
| `cb0[4]`，byte 64 | `_OutColor`，NameIndex 12 | 仅 RGB，乘窄锋 |
| `cb0[5]`，byte 80 | `_MainColor`，NameIndex 13 | 仅 RGB，乘噪声填色权重 |
| `cb0[6]`，byte 96 | `_MainTex_ST`，NameIndex 14 | MainTex 的 scale/offset |
| `cb0[7].x`，byte 112 | `_MaskPower`，NameIndex 15 | 从 alpha 门限减去 |
| `v1.xy` | 片元 `TEXCOORD0.xy` | 下文 `uv=(u,v)` |
| `v4.x` | 片元 `COLOR0.r` | 下文 `p`，控制擦除/窄锋位置 |

`_MainTex.g/b/a`、`_Noise.g/b/a`、`_ColorNoise.g/b/a`、两种颜色参数的 alpha、片元 `COLOR0.g/b/a` 均未参与该片元程序。`_Noise_ST`、`_ColorNoise_ST` 也没有出现在这个片元绑定表中；这里使用写死的 UV 倍率。

## 2. 与反汇编逐步对应的数学

`sat(x)=clamp(x,0,1)`。以下 `uv`、`p` 明确指**片元输入**，不额外推测尚未核对的顶点阶段变换。

```hlsl
float N = Sample(_Noise,      float2(u, 2*v)).r;
float C = Sample(_ColorNoise, float2(.5*u, .5*v)).r;
float M = Sample(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw).r;

float x = N + 1 - 2*p;
float E = max(1 - 20 * sqrt(3) * abs(x), 0);                  // 窄锋
float B = sat(1 - 1.369863 * (sqrt(3) * abs(1-x) - 1));       // 主体覆盖区
float G = sat(10 * (1 - C * abs(u)) - _MaskPower);            // 沿 U 的噪声门限
float W = sat((1.5*C) * (1.5*C) * (1.5*C));                  // 填色权重

float3 rgb = _OutColor.rgb * E + _MainColor.rgb * W;
float alpha = sat(G * M * (B + E));
```

证据定位：反汇编第 34–43 行得到 `x` 和 `sqrt(3)*abs(x)` / `sqrt(3)*abs(1-x)`；44–48 行得到 `E`、`B` 及相加；49–55 行得到 MainTex 覆盖和 `G`、最终 alpha；56–59 行得到 `W` 与最终 RGB。

两个 `dp3 r, scalar.xxxx, scalar.xxxx` 都是 `3*scalar²`，因此后续 `sqrt` 必须保留 `sqrt(3)*abs(scalar)`；不能误简化为一次绝对值。`1.369863` 使用编译常量；写成 `1/0.73` 仅是近似说明。

窄锋成立条件为 `abs(N+1-2*p) < 1/(20*sqrt(3)) ≈ 0.02887`，峰值位于 `p=(N+1)/2`。因此白锋位置与粒子/顶点颜色通道和 Noise 灰度有关；MainTex 不提供独立的“白锋通道”。

## 3. 颜色与 alpha 的实际意义

- `_OutColor * E` 只出现在很窄的阈值带；提高 OutColor 主要改变这条锋。
- `_MainColor * W` 在 `ColorNoise.r` 较高处形成宽填色。`C>=2/3` 时 `W=1`，这部分完整输出 MainColor。把 MainColor 设为高 HDR 会直接让广泛主体发亮，足以解释“油箔”现象，不需要归因于 mesh。
- `_MainColor=0` 时，主体覆盖 `B` 仍参与 alpha，RGB 主体为黑；OutColor 只照亮窄锋。它能产生具有遮挡力的暗体与白锋。
- `_MaskPower` 越大，`G` 越小、可见范围越受限。`G=1` 当 `10*(1-C*abs(u))-_MaskPower>=1`；`G=0` 当该值不大于 0。
- `_MainTex.r=0` 会使该片元完全透明，无论 RGB 或 Noise 多亮。Noise 决定阈值形态，ColorNoise 同时决定填色权重和 U 向 alpha 门限；三个采样各有职责。

原 pass 的 `srcBlend=5`、`destBlend=10`、`blendOp=0` 对应 `Blend SrcAlpha OneMinusSrcAlpha`；`ColorMask=14` 对应 RGB，`ZWrite=0`、`Cull=0`。最终屏幕 RGB 为 `rgb*alpha + background*(1-alpha)`，所以黑主体在 alpha 有覆盖时会压暗/遮住背景。

注意层次：原材质 `resources.assets:285` 的静态值是 MainColor=(2,2,2,0)、OutColor=(1,1,1,0)、MaskPower=3；原 shader 属性默认值又不同。主线程/视觉 owner 观察到的播放态 MPB MainColor=0、OutColor≈1.5 是另一层运行态证据，本次没有重跑该采样。不能把材质资产默认值当成动画/MPB 覆盖后的当前值。

## 4. 给新局部 mask 的最小接法

1. 将希望保留的条带/笔触覆盖写入 **MainTex 的 R**，空白区 R=0。仅有透明 alpha、RGB 仍全白的 PNG 不能提供正确覆盖，因为这个 shader 不读取纹理 alpha。
2. 若希望边缘更薄、主体更暗，优先调整 R 的覆盖比例以及原有 Noise/`p` 阈值关系；MainTex 的 G/B 不能承载额外金色或白锋标记，Shader 不会读取它们。
3. 保持 MainColor 的暗体职责，OutColor 负责锋色。不要把 ImageGen 的整幅彩色局部画面直接当作会显示原 RGB 的 MainTex；这里它会成为红通道灰度覆盖。
4. 保留原有 `p` 的时序来源再调整 Noise。该片元把 COLOR.r 当阈值控制量，而非简单颜色 tint；随意改它会移动白锋和擦除区域。
5. 原纹理的 sampler 也影响阈值：2454 MainTex 为 Bilinear + Clamp；3547 Noise 和 2100 ColorNoise 为 Bilinear + Repeat，三个原 Texture2D 的序列化 `m_ColorSpace` 都为 1。新素材导入应保持相应数值/采样语义；本次没有对新素材的导入色彩空间做实测。

## 5. 可核对范围与剩余未知

已核对：该单个 MainTrail 片元程序、其寄存器绑定、采样通道、RGB/alpha 数学和 pass 混合状态。未推断：其他 fragment keyword 变体是否逐字节相同、完整 vertex shader 的 UV/COLOR 传递、播放态 MPB 的整条时间曲线、独立 Outline 材质的规则。当前结论已足够纠正 MainTex 通道、Main/Out 方向及 MaskPower 用法，不需要扩展到其他 shader。

来源指纹：

- 原 Shader 4894 序列化对象 SHA256：`c01e5c436cb779c6abd98e2d08eaa87f1c8f0c37e420fd57b0cd759f60023e93`
- 原压缩 shader 程序 blob SHA256：`385031bed25b36dd2de43906311ae9b1278fd06c8c5220ee425845971d7cab31`
- 已有 `pixel-0-2.txt` SHA256：`10415cd1a9b1629236d7aeb3c59bc1e7e639fbc63b9ed1dd79cc72005013bd11`
