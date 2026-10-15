Shader "DefenseYard/Signal"
{
    Properties {
        _MainTex("Signal texture", 2D) = "white" {}
        _NoiseTex("Scrolling distortion noise", 2D) = "gray" {}
        _Tint("Signal color", Color) = (0.2,0.9,0.75,1)
        _Phase("UV offset", Float) = 0
        _Distortion("Noise UV displacement", Float) = 0
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha One
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST, _Tint; float _Phase, _Distortion;
            CBUFFER_END
            struct V { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct F { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            F vert(V v) { F o; o.position=TransformObjectToHClip(v.vertex.xyz); o.uv=TRANSFORM_TEX(v.uv,_MainTex); return o; }
            half4 frag(F i):SV_Target {
                float2 noiseUV=i.uv*2+float2(_Phase*0.7,_Phase*0.37);
                float2 displacement=(SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,noiseUV).rg-0.5)*_Distortion;
                float2 uv=i.uv+float2(_Phase,0)+displacement;
                half4 signal=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                return half4(signal.rgb*_Tint.rgb*1.5,signal.a*_Tint.a);
            }
            ENDHLSL
        }
    }
}
