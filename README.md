# 🎮 Unity C# Reference (Expanded & Improved)

![Unity Version](https://img.shields.io/badge/Unity-2022.2%2B-black?style=for-the-badge&logo=unity)
![License](https://img.shields.io/badge/License-Reference%20Only-red?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Improved-success?style=for-the-badge)

> **A "Quality of Life" fork of the official Unity C# Reference Source.**
> This repository transforms the raw reference code into modular, improved packages ready for local development and study.

---

## ✨ Key Improvements

We have taken the raw source code and applied modern structure and API enhancements.

### 📦 Modular Design
| Module | Status | Description |
| :--- | :--- | :--- |
| **Grid** | 🟢 **Ready** | Fully standardized with `package.json` and `.asmdef`. Ready to drop into `Packages/`. |
| **UI** | 🟡 *Planned* | Future candidate for package standardization. |
| **Physics** | 🟡 *Planned* | Future candidate for package standardization. |

### 🚀 API Enhancements
We don't just organize the code; we improve it.
*   **Grid Module**: Added `GetCellCenterWorld(Vector3 position)` overload. No more manual casting to `Vector3Int` for simple world-space checks!

---

## 🛠️ Getting Started

### Using the Improved Modules
You can use these modules directly in your Unity project as **Local Packages**.

1.  **Clone** this repository.
2.  Open your Unity Project's `Packages/manifest.json`.
3.  Add the local path to the module you want:
    ```json
    {
      "dependencies": {
        "com.unity.modules.grid": "file:../../UnityCsReference/Modules/Grid",
        ...
      }
    }
    ```
4.  **Done!** The module is now compiled as part of your project, and you can edit it freely.

---

## 📂 Project Structure

```mermaid
graph TD
    Root[📂 Repository Root] --> Modules[📂 Modules]
    Modules --> Grid[📦 Grid (Improved)]
    Grid --> Manifest[📄 package.json]
    Grid --> Asmdef[⚙️ Unity.Modules.Grid.asmdef]
    Grid --> Scripts[📂 Managed Scripts]
    Root --> External[📂 External Libs]
    Root --> Projects[📂 VS Projects]
```

*   **Modules/**: Contains the core Unity subsystems.
*   **External/**: Third-party dependencies (Mono.Cecil, NRefactory, etc.).
*   **Projects/**: Visual Studio solutions for browsing the entire codebase.

---

## ⚠️ License & Disclaimer

**Official Unity License Applies:**
> *The C# part of the Unity engine and editor source code. May be used for reference purposes only.*

See the [Official Terms of Use](https://unity3d.com/legal/licenses/Unity_Reference_Only_License) for details.

**Note for this Fork:**
This fork is for **educational and experimental purposes**. While we have structured modules as packages, you cannot republish them commercially or violate the original reference license. Use these improvements to debug, learn, or patch your own local builds.

---
*Maintained with ❤️ by the Open Source Community*
