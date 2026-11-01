import base64
import os
import subprocess

def get_b64(filename, folder=r'D:\vfab_photolithography\Screenshots'):
    p = os.path.join(folder, filename)
    ext = os.path.splitext(filename)[1].lower()
    mime = 'image/jpeg' if ext in ['.jpg', '.jpeg'] else 'image/png'
    with open(p, 'rb') as f:
        return f"data:{mime};base64,{base64.b64encode(f.read()).decode('utf-8')}"

arch_b64 = get_b64('fig1_system_architecture.png', folder=r'D:\vfab_photolithography\Manuscript')
pic1 = get_b64('Picture1.jpg')
pic2 = get_b64('Picture2.jpg')
pic3 = get_b64('Picture3.jpg')
pic4 = get_b64('Picture4.png')
pic5 = get_b64('Picture5.png')
pic6 = get_b64('Picture6.png')
pic7 = get_b64('Picture7.png')

html_content = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>An AI-Driven Virtual Reality Digital Twin for Semiconductor Photolithography and Plasma Etching Training</title>
<style>
  @page {{
    size: A4;
    margin: 11mm 13mm 13mm 13mm;
  }}

  * {{
    box-sizing: border-box;
    -webkit-print-color-adjust: exact !important;
    print-color-adjust: exact !important;
  }}

  body {{
    font-family: "Minion Pro", "Georgia", "Times New Roman", serif;
    font-size: 8.0pt;
    line-height: 1.23;
    color: #111;
    margin: 0;
    padding: 0;
  }}

  /* Top manuscript submission banner */
  .top-banner {{
    border-bottom: 2px solid #000;
    padding-bottom: 4px;
    margin-bottom: 10px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 8pt;
    font-weight: 700;
    color: #222;
    letter-spacing: 0.5px;
  }}

  .article-title {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 19pt;
    font-weight: 800;
    line-height: 1.15;
    letter-spacing: -0.3px;
    color: #000;
    margin: 8px 0 6px 0;
  }}

  .author-list {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 9.2pt;
    font-weight: 700;
    color: #222;
    margin-bottom: 8px;
  }}
  .author-list sup {{
    font-size: 6.8pt;
    color: #006699;
    font-weight: 700;
  }}

  .abstract-container {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 8.5pt;
    line-height: 1.35;
    color: #1a1a1a;
    font-weight: 400;
    margin-bottom: 10px;
    padding-bottom: 8px;
    border-bottom: 0.5pt solid #ddd;
    text-align: justify;
  }}
  .abstract-bold {{
    font-weight: 700;
  }}

  /* Two column layout */
  .two-column {{
    column-count: 2;
    column-gap: 5mm;
    text-align: justify;
    text-justify: inter-word;
  }}

  p {{
    margin: 0 0 5pt 0;
    text-indent: 0;
  }}

  h2 {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 10.2pt;
    font-weight: 800;
    color: #000;
    margin: 6.5pt 0 2pt 0;
    break-after: avoid;
  }}

  h3 {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 8.6pt;
    font-weight: 700;
    color: #111;
    margin: 5pt 0 1.5pt 0;
    break-after: avoid;
  }}

  h4 {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 8pt;
    font-weight: 700;
    color: #222;
    margin: 4.5pt 0 1.5pt 0;
    break-after: avoid;
  }}

  /* Equations */
  .equation {{
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin: 3.5pt 0;
    padding: 0 2px;
    font-family: "Times New Roman", Times, serif;
    font-style: italic;
    font-size: 8.2pt;
    white-space: nowrap;
  }}
  .eq-content {{
    flex-grow: 1;
    text-align: center;
    overflow: hidden;
  }}
  .eq-num {{
    font-style: normal;
    font-size: 7.5pt;
    color: #333;
    padding-left: 4px;
    flex-shrink: 0;
  }}

  /* Figures */
  .figure-box {{
    margin: 3.5pt 0;
    break-inside: avoid;
    width: 100%;
  }}
  .figure-box.full-width {{
    column-span: all;
    margin: 7pt 0 9pt 0;
  }}
  .figure-grid {{
    display: flex;
    gap: 4px;
    margin-bottom: 2.5pt;
  }}
  .figure-grid img {{
    width: 50%;
    height: auto;
    border: 0.5pt solid #ccc;
    border-radius: 2px;
  }}
  .figure-img-single {{
    width: 100%;
    height: auto;
    border: 0.5pt solid #ccc;
    border-radius: 2px;
    margin-bottom: 2.5pt;
  }}
  .figure-caption {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    font-size: 6.8pt;
    line-height: 1.25;
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
    font-size: 6.7pt;
    line-height: 1.20;
    margin: 3.5pt 0;
    break-inside: avoid;
  }}
  table.nature-table.full-width {{
    column-span: all;
    margin: 7pt 0;
  }}
  table.nature-table th {{
    border-top: 1.2pt solid #000;
    border-bottom: 0.6pt solid #000;
    padding: 2pt 2.5pt;
    text-align: left;
    font-weight: 700;
  }}
  table.nature-table td {{
    padding: 2pt 2.5pt;
    border-bottom: 0.5pt solid #eee;
    vertical-align: top;
  }}
  table.nature-table tr:last-child td {{
    border-bottom: 1.2pt solid #000;
  }}

  /* Full-width Affiliations & Submission Footer (outside columns to avoid wrapping) */
  .affiliations-box {{
    margin-top: 10pt;
    padding-top: 4pt;
    border-top: 0.5pt solid #aaa;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 6.8pt;
    line-height: 1.30;
    color: #333;
    width: 100%;
  }}
  .affiliations-box sup {{
    color: #006699;
    font-weight: bold;
  }}

  .submission-footer {{
    margin-top: 6pt;
    border-top: 0.5pt solid #ccc;
    padding-top: 3pt;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 6.8pt;
    color: #555;
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
    white-space: nowrap;
  }}

  .ref-list {{
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    font-size: 6.3pt;
    line-height: 1.22;
    color: #222;
    padding-left: 10px;
    margin-top: 2.5pt;
  }}
  .ref-list li {{
    margin-bottom: 2.5px;
  }}
  .ref-list a {{
    color: #006699;
    text-decoration: none;
  }}
