using UnityEngine;
using System;
using System.Collections.Generic;

namespace SemiconductorDigitalTwin.Pedagogy
{
    /// <summary>
    /// Module 10: Explainable Socratic AI Tutor.
    /// Orchestrates a three-tiered inquiry protocol when operational parameter deviations occur:
    /// Tier 1: Phenomenological Observation (Observation Prompting)
    /// Tier 2: Physical Root-Cause Deduction (Mechanistic Comprehension)
    /// Tier 3: Formulative Recipe Remediation (Parametric Correction)
    /// Employs Bayesian Knowledge Tracing (BKT Equation 8) to model concept mastery.
    /// </summary>
    public class SocraticAITutor : MonoBehaviour
    {
        public static SocraticAITutor Instance { get; private set; }

        [System.Serializable]
        public class SocraticInquiry
        {
            public string defectKey;
            public string conceptKey;

            [Header("Tier 1: Observation")]
            [TextArea(2, 4)] public string tier1ObservationPrompt;

            [Header("Tier 2: Physical Mechanism")]
            [TextArea(2, 4)] public string tier2MechanismPrompt;
            public string[] tier2Options;
            public int tier2CorrectIndex;
            [TextArea(1, 3)] public string tier2Explanation;

            [Header("Tier 3: Recipe Remediation")]
            [TextArea(2, 4)] public string tier3RemediationPrompt;
            public string[] tier3Options;
            public int tier3CorrectIndex;
            [TextArea(1, 3)] public string tier3Explanation;
        }

        [Header("Dialogue Registry")]
        [SerializeField] private List<SocraticInquiry> _inquiryLibrary = new List<SocraticInquiry>();

        [Header("Bayesian Knowledge Tracing (BKT) Parameters (Eq. 8)")]
        [Range(0f, 1f)] public float defaultPriorL0 = 0.35f;
        [Range(0f, 1f)] public float slipProbS = 0.10f;
        [Range(0f, 1f)] public float guessProbG = 0.20f;
        [Range(0f, 1f)] public float transitionProbT = 0.15f;

        // Tracks concept mastery: ConceptKey -> Probability P(L_t)
        private Dictionary<string, float> _knowledgeState = new Dictionary<string, float>();

        public event Action<SocraticInquiry, int> OnInquiryStarted; // inquiry, tier
        public event Action<bool, string> OnAnswerEvaluated; // correct, feedback
        public event Action<string, float> OnMasteryUpdated; // concept, new P(L)

        public SocraticInquiry ActiveInquiry { get; private set; }
        public int CurrentTier { get; private set; } = 1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeInquiryLibrary();
        }

