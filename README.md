# Skill Tree Pages for Quest

The Quest version of caleb's Skill Tree Pages for A Township Tale. On a server that runs Skill Tree Pages, a Quest player gets the higher skill limit, the paged orb holders and the stone page panel on the shrines, side by side with PC players.

Ported with caleb's permission. The original mod, its panel model and its design are his: https://github.com/calebv2/Skill-Tree-Pages

## What you need

- A Quest with LemonLoader (the Township Tale Quest kit).
- A server that runs Skill Tree Pages 0.3.1. Nothing changes on the server: it keeps caleb's `RevampedSkills.dll` and its `RevampedSkills.json`.
- PC players on the same server keep caleb's `RevampedSkills.dll`, as before.

## Install

Close the game. Put `SkillTreePagesQuest.dll` in the headset's Mods folder:

```
/sdcard/Android/data/com.Alta.ATownshipTale/files/MelonLoader/com.Alta.ATownshipTale/Mods/
```

Start the game and join a server that runs Skill Tree Pages. On any other server the mod does nothing and the shrines work as normal.

## Use

Open a shrine as usual. When the server allows more than eight learned skills, touch the arrows on the stone panel to change page. Move your hand away before pressing again. Pages don't change while an orb is held. Skills on other pages stay active.

## Differences from the PC version

- The mod turns on when it sees the server's page data at the first shrine you open, and stays off on servers without Skill Tree Pages. The PC mod changes every shrine as soon as the game starts, so it needs the server to run it too.
- A shrine shows its normal eight holders until you open it once.
- The panel text uses the game's own font.
- Holders on other pages are hidden and out of reach, as on PC.

## Building

`build.ps1 -SkillTreePages <folder>` takes a clone of caleb's repo, bakes the panel model from its asset bundle (`tools/bake.py`, needs `pip install UnityPy`), then builds the Quest DLL against the LemonLoader proxies of the game. Paths at the top of the script.
