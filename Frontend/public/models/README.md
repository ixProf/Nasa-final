# 3D Anatomical GLB Model Directory

This directory is the dedicated location for the 3D anatomical human model used by the interactive 3D Body Map.

---

## 1. Expected File Location and Names

Place the 3D model in this folder:
```
Frontend/public/models/human-anatomy.glb
```
*(Alternative recognized filename: `anatomy.glb`)*

The application automatically checks for either filename on startup.

---

## 2. Model Format & Technical Specs

- **Format:** Binary glTF (`.glb`)
- **Recommended File Size:** 2 MB to 10 MB (optimized for web delivery)
- **Draco Compression:** Fully supported (decoder automatically configured from Google CDN)
- **Coordinate System:** Y-up, centered at origin `(0, 0, 0)`

---

## 3. Recommended Mesh Naming Conventions

For full organ-level interactivity, the model meshes or nodes should include the following name hints (case-insensitive fuzzy matching is performed automatically in `src/data/bodyRegions.ts`):

| Anatomical Region | Recognized Mesh Names |
|---|---|
| **Brain** | `brain`, `Brain`, `Brain_mesh`, `Cerebrum`, `Head_Brain`, `cerebrum` |
| **Heart** | `heart`, `Heart`, `Heart_mesh`, `Myocardium`, `Cardio`, `CardiacMuscle` |
| **Lungs** | `lungs`, `Lungs`, `Lung_L`, `Lung_R`, `LeftLung`, `RightLung`, `Pulmonary` |
| **Eyes** | `eyes`, `Eyes`, `Eye_L`, `Eye_R`, `left_eye`, `right_eye`, `Eyeball`, `optic` |
| **GI System** | `stomach`, `Stomach`, `GI`, `pancreas`, `Pancreas`, `abdomen`, `Digestive`, `liver` |

Any meshes that do not match these regions are rendered with a neutral, semi-translucent anatomical silhouette material so the overall body remains visible.

---

## 4. Potential Open-Source Model Sources

1. **Z-Anatomy** — [https://www.z-anatomy.com](https://www.z-anatomy.com)
   - Open-source anatomical atlas (CC BY-SA 4.0).
   - Export desired layers and organs from Blender as a `.glb` with Draco compression.

2. **BodyParts3D** — [https://lifesciencedb.jp/bp3d/](https://lifesciencedb.jp/bp3d/)
   - Comprehensive anatomical database (CC BY-SA 2.1 JP).
   - Individual organs can be combined in Blender into a single `.glb`.

3. **AnatomyTool** — [https://anatomytool.org](https://anatomytool.org)
   - Open educational anatomical resources.

---

## 5. Fallback Behavior

If no `.glb` file is placed in this directory:
- The application automatically renders the high-fidelity **Biometric 3D Hologram**, allowing full interaction, region selection, and sidebar synchronization.
- The user can also switch to the **2D SVG Anatomical Map** using the toggle button in the top-right corner.
- Once a valid `.glb` is added to this directory, refreshing the page automatically loads and displays the external model.
