struct Bone
{
	float4 pose0;
	float4 pose1;
	float4 pose2;
};
StructuredBuffer<Bone> boneBuffer;

ByteAddressBuffer vertexBuffer_POS;
ByteAddressBuffer vertexBuffer_TAN;
ByteAddressBuffer vertexBuffer_BON;

RwByteAddressBuffer streamoutBuffer_POS;
RwByteAddressBuffer streamoutBuffer_TAN;


inline void Skinning(inout float3 pos, inout float3 nor, inout float3 tan, in float4 inBon, in float4 inWei)
{
	if (any(inWei))
	{
		float4 p = 0;
		float3 n = 0;
		float3 t = 0;
		float weisum = 0;

		// force loop to reduce register pressure
		//  also enabled early-exit
		[loop]
		for (uint i = 0; ((i < 4) && (weisum < 1.0f)); ++i)
		{
			float4x4 m = float4x4(
				boneBuffer[(uint)inBon[i]].pose0,
				boneBuffer[(uint)inBon[i]].pose1,
				boneBuffer[(uint)inBon[i]].pose2,
				float4(0, 0, 0, 1)
				);

			p += mul(m, float4(pos.xyz, 1)) * inWei[i];
			n += mul((float3x3)m, nor.xyz) * inWei[i];
			t += mul((float3x3)m, tan.xyz) * inWei[i];

			weisum += inWei[i];
		}

		pos.xyz = p.xyz;
		nor.xyz = normalize(n.xyz);
		tan.xyz = normalize(t.xyz);
	}
}