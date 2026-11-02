#ifndef HOLDIT_TOON_LIGHTING_INCLUDED
#define HOLDIT_TOON_LIGHTING_INCLUDED

// Toon shading of the board, called from Toon.shadergraph (Custom Function, File mode).
// Lambert = max(N.L, 0) x the main light's shadow attenuation.
// Band    = ShadeDark below BandLow, ShadeMid up to BandHigh, ShadeLit above; each step blends over +-BandSoftness.
// Rim     = (1 - N.V)^RimPower x RimStrength, only where Lambert > RimGate.
// Shade   = min(1, Band x LightScale + Ambient + Rim); the colour is base x Shade. The light's colour and intensity are
// not used: the painted texture holds the palette, a fully lit top face shows it as is.
void ToonShade_float(float3 PositionWS, float3 NormalWS, float3 ViewDirectionWS,
    float BandLow, float BandHigh, float BandSoftness,
    float ShadeDark, float ShadeMid, float ShadeLit,
    float LightScale, float Ambient,
    float RimPower, float RimStrength, float RimGate,
    out float Shade)
{
#if defined(SHADERGRAPH_PREVIEW)
    float3 lightDirection = normalize(float3(-4.0, 14.0, -6.0));
    float shadow = 1.0;
#else
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(PositionWS), PositionWS, half4(1, 1, 1, 1));
    float3 lightDirection = mainLight.direction;
    float shadow = mainLight.shadowAttenuation;
#endif
    float3 normal = normalize(NormalWS);
    float lambert = saturate(dot(normal, lightDirection)) * shadow;

    float band = lerp(ShadeDark, ShadeMid, smoothstep(BandLow - BandSoftness, BandLow + BandSoftness, lambert));
    band = lerp(band, ShadeLit, smoothstep(BandHigh - BandSoftness, BandHigh + BandSoftness, lambert));

    float facing = saturate(1.0 - dot(normal, normalize(ViewDirectionWS)));
    float rim = lambert > RimGate ? pow(facing, RimPower) * RimStrength : 0.0;

    Shade = min(1.0, band * LightScale + Ambient + rim);
}

#endif
