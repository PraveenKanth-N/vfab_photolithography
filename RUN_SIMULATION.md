# AI-Driven Virtual Reality Digital Twin for Semiconductor Training
## Complete Simulation Execution & Architecture Guide

Welcome to the **Semiconductor Cleanroom Digital Twin** platform (IEEE Access Architecture).
All project files, source code, physics engines, microservices, and assets reside on the **`D:` drive** at:
`D:\vfab_photolithography`

---

## 1. Architecture Overview (10 Integrated Modules)

| Module | Component | File Path | Key Features |
|---|---|---|---|
| **Module 1** | Cleanroom Virtual Environment | `Assets/Scenes/LabScene.unity` | ISO Class 5 cleanroom, laminar airflow, tool anchors, yellow-light litho suite. |
| **Module 2** | Authoritative Wafer Digital Twin | `Assets/Scripts/DigitalTwin/Wafers/` | Multi-layer stack, state machine, 3D defect registry, deep-copy cloning. |
| **Module 3** | Continuous Physics Engine | `Assets/Scripts/DigitalTwin/Physics/PhysicsSimulationEngine.cs` | Implements Eqs. 1–7: Emslie-Bonner-Peck spin dynamics, Rayleigh diffraction, Dill ABC kinetics, Langmuir-Hinshelwood RIE, sheath voltage, anisotropy $A_{etch}$, Murphy yield. |
| **Module 4** | Consequence-Based Rule Engine | `Assets/Scripts/DigitalTwin/Physics/ConsequenceRuleEngine.cs` | Evaluates Table II operational tolerances and injects deterministic morphology flaws on boundary violations. |
| **Module 5** | Photolithography Tool Twin | `Assets/Scripts/` | VFab spin coater, hotplate soft-bake, contact aligner, chemical developer. |
| **Module 6** | Plasma Etcher (RIE) Tool Twin | `Assets/Scripts/DigitalTwin/Equipment/PlasmaEtcherController.cs` | RF bias generator (0–600 W), vacuum chamber (5–100 mTorr), MFCs ($SF_6/CF_4/O_2$), plasma glow VFX. |
| **Module 7** | Defect Metrology & Inspection | `Assets/Scripts/DigitalTwin/Wafers/WaferDefect.cs` | 3D coordinate defect locator, severity scoring, optical microscope inspection. |
| **Module 8** | Socratic AI Tutor & BKT | `Assets/Scripts/DigitalTwin/Pedagogy/` | 3-tier guided Socratic inquiry (Observation $\rightarrow$ Deduction $\rightarrow$ Remediation) with Bayesian Knowledge Tracing (Eq. 8). |
| **Module 9** | Microservices Backend Pipeline | `D:/vfab_photolithography/Backend/` | FastAPI RESTful API, SQLite/PostgreSQL persistence, 60 Hz telemetry, WebSocket stream, Swagger docs. |
| **Module 10** | Time-Travel Debugger | `Assets/Scripts/DigitalTwin/TimeTravel/` | Immutable historical snapshot scrubber, temporal state rollback, timeline diffing. |

---

## 2. Quick-Start Guide (Step-by-Step)

### Step 1: Start the Microservices Backend Server
1. Navigate to:
   ```cmd
   D:\vfab_photolithography\Backend
   ```
2. Double-click **`run_backend.bat`** (or open PowerShell in that folder and run `python -m uvicorn main:app --host 0.0.0.0 --port 8000 --reload`).
3. You will see:
   ```
   INFO:     Uvicorn running on http://0.0.0.0:8000 (Press CTRL+C to quit)
   ```
