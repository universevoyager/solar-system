# Credits

This repository includes publicly available textures/models and scientific reference data from NASA/JPL/USGS and related sources.

Where publishers request attribution (e.g., “Please cite authors”), that request is preserved below.

---

## Scientific Data Sources (SolarSystemData_J2000_Keplerian_all_moons.json)

Sources (JPL/NASA SSD):
- Planet Keplerian elements (J2000): https://ssd.jpl.nasa.gov/planets/approx_pos.html
- Planet sidereal orbital periods (years): https://ssd.jpl.nasa.gov/planets/phys_par.html
- Moon/satellite mean elements at epoch 2000-01-01.5 TDB: https://ssd.jpl.nasa.gov/sats/elem/
- AU + Julian year definitions used for unit conversion: https://ssd.jpl.nasa.gov/astro_par.html

> Note on accuracy: This project uses simplified orbit models for visualization. It is not intended for ephemeris-grade computation. For accurate ephemerides, use NASA/JPL Horizons.

---

## AI Assistance / Transparency

Parts of the code structure and initial JSON schema/formatting were developed with assistance from **OpenAI ChatGPT (GPT-5.2 Thinking)**.  
All outputs were reviewed/modified by the project maintainers before committing.

---

## Third-Party Visual Assets (Textures / Models)

This repository includes publicly available textures/models from NASA and USGS sources.
Each entry below lists the original source URL(s) and the credit line (when provided by the publisher).
If you replace any asset, update this file accordingly.

## Sun
- Asset (repo): `Sun.jpg` (EUV 304Å Carrington map)
- Download URL (used in project):
  https://svs.gsfc.nasa.gov/vis/a030000/a030300/a030362/euvi_aia304_2012_carrington_print.jpg
- Source page (metadata / credits):
  https://svs.gsfc.nasa.gov/30362/
- Credit (as listed by publisher): NASA JPL

## Mercury
- Asset (repo): `Mercury.jpg`
- Download URL (used in project):
  https://astrogeology.usgs.gov/ckan/dataset/279e5d50-ff2f-4250-bde3-bb510096079e/resource/2b5865c2-bd0d-4962-bdb0-c12f0502def1/download/mercury_messenger_mosaic_global_1024.jpg
- Catalog record (metadata / provenance):
  https://astrogeology.usgs.gov/search/map/mercury_messenger_mdis_global_mosaic_250m
- Publisher: USGS Astrogeology Science Center
- Primary authors (as listed by publisher): MESSENGER Team
- Access constraints (as listed by publisher): Public domain
- Use constraints (as listed by publisher): Please cite authors

## Venus
- Asset (repo): `Venus.jpg` (NASA 3D Resources texture)
- Source page (includes downloads):
  https://science.nasa.gov/3d-resources/venus/
- Notes (from source): Stitched from Magellan RADAR imagery, gaps filled with global texture, from the database of JPL/Caltech generated planetary maps.
- Credit: Not listed on the source page

## Earth
- Asset (repo): `Earth.jpg`
- Download URL (used in project):
  https://assets.science.nasa.gov/content/dam/science/cds/3d/resources/image/earth-(a)/Earth%20(A).jpg
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/earth-a/
- Credit (as listed by publisher): USGS & NASA/Jet Propulsion Laboratory

## Moon
- Asset (repo): `Moon.jpg`
- Download URL (used in project):
  https://astrogeology.usgs.gov/ckan/dataset/db948a2d-4d6a-4775-a0d3-12613d36f9e7/resource/d24d5ef3-abc5-42ee-ac7c-4c3261106327/download/moon_lro_lroc-wac_mosaic_global_1024.jpg
- Catalog record (metadata / provenance):
  https://astrogeology.usgs.gov/search/map/moon_lro_lroc_wac_global_morphology_mosaic_100m
- Publisher: USGS Astrogeology Science Center
- Primary authors (as listed by publisher): LROC Team
- Access constraints (as listed by publisher): Public domain
- Use constraints (as listed by publisher): Please cite authors

## Mars
- Asset (repo): `Mars.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/mars/
- Credit (as listed by publisher): NASA/Jet Propulsion Laboratory & Caltech

## Phobos
- Asset (repo): `Phobos.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/mars-phobos/
- Credit (as listed by publisher): NASA/JPL/Solar System Simulator

## Deimos
- Asset (repo): `Deimos.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/mars-deimos/
- Credit (as listed by publisher): NASA/JPL/Solar System Simulator

## Jupiter
- Asset (repo): `Jupiter.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/jupiter/
- Credit (as listed by publisher): JPL & Caltech

### Io
- Asset (repo): `Io.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/jupiter-io-a/
- Credit (as listed by publisher): USGS, JPL, & Caltech

### Europa
- Asset (repo): `Europa.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/jupiter-europa/
- Credit (as listed by publisher): USGS, JPL, & Caltech