        private void InitializeInquiryLibrary()
        {
            // 1. RIE Isotropic Undercut (Chamber pressure too high)
            _inquiryLibrary.Add(new SocraticInquiry
            {
                defectKey = "IsotropicEtch_LateralUndercut",
                conceptKey = "Plasma_Sheath_Anisotropy",
                tier1ObservationPrompt = "Notice the trench sidewalls under the microscope. The etching has extended laterally beneath the photoresist mask rather than etching straight down.",
                tier2MechanismPrompt = "Your recipe configured chamber pressure at 145 mTorr. Recalling kinetic gas theory, how did excessive pressure affect reactive ions in the plasma sheath?",
                tier2Options = new string[]
                {
                    "Decreased ion mean free path caused collisional scattering, losing directional momentum.",
                    "Higher pressure froze ion thermal velocities, stopping all chemical etching.",
                    "Excess pressure caused optical diffraction of the RF field into the silicon."
                },
                tier2CorrectIndex = 0,
                tier2Explanation = "Correct! High pressure decreases mean free path (lambda_mfp < sheath width), causing ions to scatter sideways and etch isotropically.",
                tier3RemediationPrompt = "To restore vertical anisotropy factor A_etch > 0.85, what adjustments should you make to the tool recipe?",
                tier3Options = new string[]
                {
                    "Reduce chamber pressure to 40 mTorr and maintain RF bias power to preserve directional ion bombardment.",
                    "Double the chamber pressure to 300 mTorr and reduce RF power to 20W.",
                    "Shut off SF6 gas and use 100% pure Oxygen at atmospheric pressure."
                },
                tier3CorrectIndex = 0,
                tier3Explanation = "Precisely! Lower pressure increases the ion mean free path, ensuring energetic ions bombard the trench bottom orthogonally."
            });

            // 2. Photoresist Striations (Spin speed too low)
            _inquiryLibrary.Add(new SocraticInquiry
            {
                defectKey = "RadialThicknessNonUniformity_Striations",
                conceptKey = "SpinCoating_Hydrodynamics",
                tier1ObservationPrompt = "Look at the wafer surface under yellow illumination. The photoresist exhibits visible concentric interference rings and radial striations.",
                tier2MechanismPrompt = "You selected a spin speed of 1200 RPM. According to the Emslie-Bonner-Peck formulation (Eq. 1), why did this cause radial thickness variations?",
                tier2Options = new string[]
                {
                    "Centrifugal acceleration was insufficient to overcome viscous shear drag before solvent dried.",
                    "The wafer rotated so fast that turbulent vortex shedding stripped the photoresist completely.",
                    "UV light prematurely polymerized the liquid puddle on the chuck."
                },
                tier2CorrectIndex = 0,
                tier2Explanation = "Spot on! Below 1800 RPM, centrifugal force cannot uniformly overcome viscous resistance, freezing radial striations into the film.",
                tier3RemediationPrompt = "What spin coater recipe configuration will establish a uniform 1.2 um film across the 300mm substrate?",
                tier3Options = new string[]
                {
                    "Increase spin speed to the nominal 3500 RPM with a 30-second duration and 3000 RPM/s acceleration.",
                    "Decrease speed further to 500 RPM and dispense double the resist volume.",
                    "Heat the spin chuck to 250 degrees Celsius while spinning."
                },
                tier3CorrectIndex = 0,
                tier3Explanation = "Exactly right! 3500 RPM establishes hydrodynamic equilibrium, yielding a uniform +/- 1.5% film."
            });

            // 3. RIE Mask Faceting & Micro-Trenching (RF power too high)
            _inquiryLibrary.Add(new SocraticInquiry
            {
                defectKey = "ExcessRFPower_MaskFacetingMicroTrenching",
                conceptKey = "RIE_Ion_Bombardment",
                tier1ObservationPrompt = "Under high magnification, the photoresist edges have eroded diagonally (faceting) and deep micro-trenches have gouged into the corners of the trench bottom.",
                tier2MechanismPrompt = "RF bias power was set to 350W. How did this high power affect physical sputtering kinetics at the substrate interface?",
                tier2Options = new string[]
                {
                    "Accelerated DC self-bias voltage imparted excessive kinetic energy, causing physical sputter erosion and ion reflection into corners.",
                    "High RF power created chemical polymerization that coated the substrate in Teflon.",
                    "The high power cooled the plasma, causing condensation of fluorocarbons."
                },
                tier2CorrectIndex = 0,
                tier2Explanation = "Correct! Bias voltage scales with sqrt(P_RF). Excessive voltage causes glancing-angle ion reflection off sidewalls, carving micro-trenches.",
                tier3RemediationPrompt = "How should RF power and gas ratios be reconfigured to prevent micro-trenching while preserving etch rate?",
                tier3Options = new string[]
                {
                    "Reduce RF bias power to 150W and balance SF6 chemical radical flow.",
                    "Increase RF power to 600W to melt the corner asperities.",
                    "Remove the vacuum and run the etch in open ambient air."
                },
                tier3CorrectIndex = 0,
                tier3Explanation = "Excellent. 150W delivers sufficient anisotropic activation without mask destruction or corner over-etching."
            });
        }

