#include "Macros.fxh"


#ifdef SM4
DECLARE_TEXTURE(Texture, 0);
DECLARE_TEXTURE(Texture2, 1);
#else
texture Texture;
sampler TextureSampler : register(s0) = sampler_state
{
	Texture = (Texture);
	MAGFILTER = POINT;
	MINFILTER = POINT;
	MIPFILTER = POINT;
	AddressU = Wrap;
	AddressV = Wrap;
};
texture Texture2;
sampler Texture2Sampler : register(s1)  = sampler_state
{
	Texture = (Texture2);
	MAGFILTER = POINT; //LINEAR;
	MINFILTER = POINT; //LINEAR;
	MIPFILTER = POINT; //LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};
#endif


BEGIN_CONSTANTS

    float4 DiffuseColor     _vs(c0) _cb(c0);
    float3 FogColor         _ps(c0) _cb(c1);
    float4 FogVector        _vs(c5) _cb(c2);
    float2 MapSize;
	float2 InvAtlasSize;

MATRIX_CONSTANTS

    float4x4 WorldViewProj  _vs(c1) _cb(c0);

END_CONSTANTS


#include "Structures.fxh"
#include "Common.fxh"


// Vertex shader: basic.
VSOutputTx VSDualTexture(VSInputTx vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParams;
    
    vout.TexCoord = vin.TexCoord;

    return vout;
}


// Vertex shader: no fog.
VSOutputTxNoFog VSDualTextureNoFog(VSInputTx vin)
{
    VSOutputTxNoFog vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParamsNoFog;
    
    vout.TexCoord = vin.TexCoord;

    return vout;
}


// Vertex shader: vertex color.
VSOutputTx VSDualTextureVc(VSInputTxVc vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParams;
    
    vout.TexCoord = vin.TexCoord;
    vout.Diffuse *= vin.Color;
    
    return vout;
}


// Vertex shader: vertex color, no fog.
VSOutputTxNoFog VSDualTextureVcNoFog(VSInputTxVc vin)
{
    VSOutputTxNoFog vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParamsNoFog;
    
    vout.TexCoord = vin.TexCoord;
    vout.Diffuse *= vin.Color;
    
    return vout;
}


// Pixel shader: basic.
float4 PSDualTexture(VSOutputTx pin) : SV_Target0
{
    float2 txCoord = pin.TexCoord * MapSize;
    float2 txCoordi = floor(txCoord);
    float2 tx2Coord = (txCoord - txCoordi);

    float4 mapColor = SAMPLE_TEXTURE(Texture, txCoordi/MapSize);
    float2 tileCoord = mapColor.xy * 255;
    float alpha = mapColor.a;
	
    tx2Coord = (tx2Coord + tileCoord) * InvAtlasSize;

    float4 color = SAMPLE_TEXTURE(Texture2, tx2Coord);
    color *= alpha;
    color *= pin.Diffuse;
    
    ApplyFog(color, pin.Specular.w);
    
    return color;
}


// Pixel shader: no fog.
float4 PSDualTextureNoFog(VSOutputTxNoFog pin) : SV_Target0
{
    float2 txCoord = pin.TexCoord * MapSize;
    float2 txCoordi = floor(txCoord);
    float2 tx2Coord = (txCoord - txCoordi);

    float4 mapColor = SAMPLE_TEXTURE(Texture, txCoordi/MapSize);
	float2 tileCoord = mapColor.xy * 255;
	float alpha = mapColor.a;
	
    tx2Coord = (tx2Coord + tileCoord) * InvAtlasSize;

	float4 color = SAMPLE_TEXTURE(Texture2, tx2Coord);
	color *= alpha;
    color *= pin.Diffuse;

    return color;
}


// NOTE: The order of the techniques here are
// defined to match the indexing in DualTextureEffect.cs.

TECHNIQUE( DualTextureEffect,					VSDualTexture,			PSDualTexture );
TECHNIQUE( DualTextureEffect_NoFog,				VSDualTextureNoFog,		PSDualTextureNoFog );
TECHNIQUE( DualTextureEffect_VertexColor,		VSDualTextureVc,		PSDualTexture );
TECHNIQUE( DualTextureEffect_VertexColor_NoFog,	VSDualTextureVcNoFog,	PSDualTextureNoFog );
