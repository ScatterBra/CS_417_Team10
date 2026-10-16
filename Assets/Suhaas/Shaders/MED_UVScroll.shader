// Unlit transparent UV-scroll shader (URP). The scroll offset comes from a script (_UVOffset, written by MED_UVAnimator),
// not from _Time, so the speed can change smoothly without the texture jumping.
// VR: supports single-pass instanced stereo. SRP Batcher: all material properties are in UnityPerMaterial; a renderer that
// uses a MaterialPropertyBlock simply falls back to the normal draw path.
Shader "Suhaas/MED_UVScroll"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Tiling ("Tiling (xy)", Vector) = (1, 1, 0, 0)
        _UVOffset ("UV Offset (xy, set from script)", Vector) = (0, 0, 0, 0)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _EmissionBoost ("Emission Boost", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Tiling;
                float4 _UVOffset;
                half _Alpha;
                half _EmissionBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // tiling first, then the script-driven offset
                output.uv = input.uv * _Tiling.xy + _UVOffset.xy;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 rgb = tex.rgb * _Color.rgb * _EmissionBoost;
                half a = tex.a * _Color.a * _Alpha;
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
