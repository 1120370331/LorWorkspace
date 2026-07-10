# Repository Instructions

## General Guidance

- Before changing code, assets, shaders, effects, build scripts, or deployment logic, first read the relevant tutorials and documentation in the repository.
- Start with root-level `README` files and the relevant files under `docs/`, then inspect nearby implementations, tests, verification scripts, preview tooling, and recent commits.
- Follow established repository patterns instead of designing from outside assumptions.
- Keep progress reports concise and task-relevant. Do not send optional commentary.

## VFX Improvement Pipeline

Whenever the user proposes a visual-effect improvement, the main agent must orchestrate the following pipeline.

### 1. Research And Effect Design

- Assign a dedicated research/design sub-agent using the latest and strongest available model.
- The agent must first read relevant repository tutorials, `docs/`, existing effect implementations, shaders, assets, preview scripts, tests, and recent commits.
- It should then search external references when useful, including comparable game effects, rendering techniques, brushwork, lighting, timing, silhouettes, and color treatment.
- The deliverable must include:
  - A diagnosis of the current effect and its root visual problems.
  - The intended silhouette, motion hierarchy, brush or material treatment, lighting, timing, and color behavior.
  - Concrete implementation guidance tied to repository files and existing systems.
  - Clear acceptance criteria for preview review.
- The main agent reviews this design for feasibility and alignment with the user's request before implementation begins.

### 2. Implementation And Preview

- Assign a separate implementation sub-agent to follow the approved effect design.
- The implementation agent must not redesign the effect independently unless it reports a concrete technical blocker.
- It is responsible for:
  - Adding or updating source verification before production changes when practical.
  - Implementing the mesh, shader, texture, animation, timing, or runtime changes.
  - Preserving established repository conventions and deployment behavior.
  - Building the effect and generating representative preview frames.
  - Reporting changed files, design tradeoffs, verification commands, preview paths, unresolved risks, and any deviations from the design.

### 3. Independent Preview Review

- Assign a third sub-agent to review the generated previews independently.
- The reviewer must compare the preview against:
  - The approved design.
  - The user's original wording and visual priorities.
  - Earlier preview versions when available.
- The reviewer must return `PASS`, `REVISE`, or `BLOCK`.
- The review must identify:
  - Silhouette and proportion issues.
  - Motion readability and leading/trailing hierarchy.
  - Brush, material, glow, transparency, and lighting issues.
  - Visible seams, discontinuities, plastic appearance, flat color, or duplicated transparent layers.
  - Specific changes required for the next iteration.

### 4. Iteration And Acceptance

- If the reviewer returns `REVISE`, route the specific findings back to the implementation agent and generate a new preview.
- Reuse the same relevant agents when possible instead of creating unnecessary replacements.
- Continue the implementation/review loop until the independent reviewer returns `PASS` and the evidence satisfies the acceptance criteria.
- The main agent must not declare completion solely because the effect builds successfully.

### 5. Final Verification And Delivery

- After visual acceptance, run the relevant source verifiers, project build, Unity or asset-bundle build, and deployment checks.
- Inspect multiple representative preview frames, including early reveal, middle reveal, full effect, and retreat or fade.
- Confirm deployed files match the newly built output.
- Commit the completed version with a focused message when the task includes committing or deployment.

## Main Agent Responsibilities

- Act as the pipeline orchestrator and maintain the intent contract between all stages.
- Use the latest suitable model for each sub-agent role, preferring the strongest current model for research, visual design, and final visual judgment.
- Keep the user informed at the start and completion of each stage, when a blocker appears, and whenever a review triggers another iteration.
- Progress updates must state what was learned, what changed, what preview is being reviewed, and what happens next.
- Do not duplicate a sub-agent's assigned implementation work unless the user explicitly asks the main agent to implement directly or no sub-agent capability is available.
- If sub-agent tools are unavailable, execute the same stages sequentially in the main thread and preserve the independent review step as a separate review pass.
