# Solar System Unity Web Simulator

A simple, open-source Solar System simulator built in Unity (URP), targeting WebGL.  
This project is an early prototype and is under active development.

## Status
**Prototype / in progress.**  
Features, assets, and structure may change frequently.

## Tech
- **Engine:** Unity **6000.3+**
- **Render Pipeline:** **URP**
- **Target:** WebGL (desktop browsers)

## Goals
- Real-time, lightweight Solar System visualization in the browser (Unity WebGL)
- Simple sphere-based planets with scientifically sourced textures where available
- Multiple scenes (e.g., full Solar System view, Earth–Moon close-up, etc.)
- Clean, open-source-friendly asset sourcing and attribution

## Current Features
- Sun + planets rendered as spheres with textures
- Selected natural satellites (e.g., Moon, Phobos, Deimos, Galilean moons, Titan, Enceladus)
- Starfield / Milky Way skybox (NASA SVS Deep Star Maps)
- Asset attribution in `Credits.md`

## Project Structure (high level)
- `Assets/` — Unity project assets (materials, textures, scenes, scripts)
- `Credits.md` — third-party source links and required attributions
- `LICENSE` — MIT license

## Asset Sources / Credits
Textures and skybox resources are from NASA/USGS public resources and related outreach assets.  
See **`Credits.md`** for the complete list of sources and credit lines.

Notes:
- Some planetary textures (especially gas giants and some outer bodies) may be labeled by NASA as **fictional/representative**.
- USGS datasets may request: **“Please cite authors.”** (included in `Credits.md`)

## License
This project’s code is licensed under the **MIT License**. See `LICENSE`.

Third-party assets (textures/models) are attributed in `Credits.md` and remain subject to their original source terms.

## Getting Started (Unity 6000.3+ / URP)

### Open the project
1. Install **Unity 6000.3+** (Unity Hub recommended).
2. Open this repository folder as a Unity project.
3. Allow Unity to import assets and compile scripts.

### Play in Editor
- Open the main scene (the Solar System scene) from `Assets/Scenes/` (name may vary).
- Press **Play**.

### Build WebGL
1. `File → Build Settings…`
2. Select **WebGL** → **Switch Platform**
3. `Build`

## Development Roadmap (short)
- Add time controls (pause, speed up, slow down)
- Add orbit visualization (simple circular/elliptical paths)
- Add camera focus/track on selected bodies
- Add additional close-up scenes (Earth–Moon first)
- WebGL performance pass (texture sizing, LOD, memory)

## Contributing
Issues and pull requests are welcome.
- Keep changes small and focused
- If you add/replace third-party assets, update `Credits.md` with the new source links and required credit lines

## Disclaimer
This is a visualization project and not intended for scientific analysis.
