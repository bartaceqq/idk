Shader "Skybox/6 Sided Blend"
{
    Properties
    {
        [NoScaleOffset] _FrontTexDay ("Front (+Z) Day", 2D) = "white" {}
        [NoScaleOffset] _BackTexDay ("Back (-Z) Day", 2D) = "white" {}
        [NoScaleOffset] _LeftTexDay ("Left (-X) Day", 2D) = "white" {}
        [NoScaleOffset] _RightTexDay ("Right (+X) Day", 2D) = "white" {}
        [NoScaleOffset] _UpTexDay ("Up (+Y) Day", 2D) = "white" {}
        [NoScaleOffset] _DownTexDay ("Down (-Y) Day", 2D) = "white" {}

        [NoScaleOffset] _FrontTexNight ("Front (+Z) Night", 2D) = "white" {}
        [NoScaleOffset] _BackTexNight ("Back (-Z) Night", 2D) = "white" {}
        [NoScaleOffset] _LeftTexNight ("Left (-X) Night", 2D) = "white" {}
        [NoScaleOffset] _RightTexNight ("Right (+X) Night", 2D) = "white" {}
        [NoScaleOffset] _UpTexNight ("Up (+Y) Night", 2D) = "white" {}
        [NoScaleOffset] _DownTexNight ("Down (-Y) Night", 2D) = "white" {}

        _TintDay ("Day Tint", Color) = (0.5, 0.5, 0.5, 0.5)
        _TintNight ("Night Tint", Color) = (0.5, 0.5, 0.5, 0.5)
        [Gamma] _ExposureDay ("Day Exposure", Range(0, 8)) = 1
        [Gamma] _ExposureNight ("Night Exposure", Range(0, 8)) = 1
        [Range(0, 1)] _Blend ("Night Blend", Float) = 0
        [Range(0.5, 16)] _FaceBlend ("Face Seam Softness", Float) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FrontTexDay;
            sampler2D _BackTexDay;
            sampler2D _LeftTexDay;
            sampler2D _RightTexDay;
            sampler2D _UpTexDay;
            sampler2D _DownTexDay;

            sampler2D _FrontTexNight;
            sampler2D _BackTexNight;
            sampler2D _LeftTexNight;
            sampler2D _RightTexNight;
            sampler2D _UpTexNight;
            sampler2D _DownTexNight;

            fixed4 _TintDay;
            fixed4 _TintNight;
            half _ExposureDay;
            half _ExposureNight;
            half _Blend;
            half _FaceBlend;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed3 SampleSixSidedSky(
                float3 dir,
                sampler2D frontTex,
                sampler2D backTex,
                sampler2D leftTex,
                sampler2D rightTex,
                sampler2D upTex,
                sampler2D downTex)
            {
                float3 absDir = abs(dir) + 1e-5;

                float2 uvRight = float2(-dir.z, dir.y) / absDir.x * 0.5 + 0.5;
                float2 uvLeft = float2(dir.z, dir.y) / absDir.x * 0.5 + 0.5;
                float2 uvUp = float2(dir.x, -dir.z) / absDir.y * 0.5 + 0.5;
                float2 uvDown = float2(dir.x, dir.z) / absDir.y * 0.5 + 0.5;
                float2 uvFront = float2(dir.x, dir.y) / absDir.z * 0.5 + 0.5;
                float2 uvBack = float2(-dir.x, dir.y) / absDir.z * 0.5 + 0.5;

                fixed3 colX = (dir.x >= 0.0) ? tex2D(rightTex, uvRight).rgb : tex2D(leftTex, uvLeft).rgb;
                fixed3 colY = (dir.y >= 0.0) ? tex2D(upTex, uvUp).rgb : tex2D(downTex, uvDown).rgb;
                fixed3 colZ = (dir.z >= 0.0) ? tex2D(frontTex, uvFront).rgb : tex2D(backTex, uvBack).rgb;

                float3 weights = pow(absDir, _FaceBlend);
                weights /= max(weights.x + weights.y + weights.z, 1e-5);

                return colX * weights.x + colY * weights.y + colZ * weights.z;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                fixed3 day = SampleSixSidedSky(
                    dir,
                    _FrontTexDay, _BackTexDay, _LeftTexDay, _RightTexDay, _UpTexDay, _DownTexDay
                ).rgb * _TintDay.rgb * _ExposureDay;

                fixed3 night = SampleSixSidedSky(
                    dir,
                    _FrontTexNight, _BackTexNight, _LeftTexNight, _RightTexNight, _UpTexNight, _DownTexNight
                ).rgb * _TintNight.rgb * _ExposureNight;

                return fixed4(lerp(day, night, saturate(_Blend)), 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
