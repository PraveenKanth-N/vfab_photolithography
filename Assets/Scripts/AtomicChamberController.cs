using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtomicChamberController : MonoBehaviour
{
    [Header("Chamber Structure")]
    public Transform chamberCeiling;
    public Transform solventGroup;
    public Transform pacGroup;
    public Vector3 chamberBounds = new Vector3(5f, 5f, 5f);

    [Header("Atom Prefabs & Materials")]
    public GameObject atomPrefab;
    public Material solventMaterial; // Must support transparency (_Alpha)
    public Material pacMaterial;     // Red/Amber
    public Material acidIonMaterial;

    [Header("Tuning Parameters")]
    public int solventCount = 150;
    public int pacCount = 60;
    
    [Header("Microscopic Kinetics")]
    public float brownianJitter = 0.006f;
    public float solventDriftSpeed = 2f;
    public float dissolutionScatterSpeed = 3f;

    private List<Transform> solventAtoms = new List<Transform>();
    private List<Transform> pacAtoms = new List<Transform>();
    private List<Vector3> initialPACPositions = new List<Vector3>();
    
    public enum MicroscopePhase { Off, WetLiquid, BakedSolid, Dissolving }
    private MicroscopePhase currentPhase = MicroscopePhase.Off;
    private bool isFluidState = false;

    private void Awake() {
        ClearChamber();
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

   private void Update() {
        switch (currentPhase)
        {
            case MicroscopePhase.WetLiquid:
                // High energy, fluid exploration jitter
                ApplyBrownianMotion(solventAtoms, brownianJitter);
                ApplyBrownianMotion(pacAtoms, brownianJitter * 0.6f);
                break;

            case MicroscopePhase.BakedSolid:
                // Solid-state lattice hum (Molecules are locked but vibrating tightly)
                ApplyBrownianMotion(pacAtoms, brownianJitter * 0.15f); // 85% restriction!
                break;

            case MicroscopePhase.Dissolving:
                // Handled dynamically by the Step 4 coroutine scattering vectors
                break;
        }
    }

    /// <summary>
    /// Step 1: Spawns the highly fluid, jittering wet photoresist layer.
    /// </summary>
    public void InitializeWetMatrix()
    {
        ClearChamber();
        isFluidState = true;
        gameObject.SetActive(true);
        currentPhase = MicroscopePhase.WetLiquid;

        solventGroup.localPosition = Vector3.zero;
        pacGroup.localPosition = Vector3.zero;
        pacGroup.localScale = Vector3.one;
        chamberCeiling.localScale = Vector3.one;
        solventMaterial.SetFloat("_Alpha", 1f);

        // Spawn Solvents (Teal)
        for (int i = 0; i < solventCount; i++)
        {
            Vector3 randomPos = GetRandomLocalPosition(0.6f);
            GameObject solvent = Instantiate(atomPrefab, solventGroup);
            solvent.layer = LayerMask.NameToLayer("MicroscopeView");
            solvent.transform.localPosition = randomPos;
            solvent.transform.localScale = Vector3.one * 0.15f;
            solvent.GetComponent<MeshRenderer>().material = solventMaterial;
            solventAtoms.Add(solvent.transform);
        }

        // Spawn PACs (Red)
        for (int i = 0; i < pacCount; i++)
        {
            Vector3 randomPos = GetRandomLocalPosition(1f);
            GameObject pac = Instantiate(atomPrefab, pacGroup);
            pac.layer = LayerMask.NameToLayer("MicroscopeView");
            pac.transform.localPosition = randomPos;
            pac.transform.localScale = Vector3.one * 0.25f;
            pac.GetComponent<MeshRenderer>().material = pacMaterial;
            
            pacAtoms.Add(pac.transform);
            initialPACPositions.Add(randomPos);
        }
    }

    /// <summary>
    /// Step 2: Animates vertical matrix compaction and solvent boiling.
    /// </summary>
    public IEnumerator AnimateSoftBake(float finalThicknessRatio, float duration)
    {
        float elapsed = 0f;
        float floorY = -chamberBounds.y / 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Evaporate solvents upward
            solventMaterial.SetFloat("_Alpha", Mathf.Lerp(1f, 0f, t));
            solventGroup.Translate(Vector3.up * solventDriftSpeed * Time.deltaTime, Space.Self);

            // Compress ceiling
            float initialCeilingY = 2.5f; // Top of your 5-unit cube
            float targetCeilingY = -2.5f + (5f * finalThicknessRatio); // Maps ratio directly to remaining height

            chamberCeiling.localPosition = new Vector3(
                chamberCeiling.localPosition.x, 
                Mathf.Lerp(initialCeilingY, targetCeilingY, t), 
                chamberCeiling.localPosition.z
            );

            // Tightly pack PACs toward the floor plane (Increases density, retains object scale)
            for (int i = 0; i < pacAtoms.Count; i++) {
                if (pacAtoms[i] == null) continue;
                
                Vector3 initialPos = initialPACPositions[i];
                floorY = -chamberBounds.y / 2f; 
                
                // Math: Scale the distance from the floor by the thickness ratio
                float targetY = floorY + ((initialPos.y - floorY) * finalThicknessRatio);
                
                // Smoothly interpolate the position over time
                pacAtoms[i].localPosition = new Vector3(initialPos.x, Mathf.Lerp(initialPos.y, targetY, t), initialPos.z);
            }

            yield return null;
        }

        // Post-bake: Matrix has solidified into a film grid. Stop fluid vibrations.
        isFluidState = false;
        currentPhase = MicroscopePhase.BakedSolid;

        foreach (Transform s in solventAtoms) if (s != null) Destroy(s.gameObject);
        solventAtoms.Clear();
        
    }

    /// <summary>
    /// Step 3: Swaps target percentages into green acid ions.
    /// </summary>
    public void AnimateExposure(float conversionEfficiency, Material acidIonMaterial)
    {
        int targetConversionCount = Mathf.RoundToInt(pacAtoms.Count * conversionEfficiency);
        for (int i = 0; i < targetConversionCount; i++)
        {
            if (pacAtoms[i] != null)
            {
                pacAtoms[i].GetComponent<MeshRenderer>().material = acidIonMaterial;
            }
        }
    }

    /// <summary>
    /// Step 4: Random directional scattering and visual opacity dissolution fade.
    /// </summary>
    public IEnumerator AnimateDevelopment(float duration)
    {
        float elapsed = 0f;
        
        isFluidState = false; // Dev solution breaks the solid grid matrix completely
        currentPhase = MicroscopePhase.Dissolving;

        // Generate random dissipation velocity vectors for every remaining atom
        Vector3[] scatterDirections = new Vector3[pacAtoms.Count];
        List<Material> activeMaterials = new List<Material>();

        for (int i = 0; i < pacAtoms.Count; i++)
        {
            scatterDirections[i] = Random.onUnitSphere; // Perfect 360-degree distribution
            if (pacAtoms[i] != null)
            {
                activeMaterials.Add(pacAtoms[i].GetComponent<MeshRenderer>().material);
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. Move balls outward in their designated random directions
            for (int i = 0; i < pacAtoms.Count; i++)
            {
                if (pacAtoms[i] != null)
                {
                    pacAtoms[i].Translate(scatterDirections[i] * dissolutionScatterSpeed * Time.deltaTime, Space.Self);
                }
            }

            // 2. Smoothly fade materials away into transparency
            float rapidFade = Mathf.Pow(t, 2);
            foreach (Material mat in activeMaterials)
            {
                if (mat != null && mat.HasProperty("_Alpha"))
                {
                    mat.SetFloat("_Alpha", Mathf.Lerp(1f, 0f, rapidFade));
                }
            }

            yield return null;
        }

        ClearChamber();
    }

    private void ApplyBrownianMotion(List<Transform> atoms, float maxJitter)
    {
        foreach (Transform atom in atoms)
        {
            if (atom == null) continue;
            Vector3 jitter = new Vector3(
                Random.Range(-maxJitter, maxJitter),
                Random.Range(-maxJitter, maxJitter),
                Random.Range(-maxJitter, maxJitter)
            );
            atom.localPosition += jitter;
        }
    }

    private Vector3 GetRandomLocalPosition(float yCapping)
    {
        float xExtent = chamberBounds.x / 2f;
        float yExtent = chamberBounds.y / 2f;
        float zExtent = chamberBounds.z / 2f;

        return new Vector3(
            Random.Range(-xExtent, xExtent),
            Random.Range(-yExtent, yExtent * yCapping),
            Random.Range(-zExtent, zExtent)
        );
    }

    private void ClearChamber()
    {
        foreach (Transform child in solventGroup) Destroy(child.gameObject);
        foreach (Transform child in pacGroup) Destroy(child.gameObject);
        solventAtoms.Clear();
        pacAtoms.Clear();
        initialPACPositions.Clear();
    }

    [ContextMenu("Test Step 1: Post-Spin")]
    public void TestSpin() { InitializeWetMatrix(); }

    [ContextMenu("Test Step 2: Post-Bake")]
    public void TestBake() { StartCoroutine(AnimateSoftBake(0.55f, 2.0f)); }

    [ContextMenu("Test Step 3: Post-Exposure")]
    public void TestExposure() { AnimateExposure(0.7f, acidIonMaterial); }

    [ContextMenu("Test Step 4: Post-Develop")]
    public void TestDevelop() { StartCoroutine(AnimateDevelopment(2.0f)); }
}

