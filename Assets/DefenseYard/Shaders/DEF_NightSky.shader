Shader "DefenseYard/NightSky"
{
    Properties {
        _Horizon("Horizon", Color) = (0.035,0.055,0.085,1)
        _Zenith("Zenith", Color) = (0.004,0.009,0.025,1)
        _MoonDirection("Moon direction", Vector) = (0.3,0.65,0.2,0)
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass {
            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Horizon, _Zenith, _MoonDirection;
            struct V { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct F { float4 position:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO float3 direction:TEXCOORD0; };
            F vert(V v) { F o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.position=TransformObjectToHClip(v.vertex.xyz); o.direction=v.vertex.xyz; return o; }
            half4 frag(F i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 d=normalize(i.direction);
                float3 col=lerp(_Horizon.rgb,_Zenith.rgb,smoothstep(0,0.8,d.y));
                float3 cell=floor(d*650);
                float star=step(0.9994,frac(sin(dot(cell,float3(12.9898,78.233,43.719)))*43758.5453));
                col+=star*saturate(d.y)*0.32;
                float moon=dot(d,normalize(_MoonDirection.xyz));
                col+=smoothstep(0.9994,0.99965,moon)*float3(0.55,0.65,0.77);
                col+=pow(saturate(moon),500)*float3(0.018,0.028,0.05);
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
