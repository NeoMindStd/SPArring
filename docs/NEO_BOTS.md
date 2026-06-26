# Neo Bots

Sparring can include in-house BWAPI bots next to imported public bots. These bots live under `src\Sparring.Bots` for source and `data\bots\<BotName>` for packaged DLL assets.

## Shared behavior

The launcher writes the selected build into `bwapi-data\AI\Sparring\Bots\<BotName>\sparring-bot.ini` and mirrors the same value to `bwapi-data\AI\sparring-bot.ini` before launching the AI runtime.

```ini
build=23_nexus
```

If the value is absent, `random`, or unknown to the bot, the bot chooses one matchup-appropriate opening from its own pool.

These bots are BWAPI rule-based practice bots, not trained ML models. Keep descriptions honest about that limitation and use them as controllable sparring partners rather than tournament-strength opponents. Current Neo bots are development-preview bots: they stay selectable for manual sparring, but are excluded from ladder and random matchmaking.

## NeoProtossF

`NeoProtossF` is the first built-in Protoss practice bot. It targets low-level manual sparring rather than tournament strength. It has no public ladder ELO yet and is excluded from ladder/random matchmaking until its midgame judgment is more reliable.

### Opening Pool

At game start the bot chooses one opening from the matchup-appropriate pool:

- All matchups: 1012 two-gate zealot pressure, fast power dragoon.
- Versus Terran: 23 Nexus, 29 Arbiter, naked double, fast Dark Templar, forward-gate Dark Templar.
- Versus Zerg: Forge double, gate double into Corsair/Dark Templar.
- Versus Protoss or unknown race: naked double, fast Dark Templar, forward-gate Dark Templar, plus the all-matchup openings.

The opening rules are intentionally simple and supply-threshold based. If visible early pressure reaches the main or natural, emergency defense takes priority: probes are pulled, Zealots are trained, and Cannons are added when Forge tech exists.

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

The script uses BWAPI 4.4.0 Release_Binary headers and BWAPILIB, builds a Win32 DLL with Visual Studio C++ tools, and copies the result into `data\bots\NeoProtossF\NeoProtossF.dll`.

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

## Roadmap

- Improve midgame transition rules after the scripted opening finishes.
- Add scouting-based reactions for tech switches, expansion timing, and army composition.
- Make worker-defense behavior less brittle against small harassment.
- Add more reliable GG/leave timing based on game state instead of simple production/fighting checks.
- Re-evaluate Neo bot ELO only after longer replay review across common maps and matchups.
