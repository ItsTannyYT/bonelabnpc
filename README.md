# BONELAB NPC Gun Response Mod

This repository contains a BONELAB MelonLoader mod that allows NPCs to grab any nearby gun (including modded guns) and shoot back when the player points a gun at them.

## Features
- Detects when the player is aiming a gun at an NPC.
- Finds the closest firearm (including modded guns via reflection) within a configurable range.
- Commands the NPC to grab and shoot at the player until the threat stops.
- Adds a lightweight AI state machine (Idle, Alerted, Combat, Searching, Fleeing, Reloading, Downed, Dead).
- Implements health, body-part damage multipliers, bleeding, stagger, limb disable, and ragdoll on death.
- NPCs surrender when aimed at and will drop their weapon after their 3 magazines run dry.

## Build
1. Install MelonLoader for BONELAB.
2. Create a C# class library that references BONELAB, BoneLib, and Unity assemblies.
3. Build the project and drop the resulting DLL into `BONELAB/Mods`.
4. Remove the `BONELAB_STUBS` constant from `BonelabNpcMod.csproj` when building against the real game assemblies.

## Usage (In-Game)
1. Copy the built DLL into `BONELAB/Mods`.
2. (Optional) Add your NPC asset bundle at `BONELAB/Mods/BonelabNpcMod/custom-npc.bundle`.
3. Launch BONELAB with MelonLoader enabled.
4. Spawn any NPC (or rely on the optional custom prefab). NPCs will react when you aim a gun at them.

## Configuration
Most settings can be adjusted in `NpcGunResponder.cs`:
- `ThreatRange`
- `GunSearchRange`
- `ThreatCheckInterval`
- `ShotsPerBurst`
Combat defaults live in `NpcCombatant.cs`:
- Health, damage multipliers, bleeding/stagger timers
- Magazine size and reload timing

## Custom NPC Asset (Optional)
If you have an FBX + textures for your NPC, import them into a Unity project and build an asset bundle named `custom-npc.bundle`. Place the bundle at:
`BONELAB/Mods/BonelabNpcMod/custom-npc.bundle`.

The mod will look for a prefab named `CustomNpc` inside the bundle and spawn it in front of the player on load.

## Notes
This first iteration is focused on a working baseline. Additional behaviors can be layered on later.
