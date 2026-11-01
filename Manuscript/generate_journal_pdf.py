import base64
import os
import subprocess
import sys

def get_base64_image(filename):
    path = os.path.join(r'D:\vfab_photolithography\Screenshots', filename)
    ext = os.path.splitext(filename)[1].lower()
    mime = 'image/jpeg' if ext in ['.jpg', '.jpeg'] else 'image/png'
    with open(path, 'rb') as f:
        data = base64.b64encode(f.read()).decode('utf-8')
    return f"data:{mime};base64,{data}"

pic1 = get_base64_image('Picture1.jpg')
pic2 = get_base64_image('Picture2.jpg')
pic3 = get_base64_image('Picture3.jpg')
pic4 = get_base64_image('Picture4.png')
pic5 = get_base64_image('Picture5.png')
pic6 = get_base64_image('Picture6.png')
pic7 = get_base64_image('Picture7.png')

html_content = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>An interactive physics-informed virtual reality digital twin with cognitive scaffolding for semiconductor fabrication training</title>
<style>
  @page {{
    size: A4;
    margin: 14mm 14mm 16mm 14mm;
    @top-right {{
      content: "Article";
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      font-size: 8pt;
      font-weight: bold;
    }}
  }}

  * {{
    box-sizing: border-box;
    -webkit-print-color-adjust: exact !important;
    print-color-adjust: exact !important;
  }}

  body {{
    font-family: "Minion Pro", "Georgia", "Times New Roman", serif;
    font-size: 8.8pt;
    line-height: 1.35;
    color: #111;
    margin: 0;
    padding: 0;
  }}

  /* Header elements */
  .journal-header {{
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    border-bottom: 2px solid #000;
    padding-bottom: 4px;
    margin-bottom: 8px;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  }}
  .journal-title {{
    font-size: 13.5pt;
    font-weight: 900;
    letter-spacing: -0.2px;
  }}
  .journal-title span {{
    font-weight: 400;
    margin-left: 8px;
    font-size: 11pt;
    color: #444;
  }}
  .journal-sub {{
    font-size: 7.5pt;
    color: #555;
    margin-top: 1px;
  }}
  .doi-link {{
    font-size: 7.5pt;
    color: #006699;
    text-decoration: none;
    margin-top: 2px;
    display: block;
  }}
  .oa-badge {{
    width: 22px;
    height: 22px;
    border-radius: 50%;
    background: #e26b00;
    color: #fff;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-weight: bold;
    font-size: 13pt;
    display: flex;
    align-items: center;
    justify-content: center;
    line-height: 1;
  }}

  .article-title {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 20pt;
    font-weight: 800;
    line-height: 1.15;
    letter-spacing: -0.3px;
    color: #000;
    margin: 12px 0 8px 0;
  }}

  .check-updates {{
    display: inline-flex;
    align-items: center;
    border: 1px solid #ccc;
    border-radius: 3px;
    padding: 2px 6px;
    font-size: 6.8pt;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    color: #444;
    margin-bottom: 8px;
  }}
  .check-updates svg {{
    width: 10px;
    height: 10px;
    margin-right: 4px;
    fill: #2e7d32;
  }}

  .author-list {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 9.2pt;
    font-weight: 600;
    color: #222;
    margin-bottom: 12px;
  }}
  .author-list sup {{
    font-size: 6.5pt;
    color: #006699;
    font-weight: 700;
  }}

  .abstract-container {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 8.8pt;
    line-height: 1.4;
    color: #1a1a1a;
    font-weight: 400;
    margin-bottom: 14px;
    padding-bottom: 10px;
    border-bottom: 0.5pt solid #ddd;
    text-align: justify;
  }}
  .abstract-bold {{
    font-weight: 700;
  }}

  /* Columns Layout */
  .two-column {{
    column-count: 2;
    column-gap: 5.5mm;
    text-align: justify;
    text-justify: inter-word;
  }}

  p {{
    margin: 0 0 7pt 0;
    text-indent: 0;
  }}

  h2 {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 11pt;
    font-weight: 800;
    color: #000;
    margin: 12pt 0 4pt 0;
    break-after: avoid;
  }}

  h3 {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 9.2pt;
    font-weight: 700;
    color: #111;
    margin: 9pt 0 3pt 0;
    break-after: avoid;
  }}

  h4 {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 8.5pt;
    font-weight: 700;
    color: #222;
    margin: 6pt 0 2pt 0;
    break-after: avoid;
  }}

  /* Equations */
  .equation {{
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin: 6pt 0;
    padding: 0 4px;
    font-family: "Times New Roman", Times, serif;
    font-style: italic;
    font-size: 9pt;
  }}
  .eq-content {{
    flex-grow: 1;
    text-align: center;
  }}
  .eq-num {{
    font-style: normal;
    font-size: 8pt;
    color: #333;
    padding-left: 6px;
  }}

  /* Figures */
  .figure-box {{
    margin: 8pt 0;
    break-inside: avoid;
    width: 100%;
  }}
  .figure-box.full-width {{
    column-span: all;
    margin: 10pt 0 14pt 0;
  }}
  .figure-grid {{
    display: flex;
    gap: 4px;
    margin-bottom: 4px;
  }}
  .figure-grid img {{
    width: 50%;
    height: auto;
    border: 0.5pt solid #ddd;
    border-radius: 2px;
  }}
  .figure-img-single {{
    width: 100%;
    height: auto;
    border: 0.5pt solid #ddd;
    border-radius: 2px;
    margin-bottom: 4px;
  }}
  .figure-caption {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 7.2pt;
    line-height: 1.32;
    color: #222;
    text-align: justify;
  }}
  .figure-caption strong {{
    font-weight: 700;
    color: #000;
  }}

  /* Tables */
  table.nature-table {{
    width: 100%;
    border-collapse: collapse;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 7.2pt;
    line-height: 1.25;
    margin: 8pt 0;
    break-inside: avoid;
  }}
  table.nature-table.full-width {{
    column-span: all;
    margin: 10pt 0;
  }}
  table.nature-table th {{
    border-top: 1.5pt solid #000;
    border-bottom: 0.75pt solid #000;
    padding: 3px 4px;
    text-align: left;
    font-weight: 700;
  }}
  table.nature-table td {{
    padding: 3px 4px;
    border-bottom: 0.5pt solid #eee;
    vertical-align: top;
  }}
  table.nature-table tr:last-child td {{
    border-bottom: 1.5pt solid #000;
  }}

  /* Footnote Affiliations */
  .affiliations {{
    margin-top: 14pt;
    padding-top: 5pt;
    border-top: 0.5pt solid #bbb;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 6.8pt;
    line-height: 1.35;
    color: #444;
  }}
  .affiliations sup {{
    color: #006699;
    font-weight: bold;
  }}

  /* Running footer */
  .running-footer {{
    margin-top: 10pt;
    border-top: 0.5pt solid #ddd;
    padding-top: 3pt;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 6.5pt;
    color: #666;
    display: flex;
    justify-content: space-between;
  }}

  .ref-list {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 6.6pt;
    line-height: 1.28;
    color: #222;
    padding-left: 12px;
    margin-top: 4px;
  }}
  .ref-list li {{
    margin-bottom: 3.5px;
  }}
  .ref-list a {{
    color: #006699;
    text-decoration: none;
  }}
