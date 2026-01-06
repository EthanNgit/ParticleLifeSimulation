# Particle Life Simulation (Unity)

<img width="2559" height="1241" alt="image" src="https://github.com/user-attachments/assets/91e8f83c-b72b-46cc-87a6-0e8da77feb84" />

Simple, “particle life” simulation built in Unity.

## Overview

- Particles have `position`, `velocity`, and a `particleType`.
- Interaction matrix defines how each type influences others (attractive or repulsive).
- Force model:
  - Strong repulsion when particles are very close (inside `Beta`).
  - Attraction/repulsion at intermediate ranges, scaled by the interaction matrix.
  - No interaction beyond `RMax`.
- Motion integrates velocity with friction/damping and wraps at world boundaries.

## Key Components

- `SimulationManager`: Core simulation loop, rendering, and parameters.

## Parameters (Inspector)

- **`ParticleCount`**: Number of particles.
- **`ParticleTypeCount`**: Number of distinct interaction types.
- **`ForceMultiplier`**: Scales overall interaction strength.
- **`EnvSize`**: Half-extents of the world (world spans `[-EnvSize.x, +EnvSize.x]` and `[-EnvSize.y, +EnvSize.y]`).
- **`RMax`**: Max interaction distance (also used as grid cell size).
- **`Beta`**: Inner repulsion radius (fraction of `RMax` within the force function).
