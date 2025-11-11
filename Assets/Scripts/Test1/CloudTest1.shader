Shader "Hidden/CloudTest1.shader"
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

            Texture3D Cloud3DNoiseTextureShape;
            SamplerState samplerCloud3DNoiseTextureShape;

            float3 _BoundsMin;
            float3 _BoundsMax;

            float _Absorption;
            float _NumSteps;

            float sdSphere(float3 p, float radius) {
                return length(p) - radius;
            }
            
            float scene(float3 p) {
              float distance = sdSphere(p, 1.0);
              return -distance;
            }
            
            const float MARCH_SIZE = 0.08;
            
            float4 raymarch(float3 rayOrigin, float3 rayDirection) {
              float depth = 0.0;
              float3 p = rayOrigin + depth * rayDirection;
              
              float4 res = 0;
            
              for (int i = 0; i < 100; i++) {
                float density = scene(p);
            
                // We only draw the density if it's greater than 0
                if (density > 0.0) {
                  float4 color = float4(lerp(float3(1.0,1.0,1.0), float3(0.0, 0.0, 0.0), density), density );
                  color.rgb *= color.a;
                  res += color*(1.0-res.a);
                }
            
                depth += MARCH_SIZE;
                p = rayOrigin + depth * rayDirection;
              }
            
              return res;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(i.viewVector);

                float3 color = 0;
                float4 res = raymarch(ro, rd);
                color = res.rgb;

                // Output to screen
                return float4(color,1.0);

                // float4 accumulatedColor = 0.0;
                // 
                // float currentDepth = 0.0;
                // float3 samplePos = rayOrigin + rayDirection * currentDepth;
                // for (int i = 0; i < _NumSteps * 10.0; i++)
                // {
                //     float density = distance(samplePos, float3(0, 0, 0)) - 1.0;
                //     density = -density;
                // 
                //     if (density > 0.0)
                //     {
                //         float4 sampledColor = lerp(float4(1, 1, 1, 1), float4(0, 0, 0, 0), density);
                //         sampledColor.rgb *= density;
                //         accumulatedColor += sampledColor * (1.0 - accumulatedColor.a);
                //     }
                // 
                //     currentDepth += 0.005;
                //     samplePos = rayOrigin + rayDirection * currentDepth;
                // }
                // 
                // return float4(accumulatedColor.rgb, 1);
            }
            ENDCG
        }
    }
}
