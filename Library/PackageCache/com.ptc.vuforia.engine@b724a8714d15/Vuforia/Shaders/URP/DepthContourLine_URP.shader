Shader "Vuforia/URP/DepthContourLine"
{
    Properties 
    {
        _SilhouetteSize ("Size", Float) = 1
        _SilhouetteColor ("Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "14.0.0"
        }

        Tags { "RenderPipeline"="UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
            CBUFFER_START(UnityPerMaterial)
            uniform float _SilhouetteSize;
            uniform float4 _SilhouetteColor;
            CBUFFER_END

            struct v2f 
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct vertIn 
            {
                float3 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(vertIn input) 
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input); //Insert
                ZERO_INITIALIZE(v2f, output); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); //Insert
    
                // unmodified projected position of the vertex
                output.position = TransformObjectToHClip(input.position);
                output.color = _SilhouetteColor;

                // calculate silhouette in image space
                const float3 normal = mul((float3x3)UNITY_MATRIX_IT_MV, input.normal);
                const float3 normal_screen = TransformWViewToHClip(normal).xyz;

                float3 screen_offset = _SilhouetteSize * normalize(normal_screen);
                
                float2 xy_offset;
                xy_offset.x = screen_offset.x / (_ScreenParams.x * 0.5);
                xy_offset.y = screen_offset.y / (_ScreenParams.y * 0.5);
                // denormalize the screenspace offset, so it is correct after projective division by w
                // dividing output.position by w here would interfere with culling
                xy_offset *= output.position.w;
                
                output.position.xy += xy_offset;
                
                return output;
            }

            half4 frag(v2f input) :COLOR 
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); //Insert
                return input.color;
            }
            ENDHLSL
        }
    }
}
