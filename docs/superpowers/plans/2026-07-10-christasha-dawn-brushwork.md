# Christasha Dawn Slash Brushwork Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the smooth plastic-looking crescent surface with a directional white-gold brush stroke while preserving the existing shape and runtime integration.

**Architecture:** Generate a dedicated RGBA brush mask whose channels encode core fibers, gold body fibers, abrasion, and soft envelope. Update the core and edge shaders to consume those channels independently, keeping bloom separate from the detailed body. Retain the current prefab, mesh, travel, and animation interfaces.

**Tech Stack:** Unity 2019.3.15f1 Editor scripts, ShaderLab/Cg, C#, PowerShell verification scripts, .NET Framework 4.7.2 mod assembly.

## Global Constraints

- Preserve the white-gold palette and existing crescent geometry.
- Do not copy Hanzhou textures, materials, prefabs, or shaders.
- Keep `ChristashaDawnSlashVerticalCoreOnlyPrefab` and runtime effect names compatible.
- Do not add runtime dependencies.

---

### Task 1: Add focused brushwork source verification

**Files:**
- Create: `SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1`

**Interfaces:**
- Consumes: `ChristashaDawnCombatBundleBuilder.cs`, `ChristashaDawn_CoreOnlyFlow.shader`, `ChristashaDawn_CoreOnlyEdgeGlow.shader`
- Produces: a focused executable source contract for directional mask generation and channel-separated shading

- [ ] **Step 1: Write the failing verification script**

The script must assert the presence of `CreateDirectionalBrushMaskTexture`, explicit RGBA channel assignments, `_BrushCoreStrength`, `_BrushFiberStrength`, `_BrushAbrasionStrength`, `_SoftEnvelopeStrength`, and removal of `CreateEnergyFlowMaskTexture`.

- [ ] **Step 2: Run the script and verify it fails**

Run:

```powershell
& SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1
```

Expected: exit code 1 with missing directional brushwork contract messages.

- [ ] **Step 3: Commit the red test only**

```powershell
git add SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1
git commit -m "test: define Christasha brushwork shader contract"
```

### Task 2: Generate a directional multi-channel brush mask

**Files:**
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Editor/ChristashaDawnCombatBundleBuilder.cs`

**Interfaces:**
- Consumes: `CreateDirectionalBrushMaskTexture(int width, int height)`
- Produces: `ChristashaDawn_BrushMask_512.png` with R=white core, G=gold fibers, B=abrasion, A=soft envelope

- [ ] **Step 1: Replace mask asset paths**

Use `Assets/Textures/Generated/ChristashaDawn_BrushMask_512.png` in generation, material creation, and build fingerprints.

- [ ] **Step 2: Implement directional mask generation**

Generate elongated path-aligned ridges, pressure variation, broken strand clusters, sparse abrasion cuts, and a broad envelope. Avoid rounded cellular contour fields.

- [ ] **Step 3: Run the focused verification**

Run:

```powershell
& SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1
```

Expected: mask-generation assertions pass; shader-property assertions may still fail until Task 3.

### Task 3: Separate core, fiber, abrasion, and glow shading

**Files:**
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Shaders/ChristashaDawn_CoreOnlyFlow.shader`
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Shaders/ChristashaDawn_CoreOnlyEdgeGlow.shader`
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Editor/ChristashaDawnCombatBundleBuilder.cs`

**Interfaces:**
- Consumes: RGBA brush mask channels
- Produces: white blade core, broken gold fibers, dark abrasion modulation, and soft low-detail outer glow

- [ ] **Step 1: Add shader properties**

Add `_BrushCoreStrength`, `_BrushFiberStrength`, `_BrushAbrasionStrength`, and `_SoftEnvelopeStrength` with conservative defaults.

- [ ] **Step 2: Replace liquid-flow brightness logic**

Sample the brush mask with path-aligned motion. Use R for the narrow white highlight, G for intermittent gold strands, B to subtract brightness and alpha locally, and A for broad body continuity.

- [ ] **Step 3: Simplify the edge glow**

Use only the soft envelope and a small amount of gold fiber energy. Blur detail through lower contrast and keep alpha below the main body.

- [ ] **Step 4: Run the focused verification**

Run:

```powershell
& SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1
```

Expected: PASS.

### Task 4: Build and inspect the generated effect

**Files:**
- Modify if required after inspection: the three Task 2-3 implementation files
- Generated: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Textures/Generated/ChristashaDawn_BrushMask_512.png`
- Generated: Christasha dawn AssetBundle outputs

**Interfaces:**
- Consumes: builder source and shaders
- Produces: deployable AssetBundle and visual evidence

- [ ] **Step 1: Build the AssetBundle**

Run:

```powershell
& SteriaBuild/VFXSource/ChristashaDawnCombat/build_and_deploy_bundle.ps1 -Force
```

Expected: Unity exits 0 and the bundle is copied to configured targets.

- [ ] **Step 2: Inspect the generated mask**

Confirm fibers run primarily along one direction, abrasion is sparse, and the alpha envelope remains continuous.

- [ ] **Step 3: Run project verification**

Run:

```powershell
& SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1
dotnet build SteriaBuild/Steria.csproj -c Release
git diff --check
```

Expected: focused verification passes, build exits 0, and diff check reports no whitespace errors.

- [ ] **Step 4: Commit the implementation**

```powershell
git add docs/superpowers SteriaBuild/VFXSource/ChristashaDawnCombat SteriaBuild/VFXSource/AnhierBlueWhiteSlash SteriaBuild/SteriaModFolder/Assemblies/AB
git commit -m "feat: refine Christasha slash brushwork"
```
