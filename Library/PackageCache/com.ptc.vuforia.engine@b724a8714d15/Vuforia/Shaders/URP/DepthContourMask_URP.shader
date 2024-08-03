Shader "Vuforia/URP/DepthContourMask"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue" = "Geometry-1" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            Blend Zero One
        }
    }
}
