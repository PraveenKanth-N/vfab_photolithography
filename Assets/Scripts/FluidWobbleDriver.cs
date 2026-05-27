using UnityEngine;

public class FluidDirectToggle : MonoBehaviour
{
    private Material fluidMaterial;

    [Header("Direct Driver Variables")]
    [Tooltip("The exact value that will be pushed straight into the shader properties.")]
    public float targetAmplitude = 0.16f;
    
    [Tooltip("How fast the liquid switches directions.")]
    public float speed = 2.0f;

    private float timer;

    void Start()
    {
        FetchMaterial();
    }

    void Update()
    {
        if (fluidMaterial == null) FetchMaterial();

        // 1. Run a clean time counter
        timer += Time.deltaTime * speed;

        // 2. Use simple trigonometry to oscillate smoothly between exactly -1.0 and +1.0
        float smoothWave = Mathf.Sin(timer);

        // 3. Directly multiply it by your target amplitude variable. 
        // This guarantees the numbers hitting the shader stay exactly in your 0.002 sweet spot!
        float finalWobble = smoothWave * targetAmplitude;

        // 4. Inject them directly into the material properties
        if (fluidMaterial != null)
        {
            fluidMaterial.SetFloat("_WobbleX", finalWobble);
            
            // To make it change directions instead of moving in a rigid straight line, 
            // we pass a cosine wave to the Z axis so they naturally circle out of phase
            float finalWobbleZ = Mathf.Cos(timer) * targetAmplitude;
            fluidMaterial.SetFloat("_WobbleZ", finalWobbleZ);
        }
    }

    private void FetchMaterial() {
        Renderer r = GetComponent<Renderer>();

        if (r != null)
        {
            if (Application.isPlaying)
            {
                fluidMaterial = r.material;
            }
            else
            {
                fluidMaterial = r.sharedMaterial;
            }
        }
    }

    private void OnValidate()
    {
        FetchMaterial();
        if (fluidMaterial != null)
        {
            fluidMaterial.SetFloat("_WobbleX", targetAmplitude);
            fluidMaterial.SetFloat("_WobbleZ", targetAmplitude);
        }
    }
}