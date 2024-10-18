
#define GetSurfaceAndBuiltinData GetSurfaceAndBuiltinDataBuiltin
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"

#define HAVE_MESH_MODIFICATION

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

#if SHADERPASS == SHADERPASS_GBUFFER && !defined(DEBUG_DISPLAY)
    // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
#endif

#if SHADERPASS == SHADERPASS_FORWARD && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(DEBUG_DISPLAY)
    // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
#endif

#if defined(_ALPHATEST_ON)
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define VARYINGS_NEED_TEXCOORD0
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#ifdef DEBUG_DISPLAY
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif
#ifdef SCENESELECTIONPASS
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

#if SHADERPASS == SHADERPASS_FORWARD
    // The light loop (or lighting architecture) is in charge to:
    // - Define light list
    // - Define the light loop
    // - Setup the constant/data
    // - Do the reflection hierarchy
    // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))
    #define HAS_LIGHTLOOP
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
#endif

#if defined(WRITE_DECAL_BUFFER) || (defined(WRITE_RENDERING_LAYER) && !defined(_DISABLE_DECALS))
#define OUTPUT_DECAL_BUFER
#endif

#if SHADERPASS != SHADERPASS_DEPTH_ONLY || defined(WRITE_NORMAL_BUFFER) || defined(OUTPUT_DECAL_BUFER)
    #define ATTRIBUTES_NEED_NORMAL
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define ATTRIBUTES_NEED_TANGENT // will be filled by ApplyMeshModification()
    #if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #ifdef EDITOR_VISUALIZATION
        #define ATTRIBUTES_NEED_TEXCOORD3
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define VARYINGS_NEED_TEXCOORD3
        #endif
    #endif
    // Varying - Use for pixel shader
    // This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
    #define VARYINGS_NEED_POSITION_WS
    #define VARYINGS_NEED_TANGENT_TO_WORLD
    #define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    // With per-pixel normal enabled, tangent space is created in the pixel shader.
    // #undef ATTRIBUTES_NEED_NORMAL
    // #undef ATTRIBUTES_NEED_TANGENT
    // #undef VARYINGS_NEED_TANGENT_TO_WORLD
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitData.hlsl"

#undef GetSurfaceAndBuiltinData

float3 AbsoluteWorldPosition;
float3 WorldNormal;

void GetSurfaceAndBuiltinData(inout FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    AbsoluteWorldPosition = GetAbsolutePositionWS(posInput.positionWS);

    WorldNormal = input.tangentToWorld[2];

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    float2 tc = input.texCoord0.xy;
    float2 terrainNormalMapUV = (tc + 0.5f) * _TerrainHeightmapRecipSize.xy;
    tc.xy *= _TerrainHeightmapRecipSize.zw;
    float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, terrainNormalMapUV).rgb * 2 - 1;
    float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
    WorldNormal = normalWS;
#endif
    
    GetSurfaceAndBuiltinDataBuiltin(input, V, posInput, surfaceData, builtinData);
}

#if SHADERPASS == SHADERPASS_GBUFFER
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"
#elif SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"
#elif SHADERPASS == SHADERPASS_SHADOWS || SHADERPASS == SHADERPASS_DEPTH_ONLY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
#elif SHADERPASS == SHADERPASS_FORWARD
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
#endif

#if SHADERPASS == SHADERPASS_DEPTH_ONLY
    #ifdef WRITE_NORMAL_BUFFER
        #if defined(_NORMALMAP)
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Normal0
        #elif defined(_MASKMAP)
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
        #endif
    #endif
#endif

#define TerrainLitShade TerrainLitShadeBuiltin
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"
#undef TerrainLitShade

#ifdef _NORMALMAP
    #define SampleNormal(i) SampleNormalGrad(_Normal##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv, _NormalScale##i)
#else
    #define SampleNormal(i) float3(0, 0, 0)
#endif

#ifdef _MASKMAP
    #define MaskModeMasks(i, blendMask) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv), blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
    #define SampleMasks(i, blendMask) lerp(DefaultMask(i), MaskModeMasks(i, blendMask), _LayerHasMask##i)
    #define NullMask(i)               float4(0, 1, _MaskMapRemapOffset##i.z, 0) // only height matters when weight is zero.
#else
    #define SampleMasks(i, blendMask) DefaultMask(i)
    #define NullMask(i)               float4(0, 1, 0, 0)
