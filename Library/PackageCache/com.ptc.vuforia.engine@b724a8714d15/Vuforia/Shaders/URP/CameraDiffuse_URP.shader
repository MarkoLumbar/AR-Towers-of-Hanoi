//========================================================================
// Copyright (c) 2017 PTC Inc. All Rights Reserved.
//
// Vuforia is a trademark of PTC Inc., registered in the United States and other
// countries.
//=========================================================================

Shader "Vuforia/URP/CameraDiffuse"
{
    Properties
    {
        _MaterialColor("_BaseColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "14.0.0"
        }

        Pass
        {
            Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" "LightMode" = "UniversalForward"}
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            uniform float4 _MaterialColor;
            CBUFFER_END

            struct appdata
            {
                float3 position : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 diff : COLOR0; // diffuse lighting color
                float4 position : POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v); //Insert
                ZERO_INITIALIZE(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

                o.position = TransformObjectToHClip(v.position);

                // get vertex normal in world space
                half3 worldNormal = TransformObjectToWorldNormal(v.normal);

                float3 worldPos = mul(unity_ObjectToWorld, o.position).xyz;
                // compute world space view direction
                float3 worldViewDir = GetWorldSpaceNormalizeViewDir(worldPos);
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting "(support double-sided material)" 
                half nl = abs(dot(worldNormal, worldViewDir));

                // factor in the material color
                o.diff = lerp(_MaterialColor, nl * _MaterialColor, 0.4);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.diff;
            }
            ENDHLSL
        }
    }
}