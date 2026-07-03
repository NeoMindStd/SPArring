# Neo Bots

Sparring can include in-house BWAPI bots next to imported public bots. These bots live under `src\Sparring.Bots` for source and `data\bots\<BotName>` for packaged DLL assets.

## Shared behavior

The launcher writes the selected build into `bwapi-data\AI\Sparring\Bots\<BotName>\sparring-bot.ini` and mirrors the same value to `bwapi-data\AI\sparring-bot.ini` before launching the AI runtime.

```ini
build=23_nexus
```

If the value is absent, `random`, or unknown to the bot, the bot chooses one matchup-appropriate opening from its own default pool.

The Neo bots are moving from pure BWAPI rule scripts to a hybrid architecture:
opening execution remains build-order driven, while post-opening judgment is delegated to a policy model layer. The current first policy layer is a lightweight sidecar-weight model (`neo-policy.tsv`) embedded in each packaged Neo bot folder so it can run inside the 32-bit BWAPI DLL without an external service. It scores a compact game-state vector into concrete action targets such as worker count, gas workers, Nexus count, Gateway count, unit production caps, defense probes, and attack pressure. It is not yet a trained deep RL model; the important product boundary is that new midgame decisions should be added as policy features, outputs, or model weights, not as another pile of frame-by-frame if/else rules.

Keep descriptions honest about this limitation and use Neo bots as controllable sparring partners rather than tournament-strength opponents. Current Neo bots are development-preview bots: they stay selectable for manual sparring, but are excluded from ladder and random matchmaking.

Gas worker assignment defaults to three workers on a completed refinery/extractor. The bots can temporarily reduce that target to one or two when gas is already banked and minerals are low, then refill gas later when the mineral balance recovers.

The E-grade Neo bots share the same supported map pool and build-option packaging as the F-grade bots. They are intended to feel one step harder through a more aggressive policy model, slightly higher worker caps, more production, and earlier attack transitions. They are still development-preview practice bots and remain excluded from ladder/random matchmaking.

## Policy model boundary

The Neo policy layer uses this split:

- Opening build executor: follows the selected human-style build option through early supply and tech milestones.
- Low-level executor: performs safe BWAPI commands such as training workers, starting one reserved building worker, assigning gas workers, and issuing attacks.
- Policy model: reads a compact game-state vector and outputs both intent scores and concrete action targets.

Do not solve midgame behavior by adding more one-off tactical rules to `onFrame()`. If the bot needs to expand earlier, attack later, defend with fewer workers, or choose different army production after the opening, change the policy features, output weights, or action mapping first. Low-level safety rules are still allowed for command correctness, for example preventing ten probes from being assigned to one building before the first worker starts construction.

The longer-term target is to replace the lightweight sidecar scorer with a replay-trained or TorchCraftAI/CherryPi-style model while keeping the same state/action boundary.

## NeoProtossF

`NeoProtossF` is the first built-in Protoss practice bot. It targets low-level manual sparring rather than tournament strength. It has no public ladder ELO yet and is excluded from ladder/random matchmaking until its policy judgment is more reliable.

### Opening Pool

At game start the bot chooses one opening from a conservative default pool:

- All matchups: 1012 two-gate zealot pressure, fast power dragoon.
- Versus Zerg: Forge double, gate double into Corsair/Dark Templar.

Greedier or more fragile openings such as 23 Nexus, 29 Arbiter, naked double, fast Dark Templar, and forward-gate Dark Templar remain available as explicit build choices, but they are not part of the default random pool.

The opening executor is intentionally simple and supply-threshold based. The default pylon buffer is kept slightly early to reduce hard supply blocks. After the opening phase, worker targets, gas balance, production caps, expansion, and attack pressure should be driven by `neo-policy.tsv` rather than hard-coded timing constants.

### Supported Maps

The first version is registered only on maps where the generic mineral-cluster natural expansion logic is expected to work:

- `(4)Fighting Spirit`
- `(4)Fighting Spirit 1.4`
- `(4)Polypoid 1.65`
- `(4)Polypoid 1.75`
- `(4)Python`
- `(4)Circuit Breaker`
- `(2)Match Point`

Future higher-grade bots should keep the same packaging shape and add map-specific wall/choke placement before expanding the supported map list.

### Build

Run:

```powershell
.\scripts\build-neo-bots.ps1
```

The script uses BWAPI 4.4.0 Release_Binary headers and BWAPILIB, builds Win32 DLLs with Visual Studio C++ tools, and copies the results into `data\bots\<BotName>`. If MSVC is unavailable, the script may reuse already bundled Neo DLLs only when the edited source is not newer than the packaged binaries.

## NeoTerranF

`NeoTerranF` is a built-in Terran practice bot with the same target baseline as NeoProtossF: low-level manual sparring. It has no public ladder ELO yet and is excluded from ladder/random matchmaking until its midgame judgment is more reliable.

### Opening Pool

The bot exposes explicit build choices in the launcher:

- Versus Zerg: bio academy, barracks expand.
- Versus Protoss: factory expand, two-factory pressure.
- Versus Terran: factory expand, two-factory pressure, one-factory starport.
- All matchups: barracks expand can be used as a generic macro opener.