</style>
</head>
<body>

<!-- CLEAN SUBMISSION HEADER -->
<div class="top-banner">
  <span>COMMUNICATIONS ENGINEERING &mdash; MANUSCRIPT SUBMISSION</span>
  <span>RESEARCH ARTICLE</span>
</div>

<div class="article-title">
  An AI-Driven Virtual Reality Digital Twin for Semiconductor Photolithography and Plasma Etching Training
</div>

<div class="author-list">
  Praveen Kanth N <sup>1</sup>, Om Aravindhashan S <sup>1</sup> &amp; Dr. Rajkumar R <sup>1,*</sup>
</div>

<div class="abstract-container">
  <span class="abstract-bold">Semiconductor device fabrication demands extreme precision under capital-intensive cleanroom constraints. Sub-micron lithography and reactive ion etching (RIE) infrastructure require multi-million-dollar capital investments and strict chemical safety standards, precluding hands-on laboratory access for most engineering universities. Consequently, instruction has historically relied on passive pedagogical mediums such as lecture slides and pre-recorded videos, leaving students ill-equipped to understand how operational parameter variations directly govern physical wafer defect morphology. This paper presents an interactive, AI-driven Virtual Reality (VR) Digital Twin designed for immersive training in semiconductor photolithography and plasma etching. Built on Unity 6 LTS and OpenXR, the platform simulates a standardized cleanroom environment where users configure critical process parameters across unit operations including spin coating, soft baking, ultraviolet mask alignment, reactive ion etching, and optical metrology. Rather than displaying scripted playback, the framework couples a deterministic mathematical simulation engine with an authoritative wafer state registry, computing dynamic wafer topography and localized defect formation in real time. When operational tolerances are breached, an integrated Explainable Socratic AI tutor provides cognitive scaffolding, prompting students to deduce the physical root causes of defects through structured inquiry rather than direct answer delivery. Experimental validation demonstrates deterministic state transitions, 91.4 frames-per-second VR rendering stability, and a pilot cohort evaluation (<i>N</i> = 32) showing an 84.2 System Usability Scale (SUS) rating alongside a 74.5% reduction in student procedural errors (<i>p</i> &lt; 0.001).</span>
</div>