#endif

#define SampleResults(i, mask)                                                                                  \
    UNITY_BRANCH if (mask > 0)                                                                                  \
    {                                                                                                           \
        float2 splatuv = splatBaseUV * _Splat##i##_ST.xy + _Splat##i##_ST.zw;                                   \
        float2 splatdxuv = dxuv * _Splat##i##_ST.x;                                                             \
        float2 splatdyuv = dyuv * _Splat##i##_ST.y;                                                             \
        albedo[i] = SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv);            \
        albedo[i].rgb *= _DiffuseRemapScale##i.xyz;                                                             \
        normal[i] = SampleNormal(i);                                                                            \
        masks[i] = SampleMasks(i, mask);                                                                        \
    }                                                                                                           \
    else                                                                                                        \
    {                                                                                                           \
        albedo[i] = float4(0, 0, 0, 0);                                                                         \
        normal[i] = float3(0, 0, 0);                                                                            \
        masks[i] = NullMask(i);                                                                                 \
    }

#ifdef _NORMALMAP
    #define SamplePlaneNormal(i, uv, dx, dy, sw, sc) SampleNormalGrad(_Normal##i, sampler_Splat0, uv, dx, dy, _NormalScale##i) . sw * sc
#else
    #define SamplePlaneNormal(i, uv, dx, dy, sw, sc) float3(0, 0, 0)
#endif

#ifdef _MASKMAP
    #define MaskModePlaneMasks(i, blendMask, uv, dx, dy) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, uv, dx, dy), blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
    #define SamplePlaneMasks(i, blendMask, uv, dx, dy) lerp(DefaultMask(i), MaskModePlaneMasks(i, blendMask, uv, dx, dy), _LayerHasMask##i)
#else
    #define SamplePlaneMasks(i, blendMask, uv, dx, dy) SampleMasks(i, blendMask)
#endif

#define SamplePlane(i, m, tw, uv, dx, dy, sw, sc)                                                                       \
        if (tw > 0.0)                                                                                           \
        {                                                                                                       \
            albedo[i] += SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, uv, dx, dy) * float4(_DiffuseRemapScale##i.xyz, 1) * tw; \
            normal[i] += SamplePlaneNormal(i, uv, dx, dy, sw, sc) * tw;                                                 \
            masks[i] = lerp(masks[i], SamplePlaneMasks(i, m, uv, dx, dy), tw); /* TODO: FIXME */                \
        }                                                                                                       \

