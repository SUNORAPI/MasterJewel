# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project

MasterJewel is a Unity 6 (`6000.4.10f1`, URP) local-multiplayer game: up to 8 players in two teams move on a 16×16 grid and collect crystals. Early prototype — most `MonoBehaviour`s are stubs; comments are in Japanese. There is no CLI build/test workflow; develop in the Unity Editor.

## Non-obvious notes

- **Input actions are an SFC-style gamepad map**: the `Player` action map in `Assets/InputSystem_Actions.inputactions` defines `Dpad`, `ButtonA`/`B`/`X`/`Y`, `ButtonL`/`R`, `Start`, `Select` — gamepad-only bindings (SFC layout: A=East, B=South, X=North, Y=West). `ControllerInput.cs` looks these up by string via `PlayerInput.actions["..."]`, so the assigned actions asset must keep these names or the lookups throw. No keyboard bindings, so the editor needs a connected gamepad to drive a player.
- **Player ⇄ status ⇄ grid wiring**: a player GameObject self-registers into `GridSys` via a `PlayerRegistrar` component (serialized `playerId` = index into `PlayerStatusManager.playerStatuses`). Both managers are scene singletons (`Instance`, set in `Awake`); `PlayerStatusManager` builds statuses in `Awake` so registrars can read them in `Start`. `GridSys.Update` writes each player's grid cell back into its `PlayerStatus.positionX/Y`.
- **Team-split convention**: players are split evenly — first half team 0, second half team 1 — and the count must be even. Keep this consistent across `PlayerStatusManager` and grid/player code.