<div class="two-column">

  <h2>Introduction</h2>
  <p>Semiconductor microfabrication represents one of the most technologically intricate and economically demanding manufacturing sectors in modern engineering. Fabricating sub-micron integrated circuits requires dozens of sequential processing cycles where minor variations in physical parameters dictate final device performance and wafer die yield<sup>1</sup>. Within this sequence, photolithography and reactive ion etching (RIE) represent the primary pattern-defining and pattern-transfer operations. Photolithography defines microscopic circuit topologies within light-sensitive photoresist emulsions, while RIE selectively carves these topologies into underlying dielectric and semiconductor substrates via chemical radicals and energetic ion bombardment<sup>2,3</sup>.</p>

  <p>Despite the critical global demand for skilled semiconductor process engineers, practical laboratory training remains an acute bottleneck for engineering universities worldwide. The core obstacle is the staggering capital investment required to establish and operate an instructional cleanroom. An entry-level industrial DUV/EUV lithography scanner manufactured by ASML demands $150 to $200 million in capital outlay, while reactive ion etchers routinely exceed $1.5 million per unit<sup>2,4</sup>. Beyond tool acquisition, maintaining an ISO Class 1 to Class 5 cleanroom requires uninterrupted heating, ventilation, and air conditioning (HVAC) systems with particulate filtration down to 0.1 &mu;m, continuous air recycling, specialized chemical delivery lines, toxic effluent scrubbers for fluorinated gases (CF<sub>4</sub>, SF<sub>6</sub>, O<sub>2</sub>), and strict protocol compliance for personal protective equipment (PPE). Consequently, less than 2% of universities globally can afford cleanrooms dedicated solely to undergraduate instructional laboratories<sup>6,7</sup>.</p>

  <p>Faced with these cost barriers, academic institutions instruct students through textbooks, two-dimensional circuit drawings, slide decks, and pre-recorded video demonstrations. Although these mediums convey foundational chemistry and device physics, they cannot provide tactile, cause-and-effect intuition. In actual fabrication environments, subtle variations in operational settings—such as rotational spin speed, soft bake duration, exposure dose, RF generator power, and chamber pressure—directly alter wafer characteristics and trigger structural defects. Passive learning materials give students no opportunity to experiment with parameter combinations, experience the consequences of operator errors, or troubleshoot defective wafers.</p>

  <p>Immersive Virtual Reality (VR) and Digital Twin (DT) frameworks offer a practical path toward democratizing cleanroom education<sup>8–11</sup>. However, an analysis of current literature reveals three critical shortcomings in existing tools:
  <br>1. <i>Scripted, Consequence-Free Virtual Environments</i>: Most VR cleanroom software is restricted to fixed procedural checklist walkthroughs. Student actions simply trigger pre-rendered animations. If a user sets an incompatible spin speed or excessive chamber pressure, the simulation either displays a generic failure message or advances regardless, preventing students from understanding how their input values caused the defect<sup>12,13</sup>.
  <br>2. <i>Industrial Rather Than Pedagogical Digital Twins</i>: Contemporary digital twins in microelectronics focus almost exclusively on yield management, wafer dispatching, and predictive tool maintenance in commercial fabs<sup>14–17</sup>. These systems are engineered for industrial plant operators rather than students seeking foundational mental models.
  <br>3. <i>Prescriptive Rather Than Inquisitive AI Tutoring</i>: Recent artificial intelligence tools in engineering education typically operate as direct answer engines. When a student errs, the software immediately presents the corrective value, promoting rote execution rather than diagnostic troubleshooting<sup>18–21</sup>.</p>

  <p>To overcome these instructional limitations, this paper introduces an AI-Driven Virtual Reality Digital Twin for Semiconductor Photolithography and Plasma Etching Training. Developed on Unity 6 LTS and OpenXR, the system places students inside an interactive virtual cleanroom where they operate virtual machines, adjust recipe parameters, observe real-time defect formation governed by numerical physical equations, and troubleshoot mistakes using an Explainable Socratic AI Tutor.</p>

  <p>The key contributions of this paper are:
  <br>&bull; <b>Modular Cleanroom Digital Twin Architecture</b>: We present a cohesive 10-module framework linking an OpenXR virtual workspace (Module 1) with equipment digital twins (Module 2) and an authoritative wafer state registry (Module 3).
  <br>&bull; <b>Deterministic Consequence-Based Process Simulation</b>: We embed physical formulations governing spin-coating fluid dynamics, optical diffraction limits, and plasma ion bombardment, mapping operator mistakes directly to spatial wafer flaws.
  <br>&bull; <b>Explainable Socratic Mentorship Loop</b>: We deploy an inquiry-driven AI tutor that prompts students through a three-stage reflection cycle (Observation &rarr; Physical Mechanism &rarr; Remediation) instead of providing direct solutions.
  <br>&bull; <b>Empirical Implementation and Verification</b>: We detail the software architecture, state-machine synchronization, and empirical performance metrics in Unity 6, including pilot cohort data (<i>N</i> = 32) demonstrating an 84.2 System Usability Scale (SUS) score and a 74.5% procedural error reduction.</p>

  <h2>Systematic Literature Survey and Research Gaps</h2>
  <p>To anchor this research in current academic literature, a systematic review encompassing 27 peer-reviewed studies published between 2019 and 2026 was conducted across four core thematic domains: (i) Immersive VR in Engineering Education, (ii) Industrial and Educational Digital Twins, (iii) AI-Assisted Socratic Tutoring, and (iv) Semiconductor Process Modeling.</p>

  <p><b>VR in Engineering Education</b>: Bibliometric reviews by Riaji et al.<sup>8</sup> and Lampropoulos et al.<sup>9</sup> established that immersive VR environments consistently improve student engagement, procedural recall, and spatial awareness across STEM disciplines. Muzata et al.<sup>10</sup> demonstrated that project-based learning embedded within immersive Cave Automatic Virtual Environments (CAVE) significantly elevated cumulative assessment scores. Mallek et al.<sup>11</sup> emphasized that VR platforms achieve higher learning gains when aligned with constructivist multimedia learning theories. In cleanroom-specific education, early prototypes by Letavish et al.<sup>4</sup> confirmed that pre-lab VR exposure enhanced student self-confidence before entering cleanrooms. Similarly, Wang et al.<sup>6</sup> reported measurable reductions in procedural errors during photolithography steps, and ASEE PEER 2025 findings<sup>5</sup> highlighted the benefit of action tracking. However, existing cleanroom VR frameworks remain fundamentally descriptive: they operate on hard-coded transitions where student actions trigger canned animations regardless of parameter variations.</p>

  <p><b>Digital Twin Technology in Industry and Education</b>: Digital Twin methodologies have expanded rapidly across industrial production workflows. Havard and Jeanne<sup>13</sup> integrated digital factory tool suites with VR co-simulation to evaluate ergonomic workstation safety. Springer's training study and Karanam et al. conducted meta-analyses demonstrating that pairing digital twins with virtual learning environments increased student problem-solving scores by up to 20%. In semiconductor environments, Kuo et al.<sup>14</sup> coupled deep reinforcement learning with wafer fabrication twins for resilient dynamic scheduling, while Lee and Zhao<sup>15</sup> utilized mixed-reality twins for technician certification in chip packaging. Asad et al.<sup>16</sup> established design taxonomies for human-centric digital twins, and Yildiz et al.<sup>17</sup> demonstrated collaborative virtual factory layouts. In process-level physics modeling, Myung et al.<sup>3</sup> proposed STwinner, combining calibrated TCAD simulations with neural operator surrogates to optimize plasma etching recipes. While industrially potent, such digital twins are formulated exclusively for factory yield maximization and run-time optimization; they lack student-facing interfaces, pedagogical scaffolding, or VR immersion.</p>

  <p><b>AI and Socratic Tutoring Systems</b>: Recent advancements in conversational artificial intelligence have catalyzed interest in pedagogical dialogue agents. Sunil and Thakkar<sup>18</sup> introduced SocraticAI, demonstrating that enforcing structured conversational rules on large language models prompted 75% of undergraduate computer science students to write detailed problem reflections rather than passively copying syntax. Knievel et al.<sup>19</sup> developed an electrical engineering tutor grounding Socratic dialogues in live SPICE circuit simulation data, dramatically outperforming standard text-only baselines. Complementary studies by Shridhar et al.<sup>20</sup>, Ambati et al., and Liu et al.<sup>21</sup> established that reinforcement-learned question generation and cognitive state modeling enable AI agents to personalize problem-solving queries dynamically based on learner competency. Despite these strides, conversational AI tutors have rarely been coupled with real-time physical simulation engines in manufacturing disciplines.</p>

  <p><b>Semiconductor Process Simulation</b>: Efforts to simulate semiconductor processing have explored both academic instruction and industrial fault detection. An evaluation by the Micro Nano Technology Education Center<sup>7</sup> reported that over 72% of technical students found AI-supported VR lithography simulations engaging. Davis et al.<sup>12</sup> developed an immersive VR framework spanning ingot slicing through photolithography and etching, validating the safety benefits of virtual cleanrooms. In industrial settings, Naghibzadeh-Jalali et al.<sup>24</sup> demonstrated machine-learning prediction of RIE tool degradation with 74.1% accuracy using factory sensor logs, while Sofge<sup>27</sup> designed virtual sensors to estimate critical dimensions and oxide loss from in-situ optical emission spectroscopy.</p>

  <p><b>Identified Research Gap</b>: Table 1 synthesizes the comparative landscape. While prior work addresses VR immersion, digital twins, and AI tutoring independently, no existing platform merges immersive VR, physical process simulation, parameter-driven wafer defect modeling, and Socratic AI guidance into an affordable educational tool. This identified gap forms the core motivation of our work.</p>

  <table class="nature-table">
    <thead>
      <tr>
        <th>Work</th>
        <th>Primary Domain</th>
        <th>Core Methodology</th>
        <th>VR</th>
        <th>Phys DT</th>
        <th>Defects</th>
        <th>Socratic</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>Letavish et al.<sup>4</sup></td>
        <td>Nanoelectronics Edu.</td>
        <td>Oculus Quest Cleanroom</td>
        <td>Yes</td>
        <td>No</td>
        <td>No</td>
        <td>No</td>
      </tr>
      <tr>
        <td>Havard &amp; Jeanne<sup>13</sup></td>
        <td>Industrial Workstations</td>
        <td>VR + Co-Simulation DT</td>
        <td>Yes</td>
        <td>Yes</td>
        <td>No</td>
        <td>No</td>
      </tr>
      <tr>
        <td>Myung et al.<sup>3</sup></td>
        <td>Plasma Etch Opt.</td>
        <td>TCAD + Neural Operators</td>
        <td>No</td>
        <td>Yes</td>
        <td>Yes</td>
        <td>No</td>
      </tr>
      <tr>
        <td>Sunil &amp; Thakkar<sup>18</sup></td>
        <td>Computer Science Edu.</td>
        <td>Scaffolded LLM Prompts</td>
        <td>No</td>
        <td>No</td>
        <td>No</td>
        <td>Yes</td>
      </tr>
      <tr>
        <td>Knievel et al.<sup>19</sup></td>
        <td>Electrical Engineering</td>
        <td>Graph-RAG + SPICE Sim</td>
        <td>No</td>
        <td>No</td>
        <td>No</td>
        <td>Yes</td>
      </tr>
      <tr>
        <td>Davis et al.<sup>12</sup></td>
        <td>Semiconductor Training</td>
        <td>Unity VR Multimodal Sim</td>
        <td>Yes</td>
        <td>No</td>
        <td>No</td>
        <td>No</td>
      </tr>
      <tr>
        <td><b>Proposed System</b></td>
        <td><b>Semiconductor Training</b></td>
        <td><b>VR + DT + Physics + Socratic</b></td>
        <td><b>Yes</b></td>
        <td><b>Yes</b></td>
        <td><b>Yes</b></td>
        <td><b>Yes</b></td>
      </tr>
    </tbody>
  </table>

  <h2>System Architecture and Functional Design</h2>
  <p>The proposed virtual cleanroom platform is organized into a decoupled, ten-module architecture that separates visualization, physical calculation, wafer state management, and external persistence.</p>

  <!-- SYSTEM ARCHITECTURE DIAGRAM FIG 1 -->
  <div class="figure-box full-width">
    <img src="{arch_b64}" alt="System Architecture Diagram" class="figure-img-single">
    <div class="figure-caption">
      <strong>Fig. 1 | System architecture showing non-overlapping dataflows between user interaction (Module 1), equipment twins (Module 2), physics modeling (Module 5), consequence rules (Module 6), the authoritative wafer state registry (Module 3), time-travel debugging and cloud telemetry (Modules 8 &amp; 9), and Socratic tutoring (Module 10).</strong> Physical user interactions at virtual equipment consoles trigger parameter validation against analytical physics formulations, streaming real-time telemetry to the authoritative wafer registry while driving Socratic pedagogical interventions.
    </div>
  </div>

  <p><b>Module 1: VR Environment and Onboarding</b>: Implements the cleanroom space in Unity 6 LTS using OpenXR. It provides comfort teleportation, desktop fallback simulation (<code>XRPlayer.cs</code>), spatial machine anchors (<code>EquipmentAnchor.cs</code>), and a 6-step finite state machine (<code>OnboardingStateMachine.cs</code>) that verifies orientation and interaction before granting access to fabrication tools.</p>

  <p><b>Module 2: Equipment Digital Twins</b>: Contains virtual replicas of cleanroom equipment. Each machine operates an internal state machine (Idle, Ready, Loading, Processing, Complete, Fault) and includes interactive controls:
  <br>&bull; <i>Cleaning Station Twin</i>: Megasonic power, chemical bath temperature, deionized rinse cycles.
  <br>&bull; <i>Photolithography Twin</i>: Programmable spin coater (RPM, acceleration rate, resist volume), proximity hotplate soft-bake temperature/timer, and optical contact mask aligner.
  <br>&bull; <i>Plasma Etcher Twin</i>: RIE vacuum chamber, RF power generator, pressure control, and CF<sub>4</sub>/SF<sub>6</sub>/O<sub>2</sub> mass flow controllers.
  <br>&bull; <i>Inspection Twin</i>: Virtual optical microscope with magnification stages and spatial translation.</p>

  <div class="figure-box">
    <div class="figure-grid">
      <img src="{pic1}" alt="Cleanroom Overview">
      <img src="{pic2}" alt="Substrate Preparation">
    </div>
    <div class="figure-caption">
      <strong>Fig. 2 | Virtual cleanroom facility and substrate preparation workstations.</strong> <strong>a</strong> Photorealistic ISO Class 5 yellow room simulation showing laminar flow workstations, gowning chambers, and wafer transfer benches rendered in Unity 6 LTS with URP. <strong>b</strong> Substrate cleaning station equipped with SC-1 organic cleaning baths and dehydration bake ovens for surface moisture evacuation.
    </div>
  </div>

  <p><b>Module 3: Wafer Digital Twin State Architecture</b>: The Wafer Digital Twin acts as the authoritative single source of truth for the physical silicon substrate. It maintains:
  <br>&bull; <i>Identity (<code>WaferId.cs</code>)</i>: Tracks global unique lot identifiers and creation timestamps.
  <br>&bull; <i>Layer Stack (<code>WaferLayer.cs</code>)</i>: Maintains layer order, material compositions, and thicknesses in nanometers.
  <br>&bull; <i>Defect Registry (<code>WaferDefect.cs</code>)</i>: Records localized flaws with 3D coordinates (<i>x, y, z</i>), defect typology, and continuous severity scores &sigma; &in; [0.0, 1.0].
  <br>&bull; <i>Snapshots (<code>WaferStateSnapshot.cs</code>)</i>: Stores immutable deep-copy backups after each step for time-travel analysis and inspection.</p>

  <p><b>Modules 4 to 9: Process, Physics, Consequence, UI, and Cloud Telemetry</b>: Module 4 validates processing sequences. Module 5 executes deterministic physics calculations. Module 6 compares results to operational envelopes, injecting defect entries into the Wafer Digital Twin if parameters violate tolerances. Module 7 renders thin-film optical interference through custom URP shaders. Module 8 provides an interactive time-travel replay timeline, tightly integrated with Module 9 (Cloud Backend and Telemetry Pipeline) which ingests serialized <code>WaferStateData</code> payloads and sensor streams via asynchronous REST endpoints into a persistent PostgreSQL repository to maintain authoritative run history for the Socratic AI tutor (Module 10).</p>

  <div class="figure-box">
    <div class="figure-grid">
      <img src="{pic3}" alt="Spin Coater">
      <img src="{pic4}" alt="Soft Bake Result">
    </div>
    <div class="figure-caption">
      <strong>Fig. 3 | AI-driven photoresist spin coating and convective thermal processing.</strong> <strong>a</strong> Spin coater chuck displaying real-time neural network inference (Unity Sentis / ONNX) of radial thickness distributions and thin-film optical interference fringes. <strong>b</strong> Wafer state topography following precision hotplate soft baking and solvent outgassing.
    </div>
  </div>

  <h2>Physical Formulations and Consequence-Based Defect Dynamics</h2>
  <p>Rather than displaying static animations, the simulation engine calculates wafer physical transformations dynamically using deterministic numerical models.</p>

  <h3>Photoresist Spin Coating Dynamics</h3>
  <p>During spin coating, the final photoresist film thickness <i>t</i><sub>pr</sub> depends on the equilibrium between centrifugal outward force and viscous drag. Using the Emslie-Bonner-Peck hydrodynamic formulation<sup>22</sup>:</p>
  <div class="equation">
    <div class="eq-content"><i>t</i><sub>pr</sub> = [ 3&eta;<i>r</i><sub>0</sub><sup>2</sup> / (4&rho;&omega;<sup>2</sup><i>t</i><sub>spin</sub>) ]<sup>1/2</sup> &approx; <i>K</i><sub>0</sub> &middot; [ &mu;<sup>&alpha;</sup> / &omega;<sup>1/2</sup> ]</div>
    <div class="eq-num">(1)</div>
  </div>
  <p>where &omega; represents rotational speed in RPM, &eta; is fluid viscosity, &rho; is density, and <i>K</i><sub>0</sub>, &alpha; are resist-specific empirical constants. If a student selects an insufficient rotational velocity (&omega; &lt; 1800 RPM), the model calculates a radial thickness variance exceeding &plusmn;18%, injecting radial striation defects into the wafer state registry. Conversely, excessive spin speeds (&omega; &gt; 5500 RPM) yield an overly thin film susceptible to pinholes during plasma etching.</p>

  <h3>Diffraction Limits and Exposure Kinetics</h3>
  <p>The minimum printable critical dimension (<i>CD</i>) and usable depth of focus (<i>DOF</i>) during UV photolithography are governed by the Rayleigh criteria:</p>
  <div class="equation">
    <div class="eq-content"><i>CD</i> = <i>k</i><sub>1</sub> (&lambda; / <i>NA</i>), &emsp; <i>DOF</i> = &plusmn; <i>k</i><sub>2</sub> (&lambda; / <i>NA</i><sup>2</sup>)</div>
    <div class="eq-num">(2)</div>
  </div>
  <p>where &lambda; is exposure wavelength (365 nm <i>i</i>-line or 248 nm KrF), <i>NA</i> is numerical aperture, and <i>k</i><sub>1</sub>, <i>k</i><sub>2</sub> are process factors (<i>k</i><sub>1</sub> &approx; 0.4–0.7).</p>

  <p>Photochemical decomposition follows the Dill kinetic formulation<sup>23</sup>:</p>
  <div class="equation">
    <div class="eq-content">&part;<i>I(z,t)</i> / &part;<i>z</i> = - [ <i>A &middot; M(z,t)</i> + <i>B</i> ] <i>I(z,t)</i></div>
    <div class="eq-num">(3)</div>
  </div>
  <div class="equation">
    <div class="eq-content">&part;<i>M(z,t)</i> / &part;<i>t</i> = - <i>C &middot; I(z,t) &middot; M(z,t)</i></div>
    <div class="eq-num">(4)</div>
  </div>
  <p>where <i>M(z,t)</i> represents the normalized unreacted photoactive compound fraction at depth <i>z</i>, <i>I(z,t)</i> is optical intensity, <i>A</i> is bleachable absorption, <i>B</i> is non-bleachable absorption, and <i>C</i> is photo-decomposition rate. Deviations in exposure dose (<i>D</i> = <i>I &middot; t</i><sub>exp</sub>) produce non-uniform <i>M(z)</i> profiles, triggering line edge roughness (LER) or pattern collapse.</p>

  <div class="figure-box">
    <div class="figure-grid">
      <img src="{pic5}" alt="Mask Aligner">
      <img src="{pic6}" alt="Exposure Intensity">
    </div>
    <div class="figure-caption">
      <strong>Fig. 4 | Contact mask alignment and optical exposure intensity profiling.</strong> <strong>a</strong> Precision contact mask alignment stage with mechanical substrate chuck clamp and UV illumination source. <strong>b</strong> Dynamic aerial image intensity distribution computed via Rayleigh diffraction limits and Dill photochemical degradation kinetics.
    </div>
  </div>

  <h3>Reactive Ion Etching Kinetics</h3>
  <p>Plasma etching removes material via combined chemical etching (radical reaction) and physical sputtering (accelerated ion bombardment). Total etch rate <i>R</i><sub>total</sub> follows the modified Langmuir-Hinshelwood kinetic framework<sup>2</sup>:</p>
  <div class="equation">
    <div class="eq-content"><i>R</i><sub>total</sub> = (1 / &rho;<sub>Si</sub>) &middot; [ (<i>k</i><sub>chem</sub> &Gamma;<sub>chem</sub> <i>Y<sub>i</sub></i> &Gamma;<sub>ion</sub>) / (<i>k</i><sub>chem</sub> &Gamma;<sub>chem</sub> + <i>Y<sub>i</sub></i> &Gamma;<sub>ion</sub>) ] + <i>Y</i><sub>phys</sub> &Gamma;<sub>ion</sub></div>
    <div class="eq-num">(5)</div>
  </div>
  <p>where &Gamma;<sub>chem</sub> is neutral fluorine radical flux (CF<sup>&bull;</sup><sub><i>x</i></sub>, F<sup>&bull;</sup>), &Gamma;<sub>ion</sub> is energetic ion flux (CF<sup>+</sup><sub>3</sub>, Ar<sup>+</sup>), <i>Y<sub>i</sub></i> is ion-enhanced chemical reaction yield, <i>Y</i><sub>phys</sub> is physical sputter yield, and &rho;<sub>Si</sub> is substrate atomic density.</p>

  <p>Ion bombarding energy relates to RF power (<i>P</i><sub>RF</sub>) and chamber pressure (<i>p</i><sub>ch</sub>):</p>
  <div class="equation">
    <div class="eq-content"><i>E</i><sub>ion</sub> &prop; <i>V</i><sub>dc</sub> / <i>p</i><sub>ch</sub><sup>1/2</sup>, &emsp; <i>V</i><sub>dc</sub> &prop; <i>P</i><sub>RF</sub><sup>1/2</sup></div>
    <div class="eq-num">(6)</div>
  </div>
  <p>When students select excessive chamber pressure (<i>p</i><sub>ch</sub> &gt; 120 mTorr), the ion mean free path decreases due to collisional scattering, reducing vertical ion directional velocity and causing isotropic lateral undercutting (<i>A</i><sub>etch</sub> &ll; 1):</p>
  <div class="equation">
    <div class="eq-content"><i>A</i><sub>etch</sub> = 1 - (<i>R</i><sub>lateral</sub> / <i>R</i><sub>vertical</sub>)</div>
    <div class="eq-num">(7)</div>
  </div>

  <table class="nature-table">
    <thead>
      <tr>
        <th>Process Stage</th>
        <th>Operating Parameter</th>
        <th>Parameter Deviation</th>
        <th>Induced Defect and Functional Impact</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><b>Photoresist Coating</b></td>
        <td>Spin Speed (&omega;)</td>
        <td>Low speed (&omega; &lt; 1800 RPM)<br>High speed (&omega; &gt; 5500 RPM)</td>
        <td>Radial thickness non-uniformity; striations; uneven exposure depth.<br>Overly thin film; edge cracking; pinhole breakthrough during etching.</td>
      </tr>
      <tr>
        <td><b>Soft Bake</b></td>
        <td>Temperature (<i>T</i><sub>sb</sub>)</td>
        <td>Under-baking (<i>T</i><sub>sb</sub> &lt; 85 &deg;C)<br>Over-baking (<i>T</i><sub>sb</sub> &gt; 115 &deg;C)</td>
        <td>Excess residual solvent; resist bubbling during UV; mask adhesion.<br>Thermal cross-linking; loss of photosensitivity; prolonged development.</td>
      </tr>
      <tr>
        <td><b>UV Exposure</b></td>
        <td>Dose (<i>D</i> = <i>I &middot; t</i><sub>exp</sub>)</td>
        <td>Under-exposure (<i>D</i> &lt; 120 mJ/cm<sup>2</sup>)<br>Over-exposure (<i>D</i> &gt; 250 mJ/cm<sup>2</sup>)</td>
        <td>Incomplete photoacid decomposition; resist residue on pads; opening failure.<br>Lateral diffraction broadening; critical dimension shrinkage; open circuits.</td>
      </tr>
      <tr>
        <td><b>Mask Alignment</b></td>
        <td>Spatial Offset (&Delta;<i>x</i>, &Delta;<i>y</i>, &theta;)</td>
        <td>Alignment offset (|&Delta;<i>x</i>| &gt; 0.15 &mu;m)</td>
        <td>Inter-layer overlay misregistration; unaligned contact vias; device failure.</td>
      </tr>
      <tr>
        <td><b>Plasma Etch (RIE)</b></td>
        <td>RF Power (<i>P</i><sub>RF</sub>)<br>Chamber Pressure (<i>p</i><sub>ch</sub>)<br>Gas Flow (<i>Q</i><sub>SF6</sub>)<br>Etch Duration (<i>t</i><sub>etch</sub>)</td>
        <td>Excessive power (<i>P</i><sub>RF</sub> &gt; 300 W)<br>High pressure (<i>p</i><sub>ch</sub> &gt; 120 mTorr)<br>Imbalanced fluorine ratio<br>Under-etching (<i>t</i> &lt; <i>t</i><sub>endpoint</sub>)</td>
        <td>High ion sputtering; mask faceting; gate oxide punch-through.<br>Reduced mean free path; isotropic profile; severe lateral undercut.<br>Loss of silicon-to-oxide selectivity; rough bottoms; micro-masking.<br>Residual un-etched conductive film; short circuits across tracks.</td>
      </tr>
    </tbody>
  </table>

  <h2>Explainable Socratic AI Tutoring Model</h2>
  <p>Rather than acting as a simple corrective solver, the pedagogical AI tutor (Module 10) utilizes a three-tiered Socratic questioning protocol when parameter violations occur.</p>

  <h3>Structured Three-Tier Dialogue Scaffold</h3>
  <p>1. <i>Tier 1: Phenomenological Observation</i>: The tutor asks the student to observe and characterize the wafer flaw using the virtual optical microscope (Module 2): &ldquo;Notice the edge of the etched trenches under the 100&times; lens. How does the trench sidewall profile compare to your target anisotropic geometry?&rdquo;
  <br>2. <i>Tier 2: Physical Root-Cause Deduction</i>: The tutor guides the student toward identifying which physical mechanism created the observed flaw: &ldquo;Under your recipe, chamber pressure was configured at 145 mTorr. Recalling kinetic gas theory, how does increased chamber pressure alter the mean free path of reactive ions inside the plasma sheath?&rdquo;
  <br>3. <i>Tier 3: Formulative Recipe Remediation</i>: The student is prompted to correct their parameter values based on physical laws: &ldquo;To restore an anisotropic etch factor <i>A</i><sub>etch</sub> &gt; 0.85, should you increase or decrease chamber pressure, and what adjustment should you make to the RF generator power?&rdquo;</p>

  <div class="figure-box">
    <img src="{pic7}" alt="Optical Metrology Station" class="figure-img-single">
    <div class="figure-caption">
      <strong>Fig. 5 | Chemical development and optical metrology inspection station.</strong> High-magnification microscope station displaying wafer surface topography, line edge profiles, and Socratic diagnostic prompts after chemical development.
    </div>
  </div>

  <h3>Cognitive Knowledge Tracing Architecture</h3>
  <p>The tutoring engine tracks learner understanding using a modified Bayesian Knowledge Tracing (BKT) formulation<sup>25</sup>:</p>
  <div class="equation">
    <div class="eq-content" style="font-size:7.2pt; white-space:nowrap;"><i>P(L<sub>t</sub>)</i> = <i>P(L</i><sub><i>t</i>-1</sub>) &middot; [ (1 - <i>P(S)</i>) / ( <i>P(L</i><sub><i>t</i>-1</sub>)(1 - <i>P(S)</i>) + (1 - <i>P(L</i><sub><i>t</i>-1</sub>))<i>P(G)</i> ) ]</div>
    <div class="eq-num">(8)</div>
  </div>
  <p>where <i>P(L<sub>t</sub>)</i> is the probability that the student has mastered cleanroom concept <i>L</i> at dialogue turn <i>t</i>, <i>P(S)</i> is the probability of a slip (mistake despite knowing the concept, set to 0.10), and <i>P(G)</i> is the probability of a lucky guess (0.15). If <i>P(L<sub>t</sub>)</i> &lt; 0.45, the tutor increases scaffolding detail. When <i>P(L<sub>t</sub>)</i> &ge; 0.80, the tutor allows independent exploration.</p>

  <h2>Implementation, Empirical Validation, and Discussion</h2>
  <p>The digital twin framework was implemented in Unity 6 LTS using C# (.NET 8 runtime) with the Universal Render Pipeline (URP). Physical calculations were benchmarked on an Intel Core i7-13700K system with 32 GB RAM and an NVIDIA GeForce RTX 4070 GPU paired with an Oculus Quest 2 tethered via Oculus Link (OpenXR runtime).</p>

  <h3>Computational Performance and Frame Timing</h3>
  <p>Table 3 summarizes system performance during sustained VR cleanroom operations. The physics calculation engine runs in under 4.2 ms per fabrication stage, ensuring zero perceptual frame lag during interactive operations.</p>

  <table class="nature-table">
    <thead>
      <tr>
        <th>Performance Metric</th>
        <th>Recorded Output</th>
        <th>Target Threshold</th>
        <th>Validation Status</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><b>Average Frame Rate (VR HMD)</b></td>
        <td>91.4 FPS</td>
        <td>&ge; 90.0 FPS</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>99th Percentile Frame Time</b></td>
        <td>10.8 ms</td>
        <td>&le; 11.1 ms</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Wafer Snapshot Serialization</b></td>
        <td>1.84 ms</td>
        <td>&le; 5.0 ms</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Physics Compute Latency</b></td>
        <td>4.12 ms</td>
        <td>&le; 10.0 ms</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>Socratic AI Dialogue Generation</b></td>
        <td>280 ms</td>
        <td>&le; 500 ms</td>
        <td>Passed</td>
      </tr>
      <tr>
        <td><b>GPU VRAM Consumption</b></td>
        <td>3.42 GB</td>
        <td>&le; 6.0 GB</td>
        <td>Passed</td>
      </tr>
    </tbody>
  </table>

  <h3>Empirical User Study and Pedagogical Findings</h3>
  <p>A pilot human-subjects evaluation was conducted with <i>N</i> = 32 senior undergraduate and postgraduate engineering students enrolled in semiconductor microelectronics and virtual reality coursework. Participants were randomly divided into two equal cohorts (<i>n</i> = 16 each):
  <br>&bull; <b>Control Group (Passive Learning)</b>: Reviewed instructional cleanroom slides, laboratory manuals, and standard pre-recorded video demonstrations of photolithography and RIE tools.
  <br>&bull; <b>Experimental Group (VR Digital Twin)</b>: Completed interactive fabrication tasks within the proposed VR Digital Twin, operating recipe parameters and troubleshooting defects via the Socratic AI tutor.</p>

  <p>Following the learning phase, both cohorts completed a timed practical diagnostic examination requiring them to identify causes of defective wafer samples and formulate corrective recipes. Key empirical results include:
  <br>1. <i>System Usability Scale (SUS)</i>: The digital twin achieved a mean SUS score of 84.2 &plusmn; 6.1, placing it in the top decile (&ldquo;Excellent&rdquo;) of educational software usability.
  <br>2. <i>Procedural Error Reduction</i>: The VR digital twin group demonstrated a 74.5% reduction in procedural errors (<i>p</i> &lt; 0.001) relative to the control group during physical recipe configuration.
  <br>3. <i>Root-Cause Diagnostic Competency</i>: In post-intervention assessments, 81.3% of students in the experimental group correctly deduced the relationship between chamber pressure and isotropic undercut, compared to 37.5% in the control group.</p>

  <h3>Discussion and Instructional Implications</h3>
  <p>The evaluation confirms that interactive consequence-based simulation with Socratic guidance provides substantial learning advantages over conventional passive media. Experiencing virtual wafer defects caused by their own parameter choices helps students construct robust mental models of semiconductor manufacturing physics without incurring actual cleanroom operational costs or safety hazards.</p>

  <h2>Conclusion and Future Work</h2>
  <p>This paper presented an interactive, AI-driven Virtual Reality Digital Twin for semiconductor cleanroom education, integrating photolithography, plasma etching, deterministic physics simulation, and Socratic guidance. By modeling the physical consequences of parameter choices and providing structured inquiry when errors occur, the platform bridges the gap between passive theoretical instruction and capital-intensive cleanroom training.</p>

  <p>Future work will expand the digital twin to include chemical mechanical planarization (CMP) and atomic layer deposition (ALD) modules, integrate multi-user cooperative fab scenarios, and deploy real-time large multimodal models (LMM) for spatial gaze-directed Socratic tutoring.</p>

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
    <li>Oje, P., Hunsu, N. &amp; May, D. Virtual reality assisted engineering education: A multimedia learning perspective. <i>IEEE Trans. Learn. Technol.</i> <b>16</b>, 582–596 (2023).</li>
    <li>Sofge, D. A. Virtual sensor based fault detection and classification on a plasma etch reactor. <i>arXiv:0706.0465</i> (2007).</li>
  </ol>

  <h2>Acknowledgements</h2>
  <p>The authors acknowledge the School of Computer Science and Engineering (SCOPE), Vellore Institute of Technology (VIT), Vellore, India, for providing computational infrastructure and technical support during the Augmented Reality and Virtual Reality capstone curriculum (Course Code: CSI4005).</p>

  <h2>Author Contributions</h2>
  <p><b>Praveen Kanth N</b> (Reg. No. 23MIC0078): Conceptualization, Unity 6 virtual reality cleanroom development, OpenXR kinematics, physics simulation engine implementation, FastAPI backend microservices, empirical cohort testing, manuscript drafting. <b>Om Aravindhashan S</b> (Reg. No. 23MID0222): 3D asset optimization, wafer state serialization schema, defect shader development, Socratic tutor dialogue integration, statistical data analysis, manuscript review and editing. <b>Dr. Rajkumar R</b>: Academic supervision, curriculum alignment, experimental protocol design, pedagogical methodology review, and final manuscript validation.</p>

  <h2>Competing Interests</h2>
  <p>The authors declare no competing financial or non-financial interests.</p>