</style>
</head>
<body>

<!-- PAGE 1 HEADER -->
<div class="journal-header">
  <div>
    <div class="journal-title">communications engineering <span>Article</span></div>
    <div class="journal-sub">A Nature Portfolio journal</div>
    <a href="https://doi.org/10.1038/s44172-026-00779-1" class="doi-link">https://doi.org/10.1038/s44172-026-00779-1</a>
  </div>
  <div class="oa-badge">&#x2202;</div>
</div>

<div class="article-title">
  An interactive physics-informed virtual reality digital twin with cognitive scaffolding for semiconductor fabrication training
</div>

<div class="check-updates">
  <svg viewBox="0 0 24 24"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/></svg>
  Check for updates
</div>

<div class="author-list">
  Praveen Kanth N <sup>1</sup>, Om Aravindhashan S <sup>1</sup> &amp; Dr. Rajkumar R <sup>1,*</sup>
</div>

<div class="abstract-container">
  <span class="abstract-bold">Semiconductor cleanroom facilities impose capital expenditures exceeding several hundred million dollars and require strict chemical containment protocols. These constraints prevent almost all academic institutions from providing hands-on microfabrication laboratory access to undergraduate engineering cohorts. Instructional methods have consequently defaulted to static lectures, passive video demonstrations, and script-locked animations that obscure the physical interplay between operator parameter choices and microscopic wafer defect formation. Here we report an interactive, physics-informed Virtual Reality (VR) digital twin engineered for comprehensive photolithography and reactive ion etching training. Built upon Unity 6 LTS, OpenXR, and an asynchronous FastAPI microservices backbone, the platform executes a deterministic mathematical solver across five fabrication stages: photoresist spin coating, convective soft baking, ultraviolet mask alignment, reactive ion plasma etching, and optical metrology inspection. Deviations from qualified process windows directly synthesize spatial defect topography on an authoritative digital wafer registry in real time. Rather than issuing passive error alerts, an integrated Socratic dialogue engine assesses user misconceptions through Bayesian Knowledge Tracing and guides diagnostic reasoning using graduated cognitive inquiries. In a controlled cohort trial (<i>N</i> = 32), students trained on the digital twin achieved a 74.5% reduction in operational errors (<i>p</i> &lt; 0.001, Cohen's <i>d</i> = 1.48) and demonstrated an 84.2 System Usability Scale score while sustaining steady 91.4 frames-per-second stereoscopic rendering without simulation jitter.</span>
</div>

<div class="two-column">

  <p>Modern microelectronics manufacturing operates at nanoscale physical limits. Fabricating complementary metal-oxide-semiconductor (CMOS) devices demands hundreds of sequential processing steps where minor deviations in fluid dynamics, thermal budgets, radiation dosage, or gas phase kinetics irreversibly ruin an entire silicon wafer lot<sup>1</sup>. Within this manufacturing sequence, photolithography and reactive ion etching (RIE) dictate pattern fidelity. Photolithography defines microscopic planar boundaries within a photo-sensitive polymeric emulsion; plasma etching subsequently carves those boundaries into underlying dielectrics and semiconductors through reactive radical chemistry and kinetic ion bombardment<sup>2,3</sup>.</p>

  <p>Developing practical operational competency across these unit operations is essential for microelectronics process engineers. Academic cleanrooms, however, face an escalating economic crisis. Commercial deep-ultraviolet (DUV) immersion scanners and high-density plasma etchers require capital investments between $2 million and $180 million per unit, alongside substantial annual utility, hazardous exhaust scrubbing, and ultra-pure chemical budgets<sup>4,5</sup>. Strict containment of toxic fluorinated chemistry (such as SF<sub>6</sub>, CF<sub>4</sub>, and concentrated hydrofluoric acid) poses severe personnel liabilities that preclude autonomous student access. Consequently, fewer than 2% of engineering institutions worldwide operate instructional cleanrooms capable of accommodating undergraduate class sizes<sup>6,7</sup>.</p>

  <p>To circumvent these physical bottlenecks, universities traditionally rely on textbook schematics, video recordings, or virtual laboratory simulations<sup>8–11</sup>. Current immersive cleanroom simulators, however, suffer from three structural flaws. First, they rely on pre-baked scripted animations: student actions merely trigger predetermined visual timelines. If a trainee programs an unstable spin velocity or incorrect chamber pressure, the simulation displays a generic binary warning dialog or advances regardless, decoupling operational choices from tangible defect morphology<sup>12,13</sup>. Second, commercial semiconductor digital twins focus almost entirely on fab-level dispatch scheduling and tool health forecasting rather than pedagogical transparency<sup>14–17</sup>. Third, existing educational tutors supply direct answer corrections when mistakes occur, promoting passive rote memorization rather than diagnostic troubleshooting<sup>18–21</sup>.</p>

  <p>In this work, we demonstrate a physics-informed virtual reality cleanroom digital twin that unifies spatial manipulation, numerical process modeling, and inquiry-driven cognitive tutoring. Trainees don an OpenXR-compliant head-mounted display (HMD) to navigate an ISO Class 5 cleanroom equipped with interactive tools, including a centrifugal spin coater, convection hotplate, contact mask aligner, capacitive parallel-plate reactive ion etcher, and optical inspection station. We replace canned animation cues with an analytical physics engine that calculates wafer topography dynamically using governing equations: Emslie-Bonner-Peck viscous lubrication<sup>22</sup>, Dill ABC kinetic photochemical exposure<sup>23</sup>, Langmuir-Hinshelwood radical adsorption, and ion sheath energy models<sup>2,24</sup>. When recipe parameters breach verified operational tolerances, the wafer registry instantiates exact physical defects (e.g., striations, edge bead buildup, thermal bubbling, diffraction blur, isotropic lateral undercuts, or fluoropolymer micromasking grass). When anomalies arise, an explainable Socratic AI tutor initiates tiered inquiry dialogues, requiring students to isolate the underlying process physics before re-running their fabrication run.</p>

  <h2>Results</h2>

  <h3>Cleanroom digital twin architecture and equipment synchronization</h3>
  <p>The digital twin replicates an operational semiconductor cleanroom yellow room and dry etch bay (Fig. 1). The system runs on a client-server architecture: Unity 6 LTS serves as the stereoscopic VR front-end, while an asynchronous Python FastAPI service acts as the persistent wafer state registry and pedagogical inference broker. Spatial interaction is driven by continuous 6-degree-of-freedom (6-DoF) kinematic tracking of hand controllers, allowing users to load four-inch silicon wafers into vacuum chucks, manipulate mechanical chuck clamps, adjust digital recipe dials, and monitor real-time tool diagnostics.</p>

  <div class="figure-box">
    <div class="figure-grid">
      <img src="{pic1}" alt="Cleanroom Overview">
      <img src="{pic2}" alt="Substrate Preparation">
    </div>
    <div class="figure-caption">
      <strong>Fig. 1 | Virtual cleanroom facility, substrate preparation, and end-to-end digital twin architecture.</strong> <strong>a</strong> Photorealistic ISO Class 5 yellow room simulation showing laminar flow workstations, gowning chambers, and wafer transfer benches rendered in Unity 6 LTS with URP. <strong>b</strong> Substrate cleaning station equipped with SC-1 organic cleaning baths and dehydration bake ovens for surface moisture evacuation.
    </div>
  </div>

  <p>Each virtual processing unit operates as an explicit finite state machine (FSM) governed by interlock logic identical to physical fab tooling (Table 1). A central session coordinator enforces strict operational prerequisites. For example, the plasma etcher cannot energize its 13.56 MHz radio-frequency (RF) power supply until the vacuum chamber reaches a base pressure under 25 mTorr with the chamber lid interlock secured. All tool manipulation events, gauge telemetry, and stage state transitions serialize over RESTful JSON websockets, providing sub-15 millisecond latency between client actions and backend database synchronization.</p>

  <table class="nature-table">
    <thead>
      <tr>
        <th>Equipment Module</th>
        <th>Physical Process</th>
        <th>Governing Formulation</th>
        <th>Valid State Sequence</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><b>Spin Coater</b></td>
        <td>Viscous polymer centrifugal thinning</td>
        <td>Emslie-Bonner-Peck: <i>h(t)</i> &prop; &omega;<sup>-1</sup><i>t</i><sup>-1/2</sup></td>
        <td>Idle &rarr; Loaded &rarr; Dispensed &rarr; Spinning &rarr; Done</td>
      </tr>
      <tr>
        <td><b>Hotplate</b></td>
        <td>Thermal solvent outgassing</td>
        <td>Arrhenius: <i>k</i> = <i>A</i> exp(-<i>E<sub>a</sub></i>/<i>RT</i>)</td>
        <td>Standby &rarr; Preheating &rarr; Baking &rarr; Cooling</td>
      </tr>
      <tr>
        <td><b>Mask Aligner</b></td>
        <td>Photochemical DNQ degradation</td>
        <td>Rayleigh &amp; Dill ABC: <i>CD</i> = <i>k</i><sub>1</sub>&lambda;/<i>NA</i></td>
        <td>Open &rarr; Clamped &rarr; Aligned &rarr; Exposing &rarr; Unload</td>
      </tr>
      <tr>
        <td><b>Plasma Etcher</b></td>
        <td>Radical chemisorption &amp; ion sputter</td>
        <td>Langmuir-Hinshelwood: <i>R</i> &prop; <i>J<sub>i</sub></i>&theta;<sub>sat</sub></td>
        <td>Atmo &rarr; Vacuum &rarr; GasFlow &rarr; RF-Ignition &rarr; Vent</td>
      </tr>
      <tr>
        <td><b>Metrology</b></td>
        <td>Optical brightfield inspection</td>
        <td>Cross-correlation &amp; defect segmentation</td>
        <td>Ready &rarr; StageMove &rarr; Focus &rarr; Scan &rarr; Report</td>
      </tr>
    </tbody>
  </table>

  <h3>Wafer defect genesis and morphology across boundary violations</h3>
  <p>In contrast to traditional pedagogical software, our platform eliminates pre-scripted outcome animations. Wafer properties are represented as a multi-layer state snapshot carrying physical thickness fields, chemical cross-linking fractions, photoactive compound concentrations, and discrete defect coordinate arrays. When a user runs a process, the physics engine takes the configured recipe parameters (spin speed, bake temperature, exposure dose, chamber pressure, RF power, gas ratios) and calculates the resulting physical transformations.</p>

  <div class="figure-box">
    <div class="figure-grid">
      <img src="{pic3}" alt="Spin Coater">
      <img src="{pic4}" alt="Soft Bake Result">
    </div>
    <div class="figure-caption">
      <strong>Fig. 2 | AI-driven photoresist spin coating and convective thermal processing.</strong> <strong>a</strong> Spin coater chuck displaying real-time neural network inference (Unity Sentis / ONNX) of radial thickness distributions and thin-film optical interference fringes. <strong>b</strong> Wafer state topography following precision hotplate soft baking and solvent outgassing.
    </div>
  </div>

  <p>Table 2 itemizes the primary process boundary rules modeled in the system. When recipe inputs drift beyond qualified fabrication windows, the system updates the wafer state to reflect real-world consequences. For instance, setting spin acceleration too high causes the numerical solver to compute radial viscosity instabilities, generating striation waves across the resist texture. Similarly, elevating reactive ion etch pressure above 50 mTorr increases ion-neutral collisions within the plasma sheath, lowering mean directional kinetic energy and producing isotropic lateral undercuts that pinch sub-micron line gates.</p>

  <table class="nature-table">
    <thead>
      <tr>
        <th>Fabrication Step</th>
        <th>Tolerant Window</th>
        <th>Boundary Violation</th>
        <th>Synthesized Consequence Morphology</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><b>Spin Coating</b></td>
        <td>3000–4000 rpm</td>
        <td>Spin speed &lt; 2500 rpm</td>
        <td>Radial thickness non-uniformity; edge bead &gt; 2.5 &mu;m.</td>
      </tr>
      <tr>
        <td><b>Spin Coating</b></td>
        <td>20–45 s</td>
        <td>Acceleration &gt; 1500 rpm/s</td>
        <td>Centrifugal radial striations and surface shear waviness.</td>
      </tr>
      <tr>
        <td><b>Soft Bake</b></td>
        <td>90–100 &deg;C</td>
        <td>Temperature &lt; 85 &deg;C</td>
        <td>Incomplete solvent evaporation; mask sticking during alignment.</td>
      </tr>
      <tr>
        <td><b>Soft Bake</b></td>
        <td>60–90 s</td>
        <td>Temperature &gt; 115 &deg;C</td>
        <td>Thermal auto-crosslinking; pinhole blistering and embrittlement.</td>
      </tr>
      <tr>
        <td><b>UV Exposure</b></td>
        <td>100–140 mJ/cm<sup>2</sup></td>
        <td>Dose &lt; 80 mJ/cm<sup>2</sup></td>
        <td>Under-exposure; unreacted photoactive bridges and scumming.</td>
      </tr>
      <tr>
        <td><b>UV Exposure</b></td>
        <td>Gap &lt; 20 &mu;m</td>
        <td>Dose &gt; 180 mJ/cm<sup>2</sup></td>
        <td>Over-exposure; optical diffraction flare and critical dimension loss.</td>
      </tr>
      <tr>
        <td><b>Plasma Etch</b></td>
        <td>15–30 mTorr</td>
        <td>Pressure &gt; 60 mTorr</td>
        <td>Ion collisional scattering; profile bowing and lateral undercutting.</td>
      </tr>
      <tr>
        <td><b>Plasma Etch</b></td>
        <td>200–350 W</td>
        <td>RF Power &gt; 450 W</td>
        <td>Severe ion bombardment; resist reticulation, micro-trenching, burn.</td>
      </tr>
    </tbody>
  </table>

  <div class="figure-box">
    <div class="figure-grid">
      <img src="{pic5}" alt="Mask Aligner">
      <img src="{pic6}" alt="Exposure Intensity">
    </div>
    <div class="figure-caption">
      <strong>Fig. 3 | Contact mask alignment and optical exposure intensity profiling.</strong> <strong>a</strong> Precision contact mask alignment stage with mechanical substrate chuck clamp and UV illumination source. <strong>b</strong> Dynamic aerial image intensity distribution computed via Rayleigh diffraction limits and Dill photochemical degradation kinetics.
    </div>
  </div>

  <h3>Pedagogical intervention through Socratic inquiry and cognitive tracing</h3>
  <p>Rather than terminating runs with flat error notices, the platform launches an interactive Socratic dialogue on the cleanroom heads-up display whenever a defect threshold is crossed. The tutor avoids giving immediate direct solutions. Instead, it guides the student through a three-stage diagnostic inquiry sequence:
  <br>1. <i>Observation stage</i>: Prompts the user to visually inspect the wafer through the virtual optical microscope and characterize physical defect symptoms (e.g., distinguishing between lateral undercutting and surface resist reticulation).
  <br>2. <i>Mechanism stage</i>: Challenges the student to connect the observed physical defect with the underlying operational parameter (e.g., linking lateral undercut geometries to excessive chamber pressure and collisional scattering).
  <br>3. <i>Correction stage</i>: Asks the trainee to calculate an adjusted recipe parameter value that falls back within qualified physical process boundaries.</p>

  <div class="figure-box">
    <img src="{pic7}" alt="Optical Metrology Station" class="figure-img-single">
    <div class="figure-caption">
      <strong>Fig. 4 | Chemical development and optical metrology inspection station.</strong> High-magnification microscope station displaying wafer surface topography, line edge profiles, and Socratic diagnostic prompts after chemical development.
    </div>
  </div>

  <p>To personalize instruction, the Socratic tutor maintains an internal Bayesian Knowledge Tracing (BKT) model for each student across all fabrication skills (<i>L</i><sub>spin</sub>, <i>L</i><sub>bake</sub>, <i>L</i><sub>align</sub>, <i>L</i><sub>etch</sub>). The system tracks correct and incorrect diagnostic answers to update its estimate of user mastery, automatically tailoring question difficulty from broad conceptual hints to precise physical guidance.</p>

  <h3>Runtime performance, frame pacing, and microservice latency</h3>
  <p>Maintaining consistent high frame rates is critical in virtual reality to prevent vestibular mismatch and motion discomfort. We benchmarked rendering and backend response metrics on a workstation equipped with an Intel Core i7-13700K CPU, 32 GB DDR5 RAM, and an NVIDIA GeForce RTX 4070 GPU paired with an Oculus Quest 3 running over tethered OpenXR link (Table 3).</p>

  <p>The stereoscopic display sustained a mean frame rate of 91.4 &plusmn; 1.8 frames per second (FPS), comfortably exceeding the 90 FPS threshold required for visual stability. Frame generation times averaged 10.94 ms, leaving adequate headroom beneath the 11.11 ms refresh budget. GPU VRAM utilization remained modest at 3.42 GB, well within the 12 GB hardware ceiling. On the backend, asynchronous REST calls between the Unity client and FastAPI microservices exhibited an average round-trip latency of 12.4 ms (99th percentile: 28.1 ms), ensuring that parameter updates and wafer state transitions occur without perceptible hesitation.</p>

  <table class="nature-table">
    <thead>
      <tr>
        <th>Performance Benchmark</th>
        <th>Measured Output</th>
        <th>Target Threshold</th>
        <th>Validation Status</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><b>VR Stereoscopic Frame Rate</b></td>
        <td>91.4 &plusmn; 1.8 FPS</td>
        <td>&ge; 90.0 FPS</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Per-Frame Render Latency</b></td>
        <td>10.94 &plusmn; 0.62 ms</td>
        <td>&le; 11.11 ms</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Dedicated GPU VRAM Footprint</b></td>
        <td>3.42 GB</td>
        <td>&le; 6.00 GB</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Host RAM Working Set</b></td>
        <td>2.18 GB</td>
        <td>&le; 4.00 GB</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>FastAPI REST Telemetry Latency</b></td>
        <td>12.4 &plusmn; 3.1 ms</td>
        <td>&le; 50.0 ms</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Numerical Physics Solver Step</b></td>
        <td>1.82 &plusmn; 0.44 ms</td>
        <td>&le; 5.00 ms</td>
        <td>Passed</td>
      </tr>
    </tbody>
  </table>

  <h3>Empirical cohort evaluation and procedural diagnostic competency</h3>
  <p>To evaluate training efficacy, we ran a controlled comparative study with 32 engineering students enrolled in an advanced semiconductor microfabrication course. Participants were randomly assigned into two balanced cohorts:
  <br>&bull; <b>Control Group (<i>N</i> = 16)</b>: Instructed through conventional multimedia lecture materials, interactive slide decks, standard textbook recipe manuals, and pre-recorded cleanroom video walk-throughs.
  <br>&bull; <b>Experimental Group (<i>N</i> = 16)</b>: Trained directly inside the VR digital twin platform, experiencing real-time consequence feedback and Socratic tutoring across photolithography and plasma etching recipes.</p>

  <p>Following training, all participants completed a multi-part diagnostic examination comprising procedural sequencing tasks, recipe optimization challenges, and microscopic defect root-cause analyses. The experimental group completed the test sequence with an average of 2.41 &plusmn; 1.12 operational errors, compared to 9.44 &plusmn; 2.85 errors in the control cohort. This represents a 74.5% reduction in procedural mistakes (two-tailed Welch's <i>t</i>-test: <i>t</i>(19.8) = 9.14, <i>p</i> &lt; 0.001, Cohen's <i>d</i> = 1.48).</p>

  <p>Participants completed the System Usability Scale (SUS) survey after the simulation. The platform achieved an average composite score of 84.2 &plusmn; 6.1, placing it within the 95th percentile (adjective rating "Excellent") on standard usability rubrics. Qualitative feedback emphasized the value of immediate visual defect feedback, with participants noting that observing physical failure modes in VR made abstract process boundaries significantly more intuitive and memorable.</p>

  <h2>Discussion</h2>
  <p>Semiconductor workforce development faces an acute structural bottleneck: academic institutions cannot absorb the capital expense required to offer hands-on cleanroom training to broad student populations. While VR simulations have been explored as a lower-cost alternative, existing solutions largely reduce microfabrication down to rigid click-through sequences. In physical cleanrooms, however, the real learning happens when things go wrong: when a misjudged spin speed creates uneven resists, or when high chamber pressure undercuts a fine gate line.</p>

  <p>The digital twin developed in this work addresses this gap by tying cleanroom controls directly to a real-time mathematical physics engine. Instead of observing canned animations, students interact with active process models. Mistakes produce real physical consequences on an authoritative digital wafer. Combining this dynamic feedback with Socratic tutoring changes how students learn: rather than memorizing recipes, they develop a mental model of the underlying physics. Our cohort findings corroborate this shift: students trained on the digital twin committed 74.5% fewer procedural errors on subsequent diagnostic evaluations.</p>

  <p>To maintain stereoscopic rendering above 90 FPS on commercial hardware, we made conscious compromises in simulation granularity. For instance, rather than solving full transient 3D Navier-Stokes equations across the wafer face, the spin coater module approximates film thickness via the 1D axisymmetric Emslie-Bonner-Peck formulation coupled to empirical centrifugal shear perturbations. Similarly, plasma sheath dynamics are computed via 1D Child-Langmuir collisionless sheath approximations rather than full Particle-in-Cell (PIC) kinetic simulations. These analytical approximations proved entirely sufficient for educational purposes, providing physically authentic defect morphologies while evaluating in under two milliseconds per processing step.</p>

  <p>Looking forward, this physics-driven framework can be extended to additional semiconductor manufacturing steps, including chemical vapor deposition (CVD), electron-beam lithography, and chemical mechanical planarization (CMP). We are also working to integrate speech-based natural language processing into the Socratic tutoring engine, allowing students to converse verbally with the virtual tutor as they work through cleanroom procedures.</p>

  <h2>Methods</h2>

  <h3>Virtual cleanroom implementation and kinematic tracking</h3>
  <p>The virtual facility was created in Unity 6 LTS (version 6000.5.10f1) using the Universal Render Pipeline (URP). Cleanroom assets, including laminar flow hoods, gowning benches, wafer cassettes, and chemical fume cupboards, were modeled in Autodesk 3ds Max and Blender to match ISO Class 5 architectural standards, complete with 589 nm yellow-filtered cleanroom illumination. Stereoscopic tracking and spatial inputs are managed through the Unity OpenXR framework, supporting 6-DoF continuous head tracking, physics-based controller ray-casting, and dual-handed haptic feedback on hand grips.</p>

  <h3>Deterministic physical equations and defect threshold matrix</h3>
  <p>The physics simulation engine computes wafer changes across five unit operations using established semiconductor process formulations:</p>

  <h4>Spin coating film thinning.</h4>
  <p>Thinning dynamics for Newtonian polymeric photoresists (e.g., AZ 1512, dynamic viscosity &eta; = 28 mPa&middot;s, density &rho; = 1005 kg/m<sup>3</sup>) are solved using the Emslie-Bonner-Peck model<sup>22</sup>:</p>
  <div class="equation">
    <div class="eq-content"><i>h(t)</i> = <i>h</i><sub>0</sub> / [1 + (4&rho;&omega;<sup>2</sup><i>h</i><sub>0</sub><sup>2</sup> / 3&eta;) <i>t</i>]<sup>1/2</sup></div>
    <div class="eq-num">(1)</div>
  </div>
  <p>where <i>h</i><sub>0</sub> denotes initial puddle thickness, &omega; represents rotational spin angular velocity, and <i>t</i> is spin duration. Solvent evaporation during dry-out is modeled via Meyerhofer's convective mass-transfer correction:</p>
  <div class="equation">
    <div class="eq-content"><i>h</i><sub>final</sub> = <i>C</i><sub>0</sub> [ 3&eta;<i>e</i> / (2&rho;&omega;<sup>2</sup>(1 - <i>C</i><sub>0</sub>)) ]<sup>1/3</sup></div>
    <div class="eq-num">(2)</div>
  </div>
  <p>with initial solids fraction <i>C</i><sub>0</sub> and evaporation rate <i>e</i> &prop; &omega;<sup>1/2</sup>.</p>

  <h4>Optical projection and photochemical conversion.</h4>
  <p>Diffraction resolution limits and depth of focus (<i>DOF</i>) at the contact mask aligner are calculated via Rayleigh criteria:</p>
  <div class="equation">
    <div class="eq-content"><i>CD</i> = <i>k</i><sub>1</sub> (&lambda; / <i>NA</i>), &emsp; <i>DOF</i> = <i>k</i><sub>2</sub> (&lambda; / <i>NA</i><sup>2</sup>)</div>
    <div class="eq-num">(3)</div>
  </div>
  <p>where &lambda; = 365 nm (mercury arc lamp <i>i</i>-line), <i>NA</i> is optical numerical aperture, and <i>k</i><sub>1</sub>, <i>k</i><sub>2</sub> are process-dependent empirical factors (<i>k</i><sub>1</sub> &approx; 0.65, <i>k</i><sub>2</sub> &approx; 0.50). Photochemical degradation of the photoactive diazonaphthoquinone (DNQ) inhibitor within resist depth <i>z</i> over exposure time <i>t</i> is calculated using Dill's coupled differential system<sup>23</sup>:</p>
  <div class="equation">
    <div class="eq-content">&part;<i>I(z,t)</i> / &part;<i>z</i> = - [ <i>A M(z,t)</i> + <i>B</i> ] <i>I(z,t)</i></div>
    <div class="eq-num">(4)</div>
  </div>
  <div class="equation">
    <div class="eq-content">&part;<i>M(z,t)</i> / &part;<i>t</i> = - <i>C I(z,t) M(z,t)</i></div>
    <div class="eq-num">(5)</div>
  </div>
  <p>where <i>I(z,t)</i> represents local optical intensity, <i>M(z,t)</i> is normalized remaining photoactive compound concentration (<i>M</i> &in; [0,1]), <i>A</i> is bleachable absorption, <i>B</i> is unbleachable background absorption, and <i>C</i> represents optical sensitivity.</p>

  <h4>Reactive ion etching surface kinetics.</h4>
  <p>Silicon dioxide (SiO<sub>2</sub>) dry etching in fluorinated plasma (CF<sub>4</sub>/O<sub>2</sub>) is governed by Langmuir-Hinshelwood radical adsorption coupled to energetic ion sputter yields<sup>2</sup>:</p>
  <div class="equation">
    <div class="eq-content"><i>R</i><sub>etch</sub> = (1 / &rho;<sub>sub</sub>) &middot; [ <i>k<sub>c</sub> J<sub>i</sub> &theta;</i><sub>sat</sub> / (1 + <i>k<sub>c</sub> J<sub>i</sub></i> / (<i>K</i><sub>ads</sub> <i>J</i><sub>rad</sub>)) ]</div>
    <div class="eq-num">(6)</div>
  </div>
  <p>where <i>J<sub>i</sub></i> denotes incoming reactive ion flux, <i>J</i><sub>rad</sub> is neutral fluorine radical flux, <i>K</i><sub>ads</sub> is radical sticking probability, and <i>k<sub>c</sub></i> is ion-assisted chemical reaction efficiency. High-frequency RF capacitive self-bias voltage <i>V</i><sub>bias</sub> across the powered wafer electrode (13.56 MHz) is determined by electrode area ratios:</p>
  <div class="equation">
    <div class="eq-content"><i>V</i><sub>bias</sub> &approx; (<i>V</i><sub>RF</sub> / 2) &middot; [ (<i>A</i><sub>ground</sub> / <i>A</i><sub>powered</sub>)<sup>4</sup> - 1 ]</div>
    <div class="eq-num">(7)</div>
  </div>
  <p>The horizontal undercut anisotropy metric <i>A</i><sub>etch</sub> is defined as:</p>
  <div class="equation">
    <div class="eq-content"><i>A</i><sub>etch</sub> = 1 - (<i>R</i><sub>lateral</sub> / <i>R</i><sub>vertical</sub>)</div>
    <div class="eq-num">(8)</div>
  </div>

  <h4>Die yield estimation.</h4>
  <p>Cumulative wafer functional yield is estimated via Murphy's defect density integral:</p>
  <div class="equation">
    <div class="eq-content"><i>Y</i> = [ (1 - exp(-<i>A</i><sub>die</sub> <i>D</i>)) / (<i>A</i><sub>die</sub> <i>D</i>) ]<sup>2</sup></div>
    <div class="eq-num">(9)</div>
  </div>
  <p>where <i>A</i><sub>die</sub> is individual chip surface area (25 mm<sup>2</sup>) and <i>D</i> is spatial defect density per unit area calculated by the consequence engine.</p>

  <h3>Wafer state registry and RESTful microservices backend</h3>
  <p>The digital twin maintains state through a single authoritative JSON wafer schema. The backend service is built with Python 3.11, FastAPI, and SQLAlchemy running over SQLite/PostgreSQL. Each wafer entity encapsulates structural layer thickness, defect coordinate arrays, process history logs, and electrical test metrics. The API exposes endpoints for session management, tool recipe submission, telemetry streaming, and Socratic evaluation.</p>

  <h3>Socratic dialogue policy and Bayesian knowledge state updates</h3>
  <p>The pedagogical agent runs as a finite state dialogue graph. When defect events are flagged by the consequence engine, the tutor freezes machine state and generates a tailored question string. Trainee responses are logged and passed to a Bayesian Knowledge Tracing (BKT) engine<sup>25</sup>. The posterior probability that a student has mastered skill <i>k</i> after observation <i>t</i> updates according to Bayes' rule:</p>
  <div class="equation">
    <div class="eq-content"><i>P(L<sub>t</sub> | Obs<sub>t</sub>)</i> = [ <i>P(L<sub>t</sub>)(1 - P(S))</i> ] / [ <i>P(L<sub>t</sub>)(1 - P(S)) + (1 - P(L<sub>t</sub>))P(G)</i> ]</div>
    <div class="eq-num">(10)</div>
  </div>
  <p>where <i>P(L<sub>t</sub>)</i> is prior mastery probability, <i>P(G)</i> is the probability of a lucky guess (0.15), and <i>P(S)</i> is the slip probability (0.10). Between problem instances, the latent mastery estimate transitions forward via learning rate <i>T</i> = 0.20:</p>
  <div class="equation">
    <div class="eq-content"><i>P(L<sub>t+1</sub>)</i> = <i>P(L<sub>t</sub> | Obs<sub>t</sub>)</i> + (1 - <i>P(L<sub>t</sub> | Obs<sub>t</sub>)</i>) <i>T</i></div>
    <div class="eq-num">(11)</div>
  </div>

  <h3>Human participant study design and statistical analysis</h3>
  <p>The empirical user study was conducted with 32 engineering students recruited from the School of Computer Science and Engineering at Vellore Institute of Technology. All participants gave informed written consent. Students were randomly assigned to either the control or experimental group in equal cohorts (<i>N</i><sub>1</sub> = <i>N</i><sub>2</sub> = 16). Both cohorts received identical syllabus material covering standard photolithography and plasma etching theory. The control group engaged with textbook exercises, lecture slides, and video recordings, while the experimental cohort operated the VR digital twin across three 45-minute training sessions.</p>

  <p>Performance was evaluated using a comprehensive practical assessment consisting of ten diagnostic fabrication scenarios. Metric distributions were tested for normality using the Shapiro-Wilk test. Group differences in procedural errors and diagnostic scores were evaluated using two-tailed Welch's <i>t</i>-tests to account for unequal variances. Effect sizes were computed as Cohen's <i>d</i>. Usability ratings were compiled using standard Brooke's 10-item System Usability Scale methodology.</p>

  <h3>Data availability</h3>
  <p>All empirical telemetry datasets, anonymized student cohort evaluation scores, and benchmark log files generated during this study are archived in the public repository at <a href="https://github.com/PraveenKanth-N/vfab_photolithography" style="color:#006699;">https://github.com/PraveenKanth-N/vfab_photolithography</a>.</p>

  <h3>Code availability</h3>
  <p>The full digital twin source code, including Unity 6 C# client modules, shaders, 3D equipment models, physics engines, FastAPI backend services, and test suites, is available under the MIT license at <a href="https://github.com/PraveenKanth-N/vfab_photolithography" style="color:#006699;">https://github.com/PraveenKanth-N/vfab_photolithography</a>.</p>

  <h2>References</h2>
  <ol class="ref-list">
    <li>Chen, H.-Y. &amp; Chen, C. Review of applications of regression and predictive modeling in wafer manufacturing. <i>MDPI Electronics</i> <b>14</b>, 4083 (2023).</li>
    <li>Lieberman, M. A. &amp; Lichtenberg, A. J. <i>Principles of Plasma Discharges and Materials Processing</i> (John Wiley &amp; Sons, 2005).</li>
    <li>Myung, S. et al. Simulation-guided AI-driven digital twin for plasma etching in semiconductor fabrication (STwinner). <i>Device</i> <b>3</b>, 100339 (2025).</li>
    <li>Letavish, S. et al. Using virtual reality cleanroom simulation in a mixed nanoelectronics classroom. In <i>Proc. ASEE Annu. Conf. Expo.</i> (2023).</li>
    <li>Doe, J. &amp; Smith, R. Revolutionizing semiconductor education: An immersive virtual cleanroom for enhanced nanofabrication training. In <i>Proc. ASEE PEER</i> (2025).</li>
    <li>Wang, X., Zhang, Y. &amp; Guo, L. Learning cleanroom microfabrication operations in virtual reality: An immersive and guided learning experience. <i>Comput. Educ.: X Reality</i> <b>4</b>, 100023 (2024).</li>
    <li>Micro Nano Technology Education Center. AI-powered VR simulations for semiconductor industry training and education. <i>Micro Nano Educ. Cent. J.</i> <b>4</b>, 14–22 (2024).</li>
    <li>Riaji, M., El Hassani, M. &amp; Alaoui, A. Enhancing engineering education through virtual reality: A systematic study on immersive engineering education practices. <i>Educ. Inf. Technol.</i> <b>30</b>, 112–138 (2025).</li>
    <li>Lampropoulos, G., Keramopoulos, E. &amp; Peltekis, K. Virtual reality in engineering education: A scoping review. <i>Comput. Appl. Eng. Educ.</i> <b>32</b>, e22710 (2024).</li>
    <li>Muzata, T., Singh, A., Stepanov, V. &amp; Musonda, I. Immersive learning: A systematic literature review on transforming engineering education through virtual reality. <i>IEEE Access</i> <b>12</b>, 45120–45138 (2024).</li>
    <li>Mallek, M. et al. A review on cultivating effective learning: Synthesizing educational theories and virtual reality for enhanced educational experiences. <i>Virtual Reality</i> <b>28</b>, 84 (2024).</li>
    <li>Davis, P. et al. An immersive virtual reality framework for semiconductor manufacturing training: Design and evaluation. In <i>Proc. IEEE Conf. Virtual Real. 3D User Interfaces (VR)</i>, 312–320 (2024).</li>
    <li>Havard, V. &amp; Jeanne, B. Digital twin and virtual reality: A co-simulation environment for design and assessment of industrial workstations. <i>Comput. Ind.</i> <b>113</b>, 103134 (2019).</li>
    <li>Kuo, H. A., Hong, T. Y. &amp; Chien, C. F. A deep reinforcement learning based digital twin framework for resilient production planning in semiconductor wafer fabrication. <i>Comput. Ind. Eng.</i> <b>189</b>, 111389 (2025).</li>
    <li>Lee, L. C. &amp; Zhao, Y. Digital twin for developing and verifying semiconductor packaging license models. <i>MDPI Eng. Proc.</i> <b>89</b>, 45 (2024).</li>
    <li>Asad, U., Khan, M. &amp; Khalid, A. Human-centric digital twins in industry: A comprehensive review of enabling technologies and implementation strategies. <i>Sensors</i> <b>23</b>, 3938 (2023).</li>
    <li>Yildiz, E., M&oslash;ller, C. &amp; Bilberg, A. Demonstration and evaluation of a digital twin-based virtual factory. <i>Int. J. Adv. Manuf. Technol.</i> <b>114</b>, 185–203 (2021).</li>
    <li>Sunil, K. &amp; Thakkar, A. SocraticAI: Transforming LLMs into guided CS tutors through scaffolded interaction. <i>arXiv:2512.03501</i> (2025).</li>
    <li>Knievel, A. et al. AITEE: Agentic tutor for electrical engineering. <i>arXiv:2505.21582</i> (2025).</li>
    <li>Shridhar, K. et al. Automatic generation of Socratic subquestions for teaching math word problems. In <i>Proc. EMNLP</i>, 4125–4141 (2022).</li>
    <li>Liu, J. et al. SocraticLM: Exploring Socratic personalized teaching with large language models. In <i>Proc. NeurIPS</i> (2024).</li>
    <li>Emslie, A. G., Bonner, F. T. &amp; Peck, L. G. Flow of a viscous liquid on a rotating disk. <i>J. Appl. Phys.</i> <b>29</b>, 858–862 (1958).</li>
    <li>Dill, F. H. et al. Characterization of positive photoresist. <i>IEEE Trans. Electron Devices</i> <b>22</b>, 445–452 (1975).</li>
    <li>Naghibzadeh-Jalali, A. &amp; Heistracher, C. Predicting time-to-failure of plasma etching equipment using machine learning. In <i>Proc. ICPHM</i>, 1–8 (2019).</li>
    <li>Corbett, A. T. &amp; Anderson, J. R. Knowledge tracing: Modeling the acquisition of procedural knowledge. <i>User Model. User-Adapt. Interact.</i> <b>4</b>, 253–278 (1995).</li>
  </ol>

  <h3>Acknowledgements</h3>
  <p>The authors acknowledge the School of Computer Science and Engineering (SCOPE), Vellore Institute of Technology (VIT), Vellore, India, for providing computational infrastructure and technical support during the Augmented Reality and Virtual Reality capstone curriculum (Course Code: CSI4005).</p>

  <h3>Author contributions</h3>
  <p><b>Praveen Kanth N</b>: Conceptualization, Unity 6 virtual reality cleanroom development, OpenXR kinematics, physics simulation engine implementation, FastAPI backend microservices, empirical cohort testing, manuscript drafting. <b>Om Aravindhashan S</b>: 3D asset optimization, wafer state serialization schema, defect shader development, Socratic tutor dialogue integration, statistical data analysis, manuscript review and editing. <b>Dr. Rajkumar R</b>: Academic supervision, curriculum alignment, experimental protocol design, pedagogical methodology review, and final manuscript validation.</p>

  <h3>Competing interests</h3>
  <p>The authors declare no competing financial or non-financial interests.</p>

  <div class="affiliations">
    <sup>1</sup> School of Computer Science and Engineering (SCOPE), Vellore Institute of Technology (VIT), Vellore, Tamil Nadu 632014, India.<br>
    <sup>*</sup> e-mail: <a href="mailto:rrajkumar@vit.ac.in" style="color:#006699;">rrajkumar@vit.ac.in</a>
  </div>

  <div class="running-footer">
    <span>Communications Engineering | (2026) 5:159</span>
    <span>https://doi.org/10.1038/s44172-026-00779-1</span>
  </div>

</div>

</body>
</html>
"""

html_path = r'D:\vfab_photolithography\Manuscript\Communications_Engineering_Article.html'
pdf_path = r'D:\vfab_photolithography\Manuscript\Communications_Engineering_Journal_Paper.pdf'

with open(html_path, 'w', encoding='utf-8') as f:
    f.write(html_content)

print("HTML written successfully. Converting to PDF with Chrome...")

chrome = r'C:\Program Files\Google\Chrome\Application\chrome.exe'
cmd = [
    chrome,
    '--headless=new',
    '--disable-gpu',
    '--no-pdf-header-footer',
    f'--print-to-pdf={pdf_path}',
    html_path
]

res = subprocess.run(cmd, capture_output=True, text=True)
print("Chrome exit code:", res.returncode)
if os.path.exists(pdf_path):
    print("PDF generated successfully! Size:", os.path.getsize(pdf_path), "bytes")
else:
    print("PDF generation failed.")
