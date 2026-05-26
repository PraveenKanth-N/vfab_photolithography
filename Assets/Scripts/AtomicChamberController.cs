using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtomicChamberController : MonoBehaviour
{
    public enum MicroscopePhase { Off, WetLiquid, BakedSolid, Dissolving }

    [Header("Chamber Structure")]
    [Tooltip("Assign your new flat Film_Surface_Interface Quad/Plane object here.")]
    public Transform filmSurfaceInterface;
    public Transform solventGroup;
    public Transform pacGroup;
    public Vector3 chamberBounds = new Vector3(5f, 5f, 5f);

    [Header("Atom Prefabs & Materials")]
    public GameObject atomPrefab;
    public Material solventMaterial;      // Teal
    public Material pacMaterial;          // Red/Amber
    public Material acidIonMaterial;      // Neon Lime Green

    [Header("Tuning Parameters")]
    public int solventCount = 380;        // Initial massive puddle crowd volume
    public int pacCount = 110;
    
    [Header("Microscopic Kinetics")]
    public float brownianJitter = 0.05f;
    public float solventEvaporationSpeed = 2.5f;

    private List<Transform> solventAtoms = new List<Transform>();
    private List<Transform> pacAtoms = new List<Transform>();
    private List<Vector3> initialPACPositions = new List<Vector3>();
    
    private MicroscopePhase currentPhase = MicroscopePhase.Off;

    private void Awake()
    {
        // Clean out any editor-mode leftovers so we start fresh and dynamic
        ClearChamber();
    }

    private void Update()
    {
        // Continuous, frame-rate independent Brownian Motion Jitter
        switch (currentPhase)
        {
            case MicroscopePhase.WetLiquid:
                ApplyBrownianMotion(solventAtoms, brownianJitter);
                ApplyBrownianMotion(pacAtoms, brownianJitter * 0.6f);
                break;

            case MicroscopePhase.BakedSolid:
                ApplyBrownianMotion(pacAtoms, brownianJitter * 0.12f); // Tight lattice hum
                break;
        }
    }

    /// <summary>
    /// Step 1: Spawns the highly dense, chaotic initial wet puddle matrix.
    /// </summary>
    public void InitializeWetMatrix()
    {
        ClearChamber();
        currentPhase = MicroscopePhase.WetLiquid;

        solventGroup.localPosition = Vector3.zero;
        pacGroup.localPosition = Vector3.zero;
        
        // Reset surface plane to top boundary ceiling
        if (filmSurfaceInterface != null)
        {
            filmSurfaceInterface.gameObject.SetActive(true);
            filmSurfaceInterface.localPosition = new Vector3(0f, chamberBounds.y / 2f, 0f); // Y = 2.5f
        }

        // Safety reset for transparency on base material asset
        if (solventMaterial != null) solventMaterial.SetFloat("_Alpha", 1f);

        // Capture the active camera's current layer so spawned atoms are guaranteed visible
        int renderingLayer = gameObject.layer;

        // Spawn Solvents (Teal)
        for (int i = 0; i < solventCount; i++)
        {
            Vector3 randomPos = GetRandomLocalPosition(0.65f);
            GameObject solvent = Instantiate(atomPrefab, solventGroup);
            
            // CRITICAL RENDER FIX: Force clone to match the controller's exact layer mask
            solvent.layer = renderingLayer;
            
            solvent.transform.localPosition = randomPos;
            solvent.transform.localScale = Vector3.one * 0.14f;
            solvent.GetComponent<MeshRenderer>().material = solventMaterial;
            solventAtoms.Add(solvent.transform);
        }

        // Spawn PACs (Red)
        for (int i = 0; i < pacCount; i++)
        {
            Vector3 randomPos = GetRandomLocalPosition(0.65f);
            GameObject pac = Instantiate(atomPrefab, pacGroup);
            
            // CRITICAL RENDER FIX: Force clone to match the controller's exact layer mask
            pac.layer = renderingLayer;
            
            pac.transform.localPosition = randomPos;
            pac.transform.localScale = Vector3.one * 0.22f;
            pac.GetComponent<MeshRenderer>().material = pacMaterial;
            
            pacAtoms.Add(pac.transform);
            initialPACPositions.Add(randomPos);
        }
    }

    /// <summary>
    /// Step 1.5: Simulates centrifugal fluid ejection during high RPM spin.
    /// </summary>
    public IEnumerator AnimateCentrifugalThinning(float duration)
    {
        float elapsed = 0f;
        int targetSolventCount = 60; 
        int targetPACCount = 45;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (solventAtoms.Count > targetSolventCount && Random.value > 0.6f)
            {
                int removeCount = Mathf.Min(4, solventAtoms.Count - targetSolventCount);
                for (int i = 0; i < removeCount; i++)
                {
                    int index = Random.Range(0, solventAtoms.Count);
                    if (solventAtoms[index] != null) Destroy(solventAtoms[index].gameObject);
                    solventAtoms.RemoveAt(index);
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// Step 2: Animates vertical matrix compaction and solvent outgassing.
    /// </summary>
    public IEnumerator AnimateSoftBake(float finalThicknessRatio, float duration)
    {
        float elapsed = 0f;
        float floorY = -chamberBounds.y / 2f;      // -2.5f
        float initialCeilingY = chamberBounds.y / 2f; // 2.5f
        float targetCeilingY = floorY + (chamberBounds.y * finalThicknessRatio);

        // Cache runtime materials to prevent modifying project source assets
        List<Material> solventInstances = new List<Material>();
        foreach (Transform s in solventAtoms)
        {
            if (s != null) solventInstances.Add(s.GetComponent<MeshRenderer>().material);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. Opcacity fade directly on runtime clones
            float currentAlpha = Mathf.Lerp(1f, 0f, t);
            foreach (Material mat in solventInstances)
            {
                if (mat != null && mat.HasProperty("_Alpha"))
                {
                    mat.SetFloat("_Alpha", currentAlpha);
                }
            }

            solventGroup.Translate(Vector3.up * solventEvaporationSpeed * Time.deltaTime, Space.Self);

            // 2. Slide boundary plane position
            if (filmSurfaceInterface != null)
            {
                filmSurfaceInterface.localPosition = new Vector3(
                    filmSurfaceInterface.localPosition.x,
                    Mathf.Lerp(initialCeilingY, targetCeilingY, t),
                    filmSurfaceInterface.localPosition.z
                );
            }

            // 3. Compact PAC structures
            for (int i = 0; i < pacAtoms.Count; i++)
            {
                if (pacAtoms[i] == null) continue;
                Vector3 initialPos = initialPACPositions[i];
                float targetY = floorY + ((initialPos.y - floorY) * finalThicknessRatio);
                
                pacAtoms[i].localPosition = new Vector3(initialPos.x, Mathf.Lerp(initialPos.y, targetY, t), initialPos.z);
            }

            yield return null;
        }

        currentPhase = MicroscopePhase.BakedSolid;

        foreach (Transform s in solventAtoms) if (s != null) Destroy(s.gameObject);
        solventAtoms.Clear();
    }

    /// <summary>
    /// Step 3: Progressive chemical transformation wave fade-in.
    /// </summary>
    public void AnimateExposure(float conversionEfficiency, Material targetAcidIonMaterial)
    {
        if (targetAcidIonMaterial == null) targetAcidIonMaterial = acidIonMaterial;
        StartCoroutine(ChemicalTransformationRoutine(conversionEfficiency, targetAcidIonMaterial));
    }

    private IEnumerator ChemicalTransformationRoutine(float conversionEfficiency, Material targetMat)
    {
        int targetConversionCount = Mathf.RoundToInt(pacAtoms.Count * conversionEfficiency);
        float elapsed = 0f;
        float fadeDuration = 1.8f; 

        List<MeshRenderer> targets = new List<MeshRenderer>();
        for (int i = 0; i < targetConversionCount; i++)
        {
            if (pacAtoms[i] != null) targets.Add(pacAtoms[i].GetComponent<MeshRenderer>());
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            foreach (var renderer in targets)
            {
                if (renderer != null)
                {
                    renderer.material.Lerp(renderer.material, targetMat, t);
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// Step 4: Random scattering dissolution.
    /// </summary>
    public IEnumerator AnimateDevelopment(float duration)
    {
        float elapsed = 0f;
        currentPhase = MicroscopePhase.Off; 

        Vector3[] scatterDirections = new Vector3[pacAtoms.Count];
        List<Material> activeMaterials = new List<Material>();

        for (int i = 0; i < pacAtoms.Count; i++)
        {
            scatterDirections[i] = Random.onUnitSphere;
            if (pacAtoms[i] != null) activeMaterials.Add(pacAtoms[i].GetComponent<MeshRenderer>().material);
        }

        if (filmSurfaceInterface != null) filmSurfaceInterface.gameObject.SetActive(false);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < pacAtoms.Count; i++)
            {
                if (pacAtoms[i] != null)
                {
                    pacAtoms[i].Translate(scatterDirections[i] * 1.2f * Time.deltaTime, Space.Self);
                }
            }

            float rapidExponentialFade = Mathf.Pow(t, 2.5f);
            foreach (Material mat in activeMaterials)
            {
                if (mat != null && mat.HasProperty("_Alpha"))
                {
                    mat.SetFloat("_Alpha", Mathf.Lerp(1f, 0f, rapidExponentialFade));
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
}