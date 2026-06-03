Shader "Custom/CleanroomInteractableGlow"
{
    Properties
    {
        [HDR] _GlowColor ("Base Glow Color (Amber)", Color) = (1.0, 0.4, 0.0, 1.0)
        _PulseSpeed ("Pulse Speed Frequency", Range(0.1, 10.0)) = 4.0
        _HoverBrightness ("Base Brightness Factor", Range(1.0, 5.0)) = 2.5
        [HDR] _EmissionColor ("Script State Input Cache", Color) = (0.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _GlowColor;
            float _PulseSpeed;
            float _HoverBrightness;
            float4 _EmissionColor; // Map straight back onto your script cache register

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simple, solid, high-intensity color pass driven strictly by script states
                float wavePulse = sin(_Time.y * _PulseSpeed) * 0.3 + 0.7;
                
                // Read absolute brightness passing through from script color parameters
                float targetAlpha = (_EmissionColor.r + _EmissionColor.g + _EmissionColor.b);
                
                // If the script sets color to black, completely drop the pixels out
                if (targetAlpha < 0.01)
                {
                    discard;
                }

                // High-intensity additive blending projection vector
                float4 finalColor = _GlowColor * _HoverBrightness * wavePulse;
                finalColor.a = targetAlpha;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}