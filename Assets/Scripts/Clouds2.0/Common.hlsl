#pragma once

float InverseLerp(float value, float minValue, float maxValue)
{
    return (value - minValue) / (maxValue - minValue);
}

float2 InverseLerp(float2 value, float2 minValue, float2 maxValue)
{
    return (value - minValue) / (maxValue - minValue);
}

// Maps a value from one range to another
float Remap(float original_value, float original_min, float original_max, float new_min, float new_max)
{
    return new_min + (((original_value - original_min) / (original_max - original_min)) * (new_max - new_min));
}

float2 TexelToUV(uint2 texel, float2 texelSize)
{
    return ((float2) texel + 0.5f) * texelSize;
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