4. Open your browser to test the interactive API documentation:
   - **Swagger UI**: [http://127.0.0.1:8000/docs](http://127.0.0.1:8000/docs)
   - **Health Check**: [http://127.0.0.1:8000/health](http://127.0.0.1:8000/health)

### Step 2: Open the Project in Unity Hub
1. Open **Unity Hub**.
2. Look at the Projects list — **`vfab_photolithography`** is already registered and pinned at `D:\vfab_photolithography` using **Unity 6 (6000.5.10f1)** with the Universal Render Pipeline (URP).
3. Click on **`vfab_photolithography`** to open the project.
4. Once Unity opens:
   - In the **Project** window at the bottom, go to:
     `Assets` $\rightarrow$ `Scenes` $\rightarrow$ double-click **`LabScene.unity`**.
   - Press the **Play** button (`▶`) at the top of the Unity Editor.

---

## 3. How to Interact with the Digital Twin in Unity

### A. Automatic Backend Bridge (`TelemetryBridge`)
- When the scene starts, `TelemetryBridge` probes `http://localhost:8000/health`.
- If the backend is running, it automatically opens a student training session (`STUDENT_VR_01`), streams live equipment telemetry, and uploads wafer snapshots at each fabrication milestone.
- If the backend is offline, the simulation runs smoothly in local offline mode without blocking or crashing.

### B. Equipment Progression & Consequence Rule Violations
1. **Photoresist Spin Coating**:
   - Optimal Recipe: 3000–4000 RPM for 30s $\rightarrow$ Nominal 1.2 $\mu$m AZ 1512 film.
   - Out-of-Bounds Test: Spin at 1500 RPM $\rightarrow$ Consequence engine injects *Resist Striations & Edge Bead*.
2. **Soft-Bake Hotplate**:
   - Optimal Recipe: 95 °C for 60s $\rightarrow$ Residual solvent reduced to 5%.
   - Out-of-Bounds Test: Bake at 140 °C $\rightarrow$ Injects *Solvent Bubble Pinholes & Photoactive Compound Degradation*.
3. **Optical Mask Exposure**:
   - Optimal Recipe: 120 mJ/cm² dose, contact alignment $\rightarrow$ 0.8 $\mu$m CD pattern.
   - Out-of-Bounds Test: Under-exposure (40 mJ/cm²) $\rightarrow$ Injects *Incomplete Pattern Clearing*.
4. **Plasma Etcher (RIE)**:
   - Optimal Recipe: 300 W RF power, 20 mTorr chamber pressure, 50 sccm $SF_6$ $\rightarrow$ Highly anisotropic trench ($A_{etch} \ge 0.90$).
   - Out-of-Bounds Test: Pressure 80 mTorr $\rightarrow$ Ion scattering causes *Isotropic Lateral Undercut*.
   - Out-of-Bounds Test: RF Power 550 W $\rightarrow$ Excessive DC bias causes *Micro-Trenching & Mask Sputter*.

### C. Socratic AI Tutor & Bayesian Knowledge Tracing (M8)
- Whenever a physical defect is detected, the Socratic AI Tutor automatically triggers a 3-tier dialogue HUD:
  - **Tier 1 (Physical Observation)**: Directs student attention to the morphology of the wafer flaw.
  - **Tier 2 (Phenomenological Deduction)**: Tests understanding of the underlying physical equation (e.g. Langmuir-Hinshelwood, sheath bias voltage).
  - **Tier 3 (Recipe Remediation)**: Student selects corrective recipe parameters.
- Concept mastery probability $P(L_t)$ is recalculated using BKT (Eq. 8) and synced to the cloud database.

### D. Time-Travel Debugger (M10)
- The Time-Travel UI slider displays every captured process milestone.
- Drag the timeline slider or click **Step Backward / Forward** to scrub the physical wafer back to any previous state to trace defect origins.

---

## 4. Automated Backend Test Suite
You can verify the backend anytime by running:
```powershell
python D:\vfab_photolithography\Backend\test_backend.py
```
This runs 7 automated end-to-end integration tests validating session lifecycle, wafer serialization, telemetry ingestion, defect Murphy yield estimation, baseline recipes, and Bayesian Knowledge Tracing.
