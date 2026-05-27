using UnityEngine;

public class FluidInteractionController : MonoBehaviour
{
    private Material fluidMaterial;
    private Vector3 lastPosition;

    [Header("Base Wave Configuration")]
    public float targetAmplitude = 0.17f; 
    public float waveSpeed = 3.0f;

    [Header("Interaction 1: Shaking & Swerving")]
    public float shakeSensitivity = 0.05f;
    public float maxShakeEnergy = 5.0f;
    [Tooltip("How fast the wave direction transitions. Lower values prevent teleportation and add wave weight.")]
    public float directionalSmoothSpeed = 4.5f;

    [Header("Interaction 2: Wafer Displacement")]
    public float waferSpikeAmount = 2.0f;

    [Header("Energy Decay Rate")]
    public float decayRate = 1.8f;

    [Header("Beaker Physical Vibrations")]
    public Transform beakerParentTransform;
    public float beakerShakeRange = 0.015f;
    public float beakerTiltRange = 2.0f;

    [Header("Optics Configuration")]
    [Range(0f, 1f)] public float glossiness = 0.92f;
    [Range(0f, 5f)] public float specularIntensity = 2.5f;
    [Range(0f, 0.2f)] public float refractionIndex = 0.04f;
    [Range(1f, 5f)] public float fresnelPower = 3.0f;
    public Color liquidColor = new Color(0.0f, 0.5f, 0.8f, 0.3f);

    private float timer;
    private float currentEnergy = 1.0f;
    
    // Internal vector targets for directional interpolation
    private float spikeDirX = 1.0f;
    private float spikeDirZ = 0.0f;
    private float targetDirX = 1.0f;
    private float targetDirZ = 0.0f;

    private bool isSwerving = false;
    private float swerveDurationTimer = 0f;
    private float swerveTotalTime = 0f;
    
    private Vector3 originalBeakerLocalPos;
    private Quaternion originalBeakerLocalRot;

    [HideInInspector] public float uiSwerveDuration = 30.0f; 

    void Start()
    {
        FetchMaterial();
        ApplyStaticOptics();
        lastPosition = transform.position;

        if (beakerParentTransform != null)
        {
            originalBeakerLocalPos = beakerParentTransform.localPosition;
            originalBeakerLocalRot = beakerParentTransform.localRotation;
        }

        UnityEngine.UI.Slider sceneSlider = Object.FindAnyObjectByType<UnityEngine.UI.Slider>();
        if (sceneSlider != null)
        {
            sceneSlider.minValue = 15f;
            sceneSlider.maxValue = 40f;
            sceneSlider.wholeNumbers = true;
            
            uiSwerveDuration = 30f;
            sceneSlider.value = uiSwerveDuration; 
        }
    }