        /// <summary>
        /// Triggers Socratic inquiry for a detected defect.
        /// </summary>
        public void TriggerInquiryForDefect(string defectType)
        {
            SocraticInquiry match = _inquiryLibrary.Find(i => i.defectKey.Equals(defectType, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                // Fallback to first inquiry if exact defect key has no specific dialogue
                match = _inquiryLibrary[0];
            }

            ActiveInquiry = match;
            CurrentTier = 1;

            Debug.Log($"[SocraticTutor] Initiating Tier 1 inquiry for defect: {defectType}");
            OnInquiryStarted?.Invoke(ActiveInquiry, CurrentTier);
        }

        /// <summary>
        /// Submits student response to the current tier question.
        /// </summary>
        public bool SubmitAnswer(int selectedOptionIndex)
        {
            if (ActiveInquiry == null) return false;

            bool isCorrect = false;
            string feedback = "";

            if (CurrentTier == 1)
            {
                // Tier 1 is observation acknowledgment, auto-advances to Tier 2
                CurrentTier = 2;
                OnInquiryStarted?.Invoke(ActiveInquiry, CurrentTier);
                return true;
            }
            else if (CurrentTier == 2)
            {
                isCorrect = (selectedOptionIndex == ActiveInquiry.tier2CorrectIndex);
                feedback = isCorrect ? ActiveInquiry.tier2Explanation : "Not quite. Think about how pressure changes the collision frequency of gas molecules.";
                
                UpdateBKT(ActiveInquiry.conceptKey, isCorrect);
                OnAnswerEvaluated?.Invoke(isCorrect, feedback);

                if (isCorrect)
                {
                    CurrentTier = 3;
                    OnInquiryStarted?.Invoke(ActiveInquiry, CurrentTier);
                }
                return isCorrect;
            }
            else if (CurrentTier == 3)
            {
                isCorrect = (selectedOptionIndex == ActiveInquiry.tier3CorrectIndex);
                feedback = isCorrect ? ActiveInquiry.tier3Explanation : "Review the relationship between pressure, mean free path, and vertical etch rate.";

                UpdateBKT(ActiveInquiry.conceptKey, isCorrect);
                OnAnswerEvaluated?.Invoke(isCorrect, feedback);

                if (isCorrect)
                {
                    Debug.Log($"[SocraticTutor] Socratic sequence completed for {ActiveInquiry.conceptKey}! Student successfully deduced remediation.");
                }
                return isCorrect;
            }

            return false;
        }

        /// <summary>
        /// Updates Bayesian Knowledge Tracing probability P(L_t) according to Equation (8).
        /// </summary>
        private void UpdateBKT(string conceptKey, bool correct)
        {
            if (!_knowledgeState.ContainsKey(conceptKey))
            {
                _knowledgeState[conceptKey] = defaultPriorL0;
            }

            float prevL = _knowledgeState[conceptKey];
            float pL_given_obs;

            if (correct)
            {
                // P(L_t | Correct) = [ P(L_{t-1}) * (1 - S) ] / [ P(L_{t-1}) * (1 - S) + (1 - P(L_{t-1})) * G ]
                float numerator = prevL * (1f - slipProbS);
                float denominator = numerator + (1f - prevL) * guessProbG;
                pL_given_obs = numerator / Mathf.Max(0.001f, denominator);
            }
            else
            {
                // P(L_t | Incorrect) = [ P(L_{t-1}) * S ] / [ P(L_{t-1}) * S + (1 - P(L_{t-1})) * (1 - G) ]
                float numerator = prevL * slipProbS;
                float denominator = numerator + (1f - prevL) * (1f - guessProbG);
                pL_given_obs = numerator / Mathf.Max(0.001f, denominator);
            }

            // Transit to next state: P(L_t) = P(L_t | obs) + (1 - P(L_t | obs)) * T
            float newL = pL_given_obs + (1f - pL_given_obs) * transitionProbT;
            newL = Mathf.Clamp01(newL);

            _knowledgeState[conceptKey] = newL;
            Debug.Log($"[SocraticTutor-BKT] Concept '{conceptKey}' updated: P(L) = {newL:F3} (Correct: {correct})");
            OnMasteryUpdated?.Invoke(conceptKey, newL);
        }

        public float GetConceptMastery(string conceptKey)
        {
            return _knowledgeState.TryGetValue(conceptKey, out float val) ? val : defaultPriorL0;
        }
    }
}
