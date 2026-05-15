# Last Light Unity Port

This repository is the Unity remake of the original `Glow-Jump` web/Capacitor game.

Scope:
- exact gameplay remake
- reuse the same game feel, audio, and flow where practical
- remove company logo usage from the Unity version
- keep the project organized for long-term iteration

Planned port milestones:
1. Project structure and source control setup
2. Shared runtime systems: app state, save data, audio, config
3. Intro, menu, settings, and game over flow
4. Horizontal mode gameplay
5. Vertical mode gameplay
6. Final polish and parity fixes

Top-level asset organization:
- `Assets/Art`
- `Assets/Audio`
- `Assets/Prefabs`
- `Assets/Scenes`
- `Assets/Scripts/Core`
- `Assets/Scripts/Data`
- `Assets/Scripts/Gameplay`
- `Assets/Scripts/UI`
- `Assets/Scripts/Editor`

Current progress:
- Unity project repo is initialized and connected to GitHub
- asset folders are organized
- core runtime scripts are added
- save/settings models are added
- audio manager is added
- horizontal and vertical gameplay config parity is added

Current blocker:
- the Unity editor is still open on this project, so scene generation and compile validation are blocked until it is closed
