Shader "Custom/CleanroomFloorPerforatedURP"
{
    Properties
    {
        _TileColor ("Panel Base Color (Neutral)", Color) = (0.7, 0.7, 0.7, 1.0) 
        _BorderColor ("Panel Border Color", Color) = (0.3, 0.3, 0.3, 1.0)
        _HoleColor ("Perforation Deep Cavity Color", Color) = (0.05, 0.05, 0.05, 1.0)
        
        [Header(Point Light Ambient Safety)]
        _FloorAmbientBoost ("Floor Base Visibility Boost", Range(0.0, 1.0)) = 0.15
        
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
            
            // Force Unity to enable additional local point lights for this shader mesh
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
                float3 viewDirWS    : TEXCOORD2; 
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TileColor;
                half4 _BorderColor;
                half4 _HoleColor;
                float _FloorAmbientBoost;
                float _PanelGridScale;
                float _PanelBorderThickness;
                float _PerforationDensity;
                float _Seed;
                float _HoleFrequency;
                float _HoleRadius;
                float _SmoothEdge;
                float _HoleDepth;
            CBUFFER_END

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
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. PANEL COORDINATES
                float2 panelUV = input.positionWS.xz * _PanelGridScale;
                float2 panelFrac = frac(panelUV);
                float2 panelIndex = floor(panelUV);

                float2 borderCheck = smoothstep(0.0, _PanelBorderThickness, panelFrac) * smoothstep(1.0, 1.0 - _PanelBorderThickness, panelFrac);
                float isInsidePanelBody = borderCheck.x * borderCheck.y;

                // 2. RANDOMIZATION
                float randomValue = GenerateRandomHash(panelIndex, _Seed);
                float isThisPanelPerforated = step(randomValue, _PerforationDensity);

                // 3. PARALLAX SHIFTING
                float3 viewDir = normalize(input.viewDirWS);
                float2 parallaxOffset = (viewDir.xz / (viewDir.y + 0.001)) * _HoleDepth;
                float2 shiftedPanelFrac = panelFrac - (parallaxOffset * isThisPanelPerforated);

                // 4. PERFORATION HOLES
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

                // 5. COLOR ALBEDO GENERATION
                half3 albedoColor = _TileColor.rgb;
                albedoColor = lerp(albedoColor, _HoleColor.rgb, finalHoleMask);
                albedoColor = lerp(_BorderColor.rgb, albedoColor, isInsidePanelBody);

                float3 normalDirection = normalize(input.normalWS);

                // --- 6. ACCUMULATE POINT LIGHT DATA ---
                // We add our new manual property slider here to make sure the floor is never pitch black
                half3 dynamicLightingPass = half3(_FloorAmbientBoost, _FloorAmbientBoost, _FloorAmbientBoost); 

                // Process every single point light affecting this area
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float3 lightDirection = normalize(light.direction);
                    
                    // Simple diffuse check for flat surfaces
                    float diffuseIntensity = saturate(dot(normalDirection, lightDirection));
                    float totalIntensity = diffuseIntensity * light.distanceAttenuation * light.shadowAttenuation;
                    
                    dynamicLightingPass += light.color * totalIntensity;
                }

                // 7. FINAL LIGHT MULTIPLICATION
                half3 finalLitColor = albedoColor * dynamicLightingPass;

                return half4(finalLitColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}