# Photolithography Cleanroom (Virtual FAB)

An interactive, physics-driven photolithography cleanroom simulation built in Unity using the Universal Render Pipeline (URP). The project combines realistic cleanroom equipment workflows with real-time neural network inference (Unity Inference Engine / ONNX) to model semiconductor fabrication processes such as spin coating film thickness, optical exposure, and chemical development.

<img src="Screenshots/Picture1.jpg" alt="Cleanroom Laboratory Overview" width="550"/>

## Overview

The photolithography section of VFAB aims to simulate the end-to-end positive photoresist photolithography sequence performed in a cleanroom environment. Users interact with equipment stations, substrates, chemical dispensers, and automated metrology tools while receiving real-time physical and visual feedback.

## Core Modules & Process Flow

### 1. Substrate Cleaning & Dehydration

- **SC-1 Wet Bench**: Immersion cleaning sequence to strip organic residues and particulates from silicon substrates.
- **Dehydration Oven**: High-temperature thermal bake to remove surface moisture and promote photoresist adhesion.

<img src="Screenshots/Picture2.jpg" alt="Substrate Preparation Station" width="550"/>

### 2. Photoresist Spin Coating (AI-Driven Film Thickness Modeling)

- **Spin Coater**: Dynamic spin speed and dispense volume control.
- **Neural Network Integration**: Embedded ONNX regression models (`Unity.InferenceEngine`) predict radial photoresist thickness distribution across the wafer surface based on spin speed, acceleration, and viscosity.
- **Optical Interferometry Visualizer**: Maps predicted film thickness to real-time thin-film color gradients across the substrate mesh.

<img src="Screenshots/Picture3.jpg" alt="Spin Coater & Film Visualizer" width="550"/>

### 3. Soft Bake & Hotplate Processing

- **Precision Hotplate**: Thermal baking stage to drive off solvent content and stabilize the resist film before exposure.
- **Interactive Controls**: Temperature setpoints, bake duration tracking, and process interlocks.

<img src="Screenshots/Picture4.png" alt="Output of Hotplate after Soft baking and Post exposure bake" width="550"/>

### 4. Mask Alignment & Optical Exposure

- **Contact Mask Aligner**: Positioning of photomasks relative to the coated substrate.
- **UV Exposure Simulation**: Pattern transfer simulation computing exposure dosage and latent image formation across the resist layer.

<img src="Screenshots/Picture5.png" alt="Mask Aligner / Exposure Station" width="550"/>
<img src="Screenshots/Picture6.png" alt="Image intensity output" width="550"/>

### 5. Chemical Development & Metrology

- **Developer Immersion**: Selective dissolution of exposed positive photoresist regions.
- **Metrology & Inspection**: Optical microscope and line/edge inspection interfaces measuring critical dimensions and profile fidelity.

<img src="Screenshots/Picture7.png" alt="Development & Metrology Inspection" width="550"/>

## Technology Stack & Architecture

- **Engine**: Unity 6 / Unity 2022.3 LTS
- **Target Platform**: WebGL & Standalone Desktop
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Machine Learning Runtime**: Unity Inference Engine (`com.unity.ai.inference` / Unity Sentis) executing local `.onnx` models
- **UI Framework**: Unity UI Toolkit & TextMeshPro

## Getting Started

### Prerequisites & Editor Disclaimer

- Unity Editor 6000.0+ (or Unity 2022.3 LTS with URP support)
- Git Large File Storage (Git LFS) enabled if tracking binary models or assets

> **Disclaimer**: To modify scenes, edit assets, or compile scripts cleanly in the Unity Editor, ensure the following required packages are installed via the Unity Package Manager (`Packages/manifest.json`):
>
> - **glTFast**: `com.unity.cloud.gltfast`
> - **ProBuilder**: `com.unity.probuilder`
> - **Sentis / Inference Engine**: `com.unity.ai.inference` (or `com.unity.sentis`)
> - **Universal Render Pipeline**: `com.unity.render-pipelines.universal`

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repo/VFAB.git
   ```
2. Open the project folder (`VFAB`) in Unity Hub.
3. Verify that all required Package Manager dependencies listed above are resolved.
4. Open the primary laboratory scene located in `Assets/Scenes/LabScene.unity`.

## Project Structure

```
VFAB/
├── Assets/
│   ├── Models/           # Equipment 3D meshes, cleanroom environment, wafers, masks
│   ├── Scripts/          # Machine controllers, ML inference bridges, UI interaction
│   ├── Settings/         # URP pipeline assets and project render settings
│   └── UI Toolkit/       # Panels, stylesheets, and dashboard assets
├── Packages/             # Package dependencies and manifest definitions
└── ProjectSettings/      # Unity project configuration
```

## Live Demo & Deployment

Check the **Releases** section of this repository for playable builds and links to the official **itch.io** WebGL deployment.
