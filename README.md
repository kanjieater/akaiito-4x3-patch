# AKAIITO HD 4:3 patch

Unofficial user patch for the supported release identified below.

## Supported version

```text
v98_r95895-JP-CN-EN
```

The patcher validates the expected AKAIITO HD Remaster executable and Unity asset patterns before changing files. It refuses to patch when the expected input does not match.

## What is included

```text
resources/patch_clean_copy.py       reproducible scene and startup patcher
resources/patch_clean_dll.py        managed-DLL startup patch helper
resources/AKAIITO4x3Launcher.exe    standalone Windows launcher
resources/AKAIITO4x3Launcher.cs     portable launcher source
```

No game files, extracted assets, screenshots, or original game binaries are included in this repository.

## Usage

You must provide your own legally obtained clean AKAIITO HD Remaster copy. From a development environment with the patcher's Python dependencies installed:

```powershell
python resources\patch_clean_copy.py C:\path\to\clean\AkaiIto
```

The patcher creates per-file backups and writes:

```text
AKAIITO-4x3-patch-manifest.json
```

### Launcher

Copy `resources\AKAIITO4x3Launcher.exe` beside `AKAIITO_HD_REMASTER.exe`. The launcher determines its own directory at runtime, so there is no machine-specific hardcoded path and it does not depend on the caller's working directory.

Pass one width; the launcher derives the matching 4:3 height:

```powershell
AKAIITO4x3Launcher.exe 1440 windowed
```

or:

```powershell
AKAIITO4x3Launcher.exe 1920 fullscreen
```

The launcher is a standalone Windows executable and does not contain or distribute the game. It resolves the game executable relative to its own location, so no machine-specific path is embedded.

The supported development dependency versions used during investigation were:

```text
Unity target: 2020.3.43f1
Python: 3.11
UnityPy
dnfile
dncil
```

## Rollback

Each changed game file receives a `.bak-4x3-clean-*` backup. The manifest records original and patched SHA-256 hashes and the exact backup paths. To roll back, restore the relevant backup files beside the game data.

## Distribution policy

This is an unofficial fan patch and is not affiliated with or endorsed by SUCCESS Corp. It does not grant any rights to AKAIITO HD Remaster or its assets.

This repository distributes only patch/launcher tooling. Users must provide their own legally obtained game copy. Do not redistribute the game, original archives, extracted assets, or patched game files. Users are responsible for complying with the game's license and applicable law.