### Ganymede
- Asset (repo): `Ganymede.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/jupiter-ganymede/
- Credit (as listed by publisher): USGS, JPL, & Caltech

### Callisto
- Asset (repo): `Callisto.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/jupiter-callisto/
- Credit (as listed by publisher): USGS, JPL, & Caltech

## Saturn
- Asset (repo): `Saturn.jpg` (NASA 3D Resources texture)
- Source page (metadata / notes):
  https://science.nasa.gov/3d-resources/saturn/
- Notes (from source): Fictional, from the database of JPL/Caltech generated planetary maps.
- Credit: Not listed on the source page

### Saturn Rings
- Asset (repo): Saturn rings texture used by `SaturnRingsProceduralMesh`
- Source image: "Expanse of Ice" (PIA08389), Cassini Imaging Science Subsystem (ISS) mosaic of Saturn's rings (released Oct 15, 2007).
- Credit: NASA/JPL/Space Science Institute.
- Notes (optional): Modified for use as a ring texture (cropped/unwrap/levels).
- Source page:
  https://pds-rings.seti.org/press_releases/pages/PIA08xxx/PIA08389.html
- Photojournal page:
  https://photojournal.jpl.nasa.gov/catalog/PIA08389

### Titan
- Asset (repo): `Titan.jpg` (NASA 3D Resources texture)
- Source page (metadata / notes):
  https://science.nasa.gov/3d-resources/saturn-titan/
- Notes (from source): Fictional, Titan concept with color from Voyager images, from the database of JPL/Caltech generated planetary maps.
- Credit: Not listed on the source page

### Enceladus
- Asset (repo): `Enceladus.jpg` (NASA 3D Resources texture)
- Source page (metadata / notes):
  https://science.nasa.gov/3d-resources/saturn-enceladus/
- Notes (from source): Mosaic from the Voyager imagery, from the database of JPL/Caltech generated planetary maps.
- Credit: Not listed on the source page

## Uranus
- Asset (repo): 3D model files (`.gltf`, `.usdz`) from NASA Science
- Source page (metadata / credits):
  https://science.nasa.gov/resource/uranus-3d-model/
- Credit (as listed by publisher): NASA Visualization Technology Applications and Development (VTAD)

## Neptune
- Asset (repo): `Neptune.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/neptune/
- Notes (from source): Fictional, texture created by Don Davis with cloud features, from the database of JPL/Caltech generated planetary maps.
- Credit (as listed by publisher): Don Davis & JPL/Caltech

## Pluto
- Asset (repo): `Pluto.jpg` (NASA 3D Resources texture)
- Source page (metadata / credits):
  https://science.nasa.gov/3d-resources/pluto/
- Notes (from source): Fictional, texture map stitched together by David Seal from a painting of Pluto by Pat Rawlings, from the database of JPL/Caltech generated planetary maps.
- Source (as listed by publisher): Pat Rawlings & JPL/Caltech
- Credit (as listed by publisher): David Seal

## Skybox / Starfield (Milky Way + stars)
- Asset (repo): `nasa_starmap_milkyway_galaxy_2020_8k.jpg` - Deep Star Maps 2020 (equirectangular star maps designed for spherical mapping)
- Source page (downloads + metadata):
  https://svs.gsfc.nasa.gov/4851/
- Credit (as listed by publisher): Visualizations by Ernie Wright (NASA Scientific Visualization Studio)
- Notes: Choose one of the downloadable equirectangular maps on the page (e.g., the “star map” and/or the “Milky Way background” layers) and commit the exact file(s) you use into the repo.

---

## Third-Party Fonts

### Orbitron
- Name: Orbitron
- Author: Matt McInerney
- Copyright: Copyright (c) 2009, Matt McInerney
- License: SIL Open Font License, Version 1.1 (OFL-1.1)
- Reserved Font Name: Orbitron
- Source: Google Fonts — Orbitron
  https://fonts.google.com/specimen/Orbitron
- License file: `Assets/TextMesh Pro/Fonts/Orbitron/OFL.txt`
- Note: The repository is MIT-licensed, but Orbitron font files are **not** covered by MIT, they remain licensed under OFL-1.1.

### Oxanium
- Name: Oxanium
- Author: The Oxanium Project Authors
- Copyright: Copyright 2019 The Oxanium Project Authors (https://github.com/sevmeyer/oxanium)
- License: SIL Open Font License, Version 1.1 (OFL-1.1)
- Source: Google Fonts — Oxanium
  https://fonts.google.com/specimen/Oxanium
- License file: `Assets/TextMesh Pro/Fonts/Oxanium/OFL.txt`
- Note: The repository is MIT-licensed, but Oxanium font files are **not** covered by MIT, they remain licensed under OFL-1.1.
