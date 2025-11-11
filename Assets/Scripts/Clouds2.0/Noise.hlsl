#pragma once
#include "Common.hlsl"

// Move to Random.hlsl
uint HashU32(uint seed)
{
    uint state = seed * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

uint XORShift(inout uint rng_state)
{
	// Xorshift algorithm from George Marsaglia's paper.
    rng_state ^= (rng_state << 13);
    rng_state ^= (rng_state >> 17);
    rng_state ^= (rng_state << 5);
    return rng_state;
}

float Random01(inout uint rng_state)
{
    return asfloat(0x3f800000 | XORShift(rng_state) >> 9) - 1.0;
}

// David Hoskins Hash
#define UI0 1597334673U
#define UI1 3812015801U
#define UI2 uint2(UI0, UI1)
#define UI3 uint3(UI0, UI1, 2798796415U)
#define UIF (1.0 / float(0xffffffffU))

float3 hash33(float3 p)
{
    uint3 q = uint3(p) * UI3;
    q = (q.x ^ q.y ^ q.z) * UI3;
    return -1. + 2. * float3(q) * UIF;
}

float2 hash33(float2 p)
{
    uint2 q = uint2(p) * UI2;
    q = (q.x ^ q.y) * UI2;
    return -1. + 2. * float2(q) * UIF;
}

float3 WrapNoiseValue(float3 value, float frequency)
{
    return frac(value / frequency) * frequency;
}

// 3D Wrapped version
float GradientNoise(float3 x, float freq)
{
    // grid
    uint3 p = floor(x);
    float3 w = frac(x);

    // quintic interpolant
    float3 u = w * w * w * (w * (w * 6. - 15.) + 10.);

    // gradients
    float3 ga = hash33(WrapNoiseValue(p + float3(0., 0., 0.), freq));
    float3 gb = hash33(WrapNoiseValue(p + float3(1., 0., 0.), freq));
    float3 gc = hash33(WrapNoiseValue(p + float3(0., 1., 0.), freq));
    float3 gd = hash33(WrapNoiseValue(p + float3(1., 1., 0.), freq));
    float3 ge = hash33(WrapNoiseValue(p + float3(0., 0., 1.), freq));
    float3 gf = hash33(WrapNoiseValue(p + float3(1., 0., 1.), freq));
    float3 gg = hash33(WrapNoiseValue(p + float3(0., 1., 1.), freq));
    float3 gh = hash33(WrapNoiseValue(p + float3(1., 1., 1.), freq));

    // projections
    float va = dot(ga, w - float3(0., 0., 0.));
    float vb = dot(gb, w - float3(1., 0., 0.));
    float vc = dot(gc, w - float3(0., 1., 0.));
    float vd = dot(gd, w - float3(1., 1., 0.));
    float ve = dot(ge, w - float3(0., 0., 1.));
    float vf = dot(gf, w - float3(1., 0., 1.));
    float vg = dot(gg, w - float3(0., 1., 1.));
    float vh = dot(gh, w - float3(1., 1., 1.));

    // interpolation
    return va +
           u.x * (vb - va) +
           u.y * (vc - va) +
           u.z * (ve - va) +
           u.x * u.y * (va - vb - vc + vd) +
           u.y * u.z * (va - vc - ve + vg) +
           u.z * u.x * (va - vb - ve + vf) +
           u.x * u.y * u.z * (-va + vb + vc - vd + ve - vf - vg + vh);
}

float WorleyNoise(float3 uvw, uint pointsPerRow, uint seed)
{
    uvw *= pointsPerRow;
    float3 frc = frac(uvw);
    uint3 i = floor(uvw);

    float minDistSq = 1;
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            for (int z = -1; z <= 1; ++z)
            {
                int3 offset = int3(x, y, z);
                int3 neighbourCellWrappedId = int3(i + offset + pointsPerRow) % pointsPerRow;
                uint pSeed = HashU32(seed + Flatten3D(neighbourCellWrappedId, pointsPerRow));
                float3 p = float3(Random01(pSeed), Random01(pSeed), Random01(pSeed)) + offset;
                minDistSq = min(minDistSq, dot(frc - p, frc - p));
            }
        }
    }
    return 1.0f - sqrt(minDistSq);
}