#define SampleTriplanar(i, mask)                                                                                \
    {                                                                                                           \
        float2 uvXY##i, uvXZ##i, uvZY##i;                                                                       \
        GetTriplanarCoordinate(AbsoluteWorldPosition * _Splat##i##_ST.x, uvXZ##i, uvXY##i, uvZY##i);            \
                                                                                                                \
        float3 triddx##i = ddx(AbsoluteWorldPosition * _Splat##i##_ST.x);                                       \
        float3 triddy##i = ddx(AbsoluteWorldPosition * _Splat##i##_ST.x);                                       \
                                                                                                                \
        albedo[i] = float4(0, 0, 0, 0);                                                                         \
        normal[i] = float3(0, 0, 0);                                                                            \
        masks[i] = NullMask(i);                                                                                 \
                                                                                                                \
        UNITY_BRANCH if (mask > 0)                                                                              \
        {                                                                                                       \
            float3 triWeights = ComputeTriplanarWeights(WorldNormal);                                           \
            SamplePlane(i, mask, triWeights[0], uvZY##i, triddx##i.zy, triddy##i.zy, grb, float3(1, -1, 1));    \
            SamplePlane(i, mask, triWeights[1], uvXZ##i, triddx##i.xz, triddy##i.xz, rgb, float3(1, -1, 1));    \
            SamplePlane(i, mask, triWeights[2], uvXY##i, triddx##i.xy, triddy##i.xy, rgb, float3(1, -1, 1));    \
        }                                                                                                       \
    }                                                                                                           \

float3 Overlay(float3 Base, float3 Blend, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    float3 Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    return lerp(Base, Out, Opacity);
}

void CustomTerrainSplatBlend(float2 controlUV, float2 splatBaseUV, inout TerrainLitSurfaceData surfaceData)
{
    float4 albedo[_LAYER_COUNT];
    float3 normal[_LAYER_COUNT];
    float4 masks[_LAYER_COUNT];

    float2 dxuv = ddx(splatBaseUV);
    float2 dyuv = ddy(splatBaseUV);

    float2 blendUV0 = (controlUV.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
    float4 blendMasks0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, blendUV0);
    #ifdef _TERRAIN_8_LAYERS
        float2 blendUV1 = (controlUV.xy * (_Control1_TexelSize.zw - 1.0f) + 0.5f) * _Control1_TexelSize.xy;
        float4 blendMasks1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, blendUV1);
    #else
        float4 blendMasks1 = float4(0, 0, 0, 0);
    #endif

    SampleResults(0, 1 - blendMasks0.y);
    SampleResults(1, blendMasks0.y);
    SampleResults(2, blendMasks0.z);
    SampleTriplanar(2, blendMasks0.z);
    SampleResults(3, blendMasks0.w);
    #ifdef _TERRAIN_8_LAYERS
        SampleResults(4, blendMasks1.x);
        SampleResults(5, blendMasks1.y);
        SampleResults(6, blendMasks1.z);
        SampleResults(7, blendMasks1.w);
    #endif

    // Color blend
    UNITY_BRANCH if (blendMasks0.y != 1.f)
    {
        float height = AbsoluteWorldPosition.y;
        UNITY_BRANCH if (height > _Layer1_Height_Base_Origin)
        {
            float blendFactor = saturate((height - _Layer1_Height_Base_Origin) * rcp(_Layer1_Height_Gradient_Width));
            float3 color = lerp(_Layer1_Height_Gradient_Color_Base, _Layer1_Height_Gradient_Color_Top, blendFactor);
            float opacity = lerp(_Layer1_Height_Gradient_Color_Opacity_Base, _Layer1_Height_Gradient_Color_Opacity_Top, blendFactor);
            albedo[0].rgb = Overlay(albedo[0].rgb, color, opacity);
        }
    }
    
    float weights[_LAYER_COUNT];
    ZERO_INITIALIZE_ARRAY(float, weights, _LAYER_COUNT);

    #ifdef _MASKMAP
        #if defined(_TERRAIN_BLEND_HEIGHT)
            // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
            float maxHeight = masks[0].z;
            maxHeight = max(maxHeight, masks[1].z);
            maxHeight = max(maxHeight, masks[2].z);
            maxHeight = max(maxHeight, masks[3].z);
            #ifdef _TERRAIN_8_LAYERS
                maxHeight = max(maxHeight, masks[4].z);
                maxHeight = max(maxHeight, masks[5].z);
                maxHeight = max(maxHeight, masks[6].z);
                maxHeight = max(maxHeight, masks[7].z);
            #endif

            // Make sure that transition is not zero otherwise the next computation will be wrong.
            // The epsilon here also has to be bigger than the epsilon in the next computation.
            float transition = max(_HeightTransition, 1e-5);

            // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
            // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
            float4 weightedHeights0 = { masks[0].z, masks[1].z, masks[2].z, masks[3].z };
            weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
            // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
            weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

            #ifdef _TERRAIN_8_LAYERS
                float4 weightedHeights1 = { masks[4].z, masks[5].z, masks[6].z, masks[7].z };
                weightedHeights1 = weightedHeights1 - maxHeight.xxxx;
                weightedHeights1 = (max(0, weightedHeights1 + transition) + 1e-6) * blendMasks1;
            #else
                float4 weightedHeights1 = { 0, 0, 0, 0 };
            #endif

            // Normalize
            float sumHeight = GetSumHeight(weightedHeights0, weightedHeights1);
            blendMasks0 = weightedHeights0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = weightedHeights1 / sumHeight.xxxx;
            #endif
        #elif defined(_TERRAIN_BLEND_DENSITY)
            // Denser layers are more visible.
            float4 opacityAsDensity0 = saturate((float4(albedo[0].a, albedo[1].a, albedo[2].a, albedo[3].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
            opacityAsDensity0 += 0.001f * blendMasks0;      // if all weights are zero, default to what the blend mask says
            float4 useOpacityAsDensityParam0 = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
            blendMasks0 = lerp(opacityAsDensity0, blendMasks0, useOpacityAsDensityParam0);
            #ifdef _TERRAIN_8_LAYERS
                float4 opacityAsDensity1 = saturate((float4(albedo[4].a, albedo[5].a, albedo[6].a, albedo[7].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks1)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
                opacityAsDensity1 += 0.001f * blendMasks1;  // if all weights are zero, default to what the blend mask says
                float4 useOpacityAsDensityParam1 = { _DiffuseRemapScale4.w, _DiffuseRemapScale5.w, _DiffuseRemapScale6.w, _DiffuseRemapScale7.w };
                blendMasks1 = lerp(opacityAsDensity1, blendMasks1, useOpacityAsDensityParam1);
            #endif

            // Normalize
            float sumHeight = GetSumHeight(blendMasks0, blendMasks1);
            blendMasks0 = blendMasks0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = blendMasks1 / sumHeight.xxxx;
            #endif
        #endif // if _TERRAIN_BLEND_HEIGHT
    #endif // if _MASKMAP

    weights[0] = 1;
    weights[1] = blendMasks0.y;
    weights[2] = blendMasks0.z;
    weights[3] = blendMasks0.w;
    #ifdef _TERRAIN_8_LAYERS
        weights[4] = blendMasks1.x;
        weights[5] = blendMasks1.y;
        weights[6] = blendMasks1.z;
        weights[7] = blendMasks1.w;
    #endif

    surfaceData.albedo = 0;
    surfaceData.normalData = 0;
    float3 outMasks = 0;
    UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
    {
        UNITY_BRANCH if(weights[i] > 0)
        {
            surfaceData.albedo = lerp(surfaceData.albedo, albedo[i].rgb, weights[i]);
            surfaceData.normalData = lerp(surfaceData.normalData, normal[i].rgb, weights[i]); // no need to normalize
            outMasks = lerp(outMasks, masks[i].xyw, weights[i]);
        }
    }
    surfaceData.smoothness = outMasks.z;
    surfaceData.metallic = outMasks.x;
    surfaceData.ao = outMasks.y;

    UNITY_BRANCH if (blendMasks0.z != 1.f)
    {
        float height = AbsoluteWorldPosition.y;

        UNITY_BRANCH if (height > _Snow_Height_Base_Origin)
        {
            float blendFactor = (1.f - blendMasks0.z) * saturate((height - _Snow_Height_Base_Origin) * rcp(_Snow_Height_Gradient_Width));
            surfaceData.albedo = lerp(surfaceData.albedo, _Snow_Color_Top, blendFactor);
        }
    }
}

#undef SampleNormal
#undef SampleMasks
#undef SampleResults

#undef SamplePlaneNormal
#undef MaskModePlaneMasks
#undef SamplePlaneMasks
#undef SampleTriplanar

void TerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData)
{
    CustomTerrainSplatBlend(uv, uv, surfaceData);
}

// #if SHADERPASS == SHADERPASS_GBUFFER
//
// void FragGBuffer(
//     PackedVaryingsToPS packedInput,
//     OUTPUT_GBUFFER(outGBuffer)
// #ifdef _DEPTHOFFSET_ON
//     , out float outputDepth : DEPTH_OFFSET_SEMANTIC
// #endif
//     )
// {
//     WorldNormal = float3(0,1,0);
// #ifdef VARYINGS_NEED_TANGENT_TO_WORLD
//     WorldNormal = packedInput.interpolators1.xyz;
// #endif // VARYINGS_NEED_TANGENT_TO_WORLD
//     //WorldNormal = float3(0,1,0);
//
//     Frag(
//         packedInput
// #if GBUFFERMATERIAL_COUNT >= 2
//     , MERGE_NAME(outGBuffer, 0)
//     , MERGE_NAME(outGBuffer, 1)
// #endif
// #if GBUFFERMATERIAL_COUNT >= 3
//     , MERGE_NAME(outGBuffer, 2)
// #endif
// #if GBUFFERMATERIAL_COUNT >= 4
//     , MERGE_NAME(outGBuffer, 3)
// #endif
// #if GBUFFERMATERIAL_COUNT >= 5
//     , MERGE_NAME(outGBuffer, 4)
// #endif
// #if GBUFFERMATERIAL_COUNT >= 6
//     , MERGE_NAME(outGBuffer, 5)
// #endif
// #if GBUFFERMATERIAL_COUNT == 7
//     , MERGE_NAME(outGBuffer, 6)
// #endif
// #ifdef _DEPTHOFFSET_ON
//     , outputDepth
// #endif
//     );
// }
//
// #endif
