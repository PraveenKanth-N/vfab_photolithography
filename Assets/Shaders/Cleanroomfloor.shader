Shader "Custom/CleanroomFloorPerforatedURP"
{
    Properties
    {
        _TileColor ("Panel Base Color (Amber Tint)", Color) = (0.45, 0.32, 0.12, 1.0) 
        _BorderColor ("Panel Border Color", Color) = (0.12, 0.08, 0.02, 1.0)
        _HoleColor ("Perforation Deep Cavity Color", Color) = (0.02, 0.01, 0.0, 1.0)
        
        [Header(Panel Tiling Settings)]
        _PanelGridScale ("Tile Density (Size Modifier)", Float) = 1.5
        _PanelBorderThickness ("Large Panel Border Thickness", Range(0.001, 0.1)) = 0.02
        
        [Header(1. Randomization Settings)]
        _PerforationDensity ("Random Hole Density (%)", Range(0.0, 1.0)) = 0.45
        _Seed ("Randomization Seed Offset", Float) = 1337.0
        
        [Header(Perforation Hole Settings)]
        _HoleFrequency ("Perforations Per Panel Grid", Float) = 12.0
        _HoleRadius ("Perforation Radius Size", Range(0.05, 0.45)) = 0.22
        _SmoothEdge ("Perforation Edge Anti Aliasing", Range(0.001, 0.05)) = 0.01

        [Header(3. Holographic Parallax Depth Settings)]
        _HoleDepth ("Internal Cavity Depth Offset", Range(0.0, 0.25)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2; // View direction vector for calculating parallax depth angles
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TileColor;
                half4 _BorderColor;
                half4 _HoleColor;
                float _PanelGridScale;
                float _PanelBorderThickness;
                float _PerforationDensity;
                float _Seed;
                float _HoleFrequency;
                float _HoleRadius;
                float _SmoothEdge;
                float _HoleDepth;
            CBUFFER_END

            // --- PSEUDO RANDOM NOISE HASH FUNCTION ---
            // Generates a predictable, non-shifting random decimal between 0.0 and 1.0 based on tile coordinates
            float GenerateRandomHash(float2 coord, float seed)
            {
                return frac(sin(dot(coord + seed, float2(12.9898, 78.233))) * 43758.5453123);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Track where the player's eyes are looking relative to the floor surface plane
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. CALCULATE LARGE PANEL COORDINATES
                float2 panelUV = input.positionWS.xz * _PanelGridScale;
                float2 panelFrac = frac(panelUV);
                float2 panelIndex = floor(panelUV);

                // Draw panel borders
                float2 borderCheck = smoothstep(0.0, _PanelBorderThickness, panelFrac) * smoothstep(1.0, 1.0 - _PanelBorderThickness, panelFrac);
                float isInsidePanelBody = borderCheck.x * borderCheck.y;

                // 2. PARSE THE RANDOMIZATION CHECKPOINT MATRIX
                // Generate a unique random number for this specific floor panel
                float randomValue = GenerateRandomHash(panelIndex, _Seed);
                // If the panel's unique hash number is lower than our density target slider, it gets holes!
                float isThisPanelPerforated = step(randomValue, _PerforationDensity);

                // 3. APPLY 3D PARALLAX DEPTH COORDINATE SHIFTING
                // Normalize the camera angle look vector
                float3 viewDir = normalize(input.viewDirWS);
                
                // Shift the internal hole coordinate calculations along the camera view angle vector to create depth definition
                float2 parallaxOffset = (viewDir.xz / (viewDir.y + 0.001)) * _HoleDepth;
                
                // Shift coordinates dynamically based on the player's viewing angle
                float2 shiftedPanelFrac = panelFrac - (parallaxOffset * isThisPanelPerforated);

                // 4. SELF-CONTAINED PERFORATION HOLES MATRIX (Using shifted Parallax coordinates)
                float2 holeGrid = shiftedPanelFrac * _HoleFrequency;
                float2 holeFrac = frac(holeGrid);
                float2 holeIndex = floor(holeGrid); 

                float2 localCenter = holeFrac - 0.5;
                float distanceToHoleCenter = length(localCenter);
                float rawHoleMask = 1.0 - smoothstep(_HoleRadius - _SmoothEdge, _HoleRadius + _SmoothEdge, distanceToHoleCenter);

                float isSafeFromLeftEdge   = step(1.0, holeIndex.x);
                float isSafeFromRightEdge  = step(holeIndex.x, _HoleFrequency - 2.0);
                float isSafeFromBottomEdge = step(1.0, holeIndex.y);
                float isSafeFromTopEdge    = step(holeIndex.y, _HoleFrequency - 2.0);

                float selfContainedMarginMask = isSafeFromLeftEdge * isSafeFromRightEdge * isSafeFromBottomEdge * isSafeFromTopEdge;
                float finalHoleMask = rawHoleMask * selfContainedMarginMask * isThisPanelPerforated;

                // 5. COLOR SCHEME MATRIX COMBINATION
                half3 albedoColor = _TileColor.rgb;
                albedoColor = lerp(albedoColor, _HoleColor.rgb, finalHoleMask);
                albedoColor = lerp(_BorderColor.rgb, albedoColor, isInsidePanelBody);

                // 6. ACCUMULATE LOCAL SPOTLIGHT RADIANCE DATA
                float3 normalDirection = normalize(input.normalWS);
                half3 dynamicLightingPass = half3(0.02, 0.02, 0.02); 

                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float3 lightDirection = normalize(light.direction);
                    float diffuseIntensity = saturate(dot(normalDirection, lightDirection));
                    float totalIntensity = diffuseIntensity * light.distanceAttenuation;
                    
                    dynamicLightingPass += light.color * totalIntensity;
                }

                // 7. MULTIPLY LIGHT PASS AND INJECT EMISSION TO PREVENT MUDDY COLORS
                half3 litBaseColor = albedoColor * dynamicLightingPass;
                
                // FIX: Add self-illumination (Emission) directly to the base color
                // This forces the amber tint to hold its tone cleanly in dark rooms
                half3 selfIlluminatingEmission = albedoColor * 0.15; 
                half3 finalLitColor = litBaseColor + selfIlluminatingEmission;

                return half4(finalLitColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}