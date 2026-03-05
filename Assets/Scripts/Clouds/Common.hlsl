#pragma once

// Constants
static const float PI = 3.1415;

float InverseLerp(float value, float minValue, float maxValue)
{
    return (value - minValue) / (maxValue - minValue);
}

float2 InverseLerp(float2 value, float2 minValue, float2 maxValue)
{
    return (value - minValue) / (maxValue - minValue);
}

// Maps a value from one range to another
float Remap(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
{
    return newMin + (((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

float2 TexelToUV(uint2 inTexel, float2 inTexelSize)
{
    return ((float2) inTexel + 0.5f) * inTexelSize;
}

float3 TexelToUV(uint3 texel, float3 texelSize)
{
    return ((float3) texel + 0.5f) * texelSize;
}

uint Flatten3D(uint3 coord, uint2 dimensionsXY)
{
    return coord.x + coord.y * dimensionsXY.x + coord.z * dimensionsXY.x * dimensionsXY.y;
}

float4 MaskChannels(float4 inValue, float4 inChannelMask)
{
    if (inChannelMask.r == 1)
        return float4(inValue.r, inValue.r, inValue.r, 1.0);
    else if (inChannelMask.g == 1)
        return float4(inValue.g, inValue.g, inValue.g, 1.0);
    else if (inChannelMask.b == 1)
        return float4(inValue.b, inValue.b, inValue.b, 1.0);
    else if (inChannelMask.a == 1)
        return float4(inValue.a, inValue.a, inValue.a, 1.0);
    
    return inValue;
}

struct Ray
{
    float3 mOrigin;
    float3 mDirection;
};

Ray GetCameraRay(float2 inUV, float3 inCameraPosition, float4x4 inProjInv, float4x4 inViewInv)
{
    Ray ray;
    ray.mOrigin = inCameraPosition;
    ray.mDirection = mul(inProjInv, float4(inUV * 2.0 - 1.0, 0.0f, 1.0f)).xyz;
    ray.mDirection = mul(inViewInv, float4(ray.mDirection, 0.0f)).xyz;
    ray.mDirection = normalize(ray.mDirection);
    return ray;
}

//
// Collisions
//

// Returns distance to first and second box intersection
bool RayBoxIntersect(float3 boundsMin, float3 boundsMax, Ray inRay, out float2 hit)
{
    float3 invRaydir = 1.0 / inRay.mDirection;

    float3 t0 = (boundsMin - inRay.mOrigin) * invRaydir;
    float3 t1 = (boundsMax - inRay.mOrigin) * invRaydir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);
                
    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(tmax.x, min(tmax.y, tmax.z));
    
    if (dstA > dstB)
    {
        hit = -1;
        return false;
    }
    
    hit = float2(dstA, dstB);
    return true;
}

float2 RaySphereIntersection(float3 center, float radius, Ray ray)
{
    float3 of = ray.mOrigin - center;
    const float a = 1.0;
    float b = 2.0 * dot(of, ray.mDirection);
    float c = dot(of, of) - radius * radius;
    float discriminant = b * b - 4.0 * a * c;

    if (discriminant > 0)
    {
        discriminant = sqrt(discriminant);
        float dstToSphereNear = max(0.0, (-b - discriminant) / (2.0 * a));
        float dstToSphereFar = (-b + discriminant) / (2.0 * a);

        if (dstToSphereFar >= 0.0)
        {
            return float2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
        }
    }
    return float2(0.0, 0.0);
}

//
// Phase functions
//

float IsotropicPhase()
{
    return 1.0 / (4.0 * PI);
}

float HenyeyGreenstein(float inCosAngle, float inEccentricity)
{
    float eccentricity2 = inEccentricity * inEccentricity;
    return ((1.0 - eccentricity2) / pow((1.0 + eccentricity2 - 2.0 * inEccentricity * inCosAngle), 3.0 / 2.0)) / 4.0 * PI;
}

// 2-lobe phase function from 'Physically Based Sky, Atmosphere and Cloud Rendering in Frostbite'
// Allows users to better balance forward and backward scattering
// Default: inForwardScatter = 0.8, inBackwardScatter = -0.5, inWeight = 0.5
float DualLobePhase(float inCosAngle, float inForwardScatter, float inBackwardScatter, float inWeight)
{
    return lerp(HenyeyGreenstein(inCosAngle, inForwardScatter), HenyeyGreenstein(inCosAngle, inBackwardScatter), inWeight);
}

// Phase function presented in 'Nubis: Authoring Real-Time Volumetric Cloudscapes with the Decima Engine'
// inSilverIntensity controls the intensity of the second phase function and inSilverSpread controls its spread away from the sun
float HorizonPhase(float inCosAngle, float inEccentricity, float inSilverIntensity, float inSilverSpread)
{
    return max(HenyeyGreenstein(inCosAngle, inEccentricity), inSilverIntensity * HenyeyGreenstein(inCosAngle, 0.99 - inSilverSpread));
}
