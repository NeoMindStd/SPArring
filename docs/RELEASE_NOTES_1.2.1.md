# StarAI Practice Client 1.2.1

Hotfix release focused on preventing the Alt+F4 exit crash path and tightening one bot-map compatibility hole found during reproduction.

## Fixes

- Player-side Alt+F4 during an active StarAI match is now intercepted only for the captured human StarCraft process.
- The intercepted shortcut is converted into the normal in-game leave flow before session finalization and AI cleanup.
- This prevents StarCraft from taking the raw Alt+F4 path that could leave Windows application error dialogs behind.
- `RedRum` + `(4)Jade` is now blocked as a known-bad runtime pair after local AI crash evidence.
- `Stone` is excluded from the compatible bot pool until runtime safety is proven, after repeated local crashes on Fighting Spirit, Jade, and Benzene.
- `CUBOT` is blocked on Fighting Spirit variants after release-candidate UI verification reproduced a local AI crash.
- `Yuanheng Zhu` is blocked on Andromeda after release-candidate UI verification reproduced a local `Juno.dll` crash.
- Random/sparring candidate filtering now removes bots that have no currently compatible maps, and launch resolution rechecks explicit bot-map compatibility.

## Verification

- `dotnet test .\StarAI.PracticeClient.sln -v:minimal`
- `.\scripts\smoke.ps1`
- `.\scripts\audit-compatibility.ps1`
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Terran -EnemyRace Terran -MapName '(2)Benzene' -BotName 'Stone'`
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Ladder -PlayerRace Terran -EnemyRace Zerg -MapName '(4)Fighting Spirit' -BotName 'CUBOT'`
- `.\scripts\smoke-app-start.ps1 -DryRun -Mode Sparring -PlayerRace Terran -EnemyRace Protoss -MapName '(4)Andromeda' -BotName 'Yuanheng Zhu'`
- `.\scripts\smoke-app-start.ps1 -Mode Ladder -PlayerRace Protoss -EnemyRace Terran -MapName '(4)Fighting Spirit 1.4 [Remastered Ladder]' -BotName 'Dragon'`
- Alt+F4 interception is covered by `GlobalInputActionHookTests`; foreground Alt+F4 UI automation was stopped after safety feedback.
- `.\scripts\build-release.ps1`

## Install

1. Download `StarAI-PracticeClient-1.2.1-win-x64.zip`.
2. Extract it.
3. Run `install.cmd`.
4. Launch `C:\starai\Start-StarAI-PracticeClient.cmd`.
