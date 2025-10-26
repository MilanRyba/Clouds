
//=== Worley Noise ==========
float hash(float n)
{
    return frac(sin(n + 1.951) * 43758.5453);
}

float noise(float3 x)
{
    float3 p = floor(x);
    float3 f = frac(x);

    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
    return lerp(
	        lerp(
		        lerp(hash(n + 0.0), hash(n + 1.0), f.x),
			    lerp(hash(n + 57.0), hash(n + 58.0), f.x),
			    f.y),
		    lerp(
			    lerp(hash(n + 113.0), hash(n + 114.0), f.x),
			    lerp(hash(n + 170.0), hash(n + 171.0), f.x),
			    f.y),
		    f.z);
}

float3 mod(float3 x, float3 y)
{
    return x - y * floor(x / y);
}

float Worley(float3 coord, float cellCount)
{
    const float3 pCell = coord * cellCount;
    float d = 1.0e10;
    for (int xo = -1; xo <= 1; xo++)
    {
        for (int yo = -1; yo <= 1; yo++)
        {
            for (int zo = -1; zo <= 1; zo++)
            {
                float3 tp = floor(pCell) + float3(xo, yo, zo);

                tp = pCell - tp - noise(mod(tp, cellCount / 1));

                d = min(d, dot(tp, tp));
            }
        }
    }
    
    // d = sqrt(d);
    d = saturate(d);
    return d;
}