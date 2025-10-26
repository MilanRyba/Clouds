Shader "Hidden/TestImageEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float3 viewVector : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
                // (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));

                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            Texture3D GradientTex;
            SamplerState samplerGradientTex;

            float3 _BoundsMin;
            float3 _BoundsMax;

            float _Absorption;
            float _NumSteps;

            // Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
            float2 RayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir)
            {
                float3 invRaydir = 1.0 / rayDir;

                float3 t0 = (boundsMin - rayOrigin) * invRaydir;
                float3 t1 = (boundsMax - rayOrigin) * invRaydir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
                // dstA is dst to nearest intersection, dstB dst to far intersection

                // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
                // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                // CASE 3: ray misses box (dstA > dstB)

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            float SampleDensity(float3 position)
            {
                float4 noise = GradientTex.SampleLevel(samplerGradientTex, position, 0);
                return noise.x;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(i.viewVector);

                float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float depth = LinearEyeDepth(nonLinearDepth) * length(i.viewVector);

                float2 rayBoxInfo = RayBoxDst(_BoundsMin, _BoundsMax, rayOrigin, rayDirection);
                float dstToBox = rayBoxInfo.x;
                float dstInsideBox = rayBoxInfo.y;

                float stepSize = dstInsideBox / _NumSteps;
                float dstTravelled = 0;
                float dstLimit = min(depth - dstToBox, dstInsideBox);

                float totalDensity = 0;
                while (dstTravelled < dstLimit)
                {
                    float3 rayPos = rayOrigin + rayDirection * (dstToBox + dstTravelled);
                    totalDensity += SampleDensity(rayPos) * stepSize;
                    dstTravelled += stepSize;
                }

                float transmittance = exp(-totalDensity * _Absorption);
                return col * transmittance;
            }
            ENDCG
        }
    }
}
