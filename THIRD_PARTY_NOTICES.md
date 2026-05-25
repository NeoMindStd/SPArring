# Third-party notices

AIStarClient is only the launcher/client code in this repository. It does not
redistribute StarCraft, Brood War, ChaosLauncher, BWAPI runtime binaries,
SCHNAIL, BWAPI Revamped, or third-party bot binaries.

## Runtime components the user may install separately

- StarCraft: Brood War 1.16.1: proprietary Blizzard Entertainment software.
  This repository must not contain or redistribute StarCraft executables, MPQ
  files, patches, CD keys, maps from paid products, or other Blizzard-owned
  assets.
- BWAPI: https://github.com/bwapi/bwapi. GitHub identifies the BWAPI repository
  license as LGPL-3.0 plus an additional unknown license file. AIStarClient only
  edits `bwapi.ini` in a user-selected local installation.
- BWAPI Revamped: https://github.com/captain-majid/BWAPI-Revamped. GitHub did
  not report a detectable license at the time this notice was written, so this
  repository does not redistribute BWAPI Revamped packages.
- ChaosLauncher: license not verified here. Do not bundle it unless its license
  and redistribution terms are confirmed.
- SCHNAIL Client: license not verified here. AIStarClient may copy hotkey data
  from a user-local installation, but this repository must not redistribute
  SCHNAIL binaries or assets.

## Release rule

Release artifacts produced from this repository should contain only AIStarClient
application files and documentation. The smoke script checks publish output for
common forbidden game/launcher files before packaging.