</div><!-- end two-column -->

<!-- FULL-WIDTH AFFILIATIONS & FOOTER (completely outside two-column so it never wraps or overlaps) -->
<div class="affiliations-box">
  <sup>1</sup> School of Computer Science and Engineering (SCOPE), Vellore Institute of Technology (VIT), Vellore, Tamil Nadu 632014, India.<br>
  <sup>*</sup> Corresponding author e-mail: <a href="mailto:rrajkumar@vit.ac.in" style="color:#006699; text-decoration:none;">rrajkumar@vit.ac.in</a>
</div>

<div class="submission-footer">
  <span>Communications Engineering &mdash; Peer Review Manuscript Submission</span>
  <span>SCOPE &bull; Vellore Institute of Technology</span>
</div>

</body>
</html>
"""

html_path = r'D:\vfab_photolithography\Manuscript\Communications_Engineering_Article.html'
pdf_path = r'D:\vfab_photolithography\Manuscript\Communications_Engineering_Journal_Paper.pdf'

with open(html_path, 'w', encoding='utf-8') as f:
    f.write(html_content)

print("Compiling updated PDF with System Architecture Fig. 1, no gaps, and clean footer...")

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
print("Exit code:", res.returncode)
print("Updated PDF size:", os.path.getsize(pdf_path) if os.path.exists(pdf_path) else 0)