It trains SCVs, keeps gas workers assigned, pulls only a small number of SCVs against early worker harassment, and releases those SCVs back to mining once combat units can take over. If it becomes unable to fight or produce, it sends `gg`.

### Supported Maps

NeoTerranF currently uses the same conservative map pool as NeoProtossF:

- `(4)Fighting Spirit`
- `(4)Fighting Spirit 1.4`
- `(4)Polypoid 1.65`
- `(4)Polypoid 1.75`
- `(4)Python`
- `(4)Circuit Breaker`
- `(2)Match Point`

### Build

Run:

```powershell
.\scripts\build-neo-bots.ps1
```

The script builds NeoProtossF, NeoTerranF, and NeoZergF, then copies the DLLs into `data\bots\<BotName>`.

## NeoZergF

`NeoZergF` is a built-in Zerg practice bot with the same low-level manual sparring target as the other Neo bots. It has no public ladder ELO yet and is excluded from ladder/random matchmaking until its midgame judgment is more reliable.

### Opening Pool

The bot exposes explicit build choices in the launcher:

- All / ZvZ: overpool speed, 9 pool speed.
- Versus Terran: 2 hatch muta, 530 muta, lurker contain.
- Versus Protoss: 3 hatch hydra, 5 hatch hydra, 9 pool speed.

It keeps the implementation intentionally simple: drones mine and take gas, the selected opening reserves minerals for key early buildings, a small number of drones defend early worker pressure, and the bot transitions into Zergling, Hydralisk, Mutalisk, or Lurker timing attacks.

### Supported Maps

NeoZergF currently uses the same conservative map pool as NeoProtossF and NeoTerranF:

- `(4)Fighting Spirit`
- `(4)Fighting Spirit 1.4`
- `(4)Polypoid 1.65`
- `(4)Polypoid 1.75`
- `(4)Python`
- `(4)Circuit Breaker`
- `(2)Match Point`

### Build

Run:

```powershell
.\scripts\build-neo-bots.ps1
```

The script builds all three Neo bots and copies the DLLs into `data\bots\<BotName>`.

## NeoE Bots

`NeoProtossE`, `NeoTerranE`, and `NeoZergE` are one grade above the F bots. They reuse the same conservative map pool and selectable build IDs so launcher and runtime packaging stay consistent, but their timing constants are tuned to be a little more decisive.

### NeoProtossE

- Adds 23 Nexus to the default Protoss-vs-Terran candidate pool.
- Builds a little more army and production than NeoProtossF.
- Attacks earlier on 1012, fast Dragoon, Dark Templar, Arbiter, and generic macro plans.

### NeoTerranE

- Adds Barracks/Factory production a little earlier than NeoTerranF.
- Raises Marine/Medic, Vulture/Tank, and Wraith caps slightly.
- Starts the first attack with a smaller army than the F-grade Terran bot.

### NeoZergE

- Raises Drone, Zergling, Hydralisk, Mutalisk, and follow-up Hydra caps slightly.
- Attacks earlier across Muta, Hydra, Lurker, and Ling plans.
- Keeps the same gas-worker flexibility as NeoZergF.

### Build

Run:

```powershell
.\scripts\build-neo-bots.ps1
```

The script builds all six Neo bots and copies the DLLs into `data\bots\<BotName>`.

## 2026-07-03 Serial Runtime Check

Parallel Local PC bot-vs-bot validation is not reliable on one Windows machine, so Neo runtime checks should run one match at a time.

Current packaged DLL observations on `(4)Fighting Spirit`:

- `NeoProtossF` vs `Dragon`: reached in-game with no runtime errors. At about five minutes, the bot had grown mostly through Probes/Nexus/Gateway infrastructure, but gas and Dragoon/tech transition were much too late. Dragon attacked with a stronger army while NeoProtossF had little visible defense.
- `NeoProtossE` vs `Dragon`: reached in-game with no runtime errors. At about five minutes, it had more Gateways/Zealots than F, but gas was still at zero and Dragoon/tech transition was still missing.
- `NeoTerranF` vs `Hannes Bredberg`: reached in-game with no runtime errors. At three minutes, SCV, Refinery, Barracks, gas, and production progression were visible.
- `NeoZergF` vs `Dragon`: reached in-game with no runtime errors. At three minutes, Drone, Extractor/gas, and early tech progression were visible.

The Protoss policy/source changes in this worktree are newer than the packaged DLLs. Do not treat a runtime check as validating the new Protoss policy layer until `scripts\build-neo-bots.ps1` succeeds and fresh `NeoProtossF.dll` / `NeoProtossE.dll` are copied into `data\bots`.

## Roadmap

- Replace the lightweight sidecar policy scorer with a replay-trained or open-model-backed policy.
- Extend the policy layer from NeoProtossF/E to NeoTerranF/E and NeoZergF/E.
- Add scouting-based reactions for tech switches, expansion timing, and army composition.
- Make worker-defense behavior less brittle against small harassment.
- Add more reliable GG/leave timing based on game state instead of simple production/fighting checks.
- Re-evaluate Neo bot ELO only after longer replay review across common maps and matchups.