    void Update()
    {
        if (fluidMaterial == null) FetchMaterial();

        float dt = Mathf.Max(Time.deltaTime, 0.001f);
        timer += dt * waveSpeed;

        // --- PART 1: TRACK PHYSICAL VELOCITY (ALWAYS RUNNING) ---
        Vector3 currentPosition = transform.position;
        Vector3 displacement = currentPosition - lastPosition;
        float frameVelocity = (displacement / dt).magnitude;
        
        // CRUCIAL: Always update this every single frame so it never caches a stale position!
        lastPosition = currentPosition; 

        // --- PART 2: CONTROL THE STATE MACHINE ---
        if (isSwerving)
        {
            RunAutomatedSwerveCycle(dt);
            
            // Apply continuous frame-rate independent tracking to our direction vectors
            spikeDirX = Mathf.Lerp(spikeDirX, targetDirX, dt * directionalSmoothSpeed);
            spikeDirZ = Mathf.Lerp(spikeDirZ, targetDirZ, dt * directionalSmoothSpeed);
            
            if (beakerParentTransform != null)
            {
                float displacementX = Mathf.Sin(timer * 5.0f) * beakerShakeRange;
                float displacementZ = Mathf.Cos(timer * 4.5f) * beakerShakeRange;
                beakerParentTransform.localPosition = originalBeakerLocalPos + new Vector3(displacementX, 0f, displacementZ);

                float tiltAngleX = spikeDirZ * beakerTiltRange;
                float tiltAngleZ = -spikeDirX * beakerTiltRange;
                beakerParentTransform.localRotation = originalBeakerLocalRot * Quaternion.Euler(tiltAngleX, 0f, tiltAngleZ);
            }
        }
        else
        {
            // Smoothly return the beaker container chassis to rest
            if (beakerParentTransform != null)
            {
                beakerParentTransform.localPosition = Vector3.Lerp(beakerParentTransform.localPosition, originalBeakerLocalPos, dt * 4.0f);
                beakerParentTransform.localRotation = Quaternion.Lerp(beakerParentTransform.localRotation, originalBeakerLocalRot, dt * 4.0f);
            }

            // Standard manual player shaking tracking
            if (frameVelocity > 0.01f)
            {
                Vector3 moveDir = displacement.normalized;
                targetDirX = moveDir.x * -1f;
                targetDirZ = moveDir.z * -1f;
                
                spikeDirX = Mathf.Lerp(spikeDirX, targetDirX, dt * directionalSmoothSpeed);
                spikeDirZ = Mathf.Lerp(spikeDirZ, targetDirZ, dt * directionalSmoothSpeed);
                
                currentEnergy += frameVelocity * shakeSensitivity;
            }
        }

        // --- PART 3: ENERGY DECAY LOOP ---
        if (currentEnergy > 1.0f)
        {
            currentEnergy -= decayRate * dt;
        }
        else
        {
            currentEnergy = 1.0f;
            if (!isSwerving)
            {
                spikeDirX = Mathf.Lerp(spikeDirX, 1.0f, dt * 0.5f);
                spikeDirZ = Mathf.Lerp(spikeDirZ, Mathf.Cos(timer), dt * 0.5f);
            }
        }
        currentEnergy = Mathf.Min(currentEnergy, maxShakeEnergy);

        // --- PART 4: INJECT SHADER MANIPULATIONS ---
        float baseOscillation = Mathf.Sin(timer) * targetAmplitude;
        float finalWobbleX = baseOscillation * currentEnergy * spikeDirX;
        float finalWobbleZ = baseOscillation * currentEnergy * (currentEnergy > 1.05f ? spikeDirZ : Mathf.Cos(timer));

        if (fluidMaterial != null)
        {
            fluidMaterial.SetFloat("_WobbleX", finalWobbleX);
            fluidMaterial.SetFloat("_WobbleZ", finalWobbleZ);
        }
    }

    private void RunAutomatedSwerveCycle(float dt)
    {
        swerveDurationTimer += dt;
        
        // STRCIT TIMELINE ENFORCEMENT
        if (swerveDurationTimer >= swerveTotalTime)
        {
            isSwerving = false;
            swerveDurationTimer = 0f;
            Debug.Log($"[TIMING COMPLETE] Swerve sequence naturally exited at exactly: {swerveTotalTime}s");
            return;
        }

        // Keep feeding the procedural directional wave changes
        targetDirX = Mathf.Sin(timer * 2.5f);
        targetDirZ = Mathf.Cos(timer * 1.8f);
        
        // Force the physical energy to stick to maximum boundaries for the duration of the push sequence
        currentEnergy = maxShakeEnergy;
    }

    public void StartSwerveSequence(float totalDuration)
    {
        isSwerving = true;
        swerveDurationTimer = 0f;
        swerveTotalTime = totalDuration;
        currentEnergy = maxShakeEnergy;
    }

    public void SetSwerveDurationFromSlider(float value) { uiSwerveDuration = value; }
    public void ExecuteUISwerve() { StartSwerveSequence(uiSwerveDuration); }
    
    public void TriggerWaferInteractionSpike() 
    { 
        currentEnergy += waferSpikeAmount; 
        timer = 0f; 
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        targetDirX = Mathf.Cos(randomAngle);
        targetDirZ = Mathf.Sin(randomAngle);
    }

    private void ApplyStaticOptics() {
        if (fluidMaterial != null) { 
            fluidMaterial.SetFloat("_Glossiness", glossiness);
            fluidMaterial.SetFloat("_SpecularIntensity", specularIntensity);
            fluidMaterial.SetFloat("_RefractionIndex", refractionIndex);
            fluidMaterial.SetFloat("_FresnelPower", fresnelPower);
            fluidMaterial.SetColor("_BaseColor", liquidColor);
        } 
    }
    private void FetchMaterial() { 
        Renderer r = GetComponent<Renderer>();
        if (r != null) fluidMaterial = Application.isPlaying ? r.material : r.sharedMaterial; 
    }
    private void OnValidate() { 
        FetchMaterial(); 
        ApplyStaticOptics(); 
    }
}