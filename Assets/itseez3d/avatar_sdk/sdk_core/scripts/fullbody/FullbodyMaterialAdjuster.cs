/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, December 2020
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using static GLTF.CoroutineGLTFSceneImporter;

namespace ItSeez3D.AvatarSdk.Core
{
	public class FullbodyMaterialAdjuster
	{
		enum ShaderType
		{
			Standard,
			HaircutStrands,
			HaircutSolid
		}

		private static Dictionary<string, ShaderType> haircutsShaders = new Dictionary<string, ShaderType>()
		{
			{ "wavy_bob", ShaderType.HaircutStrands },
			{ "very_long", ShaderType.HaircutStrands },
			{ "shoulder_length", ShaderType.HaircutStrands },
			{ "short_parted", ShaderType.HaircutStrands },
			{ "short_curls", ShaderType.HaircutStrands },
			{ "roman", ShaderType.HaircutStrands },
			{ "rasta", ShaderType.HaircutStrands },
			{ "mid_length_straight2", ShaderType.HaircutStrands },
			{ "mid_length_ruffled", ShaderType.HaircutStrands },
			{ "corkscrew_curls", ShaderType.HaircutStrands },
			{ "bob_parted", ShaderType.HaircutStrands },

			{ "short_slick", ShaderType.HaircutSolid },
			{ "mid_length_wispy", ShaderType.HaircutSolid },
			{ "long_crimped", ShaderType.HaircutSolid },
			{ "balding", ShaderType.HaircutSolid },
			{ "short_disheveled", ShaderType.HaircutSolid },
			{ "ponytail_with_bangs", ShaderType.HaircutSolid },
			{ "mid_length_straight", ShaderType.HaircutSolid },
			{ "long_wavy", ShaderType.HaircutSolid },
			{ "long_disheveled", ShaderType.HaircutSolid },
			{ "short_simple", ShaderType.HaircutSolid },
			{ "generated", ShaderType.HaircutSolid }
		};

		private IFullbodyPersistentStorage persistentStorage = null;

		public FullbodyMaterialAdjuster()
		{
			persistentStorage = AvatarSdkMgr.FullbodyStorage();
		}

		public IEnumerator PrepareBodyMaterial(CoroutineResult<Material> executionResult, string avatarCode, bool withPbrTextures)
		{
			var materialType = withPbrTextures ? RenderingPipelineTraits.MaterialTemplate.BodyPbr : RenderingPipelineTraits.MaterialTemplate.Body;
			string templateMaterialName = RenderingPipelineTraits.GetMaterialName(materialType);
			Material templateMaterial = Resources.Load<Material>(templateMaterialName);

			if (templateMaterial == null)
			{
				Debug.LogError("Template body material isn't found!");
				executionResult.result = null;
				yield break;
			}
			Material bodyMaterial = new Material(templateMaterial);

			string bodyTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.Texture);
			var textureRequest = ImageUtils.LoadTextureAsync(bodyTextureFilename);
			yield return textureRequest;
			if(textureRequest.Result != null)
				UpdateBodyMainTexture(bodyMaterial, textureRequest.Result);

			if (withPbrTextures)
			{
				
				string metallnessTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.MetallnessMap);
				string roughnessTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.RoughnessMap);
				string normalTextureFilename = persistentStorage.GetAvatarFile(avatarCode, FullbodyAvatarFileType.NormalMap);

				yield return ConfigurePbrTextures(bodyMaterial, normalTextureFilename, metallnessTextureFilename, roughnessTextureFilename);
			}

			executionResult.result = bodyMaterial;
		}

		public IEnumerator PrepareTransparentBodyTextureForOutfit(CoroutineResult<Texture2D> executionResult, Texture2D opaqueBodyTexture, string outfitDir, string outfitName)
		{
			string alphaMaskTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.BodyVisibilityMask);
			if (File.Exists(alphaMaskTextureFilename))
			{
				var textureRequest = ImageUtils.LoadTextureAsync(alphaMaskTextureFilename);
				yield return textureRequest;
				Texture2D transparentBodyTexture = textureRequest.Result;
				if (transparentBodyTexture != null)
				{
					Color32[] transparentTexColors = transparentBodyTexture.GetPixels32();
					Color32[] bodyTexColors = opaqueBodyTexture.GetPixels32();
					for (int i = 0; i < bodyTexColors.Length; i++)
						bodyTexColors[i].a = transparentTexColors[i].r;

					transparentBodyTexture.SetPixels32(bodyTexColors);
					transparentBodyTexture.Apply();
					executionResult.result = transparentBodyTexture;
				}
			}
			else
			{
				Debug.LogWarningFormat("Body visibility mask not found: {0}", alphaMaskTextureFilename);
			}
		}

		public void UpdateBodyMainTexture(Material bodySharedMaterial, Texture2D bodyTexture)
		{
			bodySharedMaterial.SetTexture(RenderingPipelineTraits.GetTextureName(), bodyTexture);
		}

		public IEnumerator PrepareHairMaterial(CoroutineResult<Material> executionResult, string haircutDir, string haircutName)
		{
			string haircutTextureFilename = persistentStorage.GetHaircutFileInDir(haircutDir, haircutName, FullbodyHaircutFileType.Texture);
			var textureRequest = ImageUtils.LoadTextureAsync(haircutTextureFilename);
			yield return textureRequest;

			Texture2D haircutTexture = textureRequest.Result;
			if (haircutTexture == null)
			{
				Debug.LogErrorFormat("Unable to load texture for {0} haircut", haircutName);
				yield break;
			}

			if (haircutsShaders.ContainsKey(haircutName))
			{
				if (haircutsShaders[haircutName] == ShaderType.HaircutSolid)
					executionResult.result = PrepareHairMaterialWithAvatarSdkSolidShader(haircutTexture);
				else
					executionResult.result = PrepareHairMaterialWithAvatarSdkStrandsShader(haircutTexture);
			}
			else
				executionResult.result = PrepareHairMaterialWithStandardShader(haircutTexture);
		}

		public IEnumerator PrepareOutfitMaterial(CoroutineResult<Material> executionResult, string outfitDir, string outfitName, bool withPbrTextures)
		{
			Material templateMaterial = Resources.Load<Material>(RenderingPipelineTraits.GetMaterialName(RenderingPipelineTraits.MaterialTemplate.Outfit));
			if (templateMaterial == null)
			{
				Debug.LogError("Template outfit material isn't found!");
				yield break;
			}

			Material outfitMaterial = new Material(templateMaterial);

			string outfitTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.Texture);
			var textureRequest = ImageUtils.LoadTextureAsync(outfitTextureFilename);
			yield return textureRequest;
			if (textureRequest.Result != null)
				outfitMaterial.SetTexture(RenderingPipelineTraits.GetTextureName(), textureRequest.Result);

			if (withPbrTextures)
			{
				string metallnessTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.MetallnessMap);
				string roughnessTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.RoughnessMap);
				string normalTextureFilename = persistentStorage.GetOutfitFileInDir(outfitDir, outfitName, OutfitFileType.NormalMap);

				yield return ConfigurePbrTextures(outfitMaterial, normalTextureFilename, metallnessTextureFilename, roughnessTextureFilename);
			}

			executionResult.result = outfitMaterial;
		}

		private Material PrepareHairMaterialWithStandardShader(Texture2D mainTexture)
		{
			Material templateMaterial = Resources.Load<Material>(RenderingPipelineTraits.GetMaterialName(RenderingPipelineTraits.MaterialTemplate.Haircut));
			if (templateMaterial == null)
			{
				Debug.LogError("Template haircut material isn't found!");
				return null;
			}

			Material hairMaterial = new Material(templateMaterial);

			hairMaterial.SetTexture(RenderingPipelineTraits.GetTextureName(), mainTexture);
			hairMaterial.SetTexture("_EmissionMap", mainTexture);

			return hairMaterial;
		}

		private Material PrepareHairMaterialWithAvatarSdkSolidShader(Texture2D mainTexture)
		{
			string name = RenderingPipelineTraits.GetShaderName(RenderingPipelineTraits.ShaderKind.HairSolidLit);
			Shader solidShader = Shader.Find(name);
			if (solidShader == null)
			{
				Debug.LogErrorFormat("{0} shader wasn't found. Use Standard shader for haircut.", ShadersUtils.haircutSolidLitShaderName);
				return PrepareHairMaterialWithStandardShader(mainTexture);
			}

			Material hairMaterial = new Material(solidShader);
			hairMaterial.shader = solidShader;
			hairMaterial.renderQueue = (int)RenderQueue.Transparent;
			hairMaterial.SetTexture("_MainTex", mainTexture);
			return hairMaterial;
		}

		private Material PrepareHairMaterialWithAvatarSdkStrandsShader(Texture2D mainTexture)
		{
			string name = RenderingPipelineTraits.GetShaderName(RenderingPipelineTraits.ShaderKind.HairStrandsLit);
			Shader strandShader = Shader.Find(name);
			if (strandShader == null)
			{
				Debug.LogErrorFormat("{0} shader wasn't found. Use Standard shader for haircut.", ShadersUtils.haircutStrandLitShaderName);
				return PrepareHairMaterialWithStandardShader(mainTexture);
			}

			Material hairMaterial = new Material(strandShader);
			hairMaterial.shader = strandShader;
			hairMaterial.SetTexture("_MainTex", mainTexture);
			return hairMaterial;
		}

		private IEnumerator ConfigurePbrTextures(Material material, string normalMapFilename, string metallicMapFilename, string roughnessMapFilename)
		{
			Color32[] metallnessColors = null;
			Texture2D metallnessTexture = null;
			if (File.Exists(metallicMapFilename))
			{
				var textureRequest = ImageUtils.LoadTextureAsync(metallicMapFilename);
				yield return textureRequest;
				metallnessTexture = textureRequest.Result;
				if (metallnessTexture != null)
					metallnessColors = metallnessTexture.GetPixels32();
			}
			else
				Debug.LogWarningFormat("Texture not found: {0}", metallicMapFilename);

			Color32[] roughnessColors = null;
			if (File.Exists(roughnessMapFilename))
			{
				var textureRequest = ImageUtils.LoadTextureAsync(roughnessMapFilename);
				yield return textureRequest;
				Texture2D roughnessTexture = textureRequest.Result;
				if (roughnessTexture != null)
				{
					roughnessColors = roughnessTexture.GetPixels32();
					Object.DestroyImmediate(roughnessTexture);
				}
			}
			else
				Debug.LogWarningFormat("Texture not found: {0}", roughnessMapFilename);

			if (metallnessTexture != null)
			{
				for (int i = 0; i < metallnessColors.Length; i++)
				{
					metallnessColors[i].a = (byte)(255 - roughnessColors[i].r);
				}
				metallnessTexture.SetPixels32(metallnessColors);
				metallnessTexture.Apply(true, true);
				material.SetTexture("_MetallicGlossMap", metallnessTexture);
			}

			if (File.Exists(normalMapFilename))
			{
				var textureRequest = ImageUtils.LoadNormalMapTextureAsync(normalMapFilename);
				yield return textureRequest;
				if (textureRequest.Result != null)
					material.SetTexture("_BumpMap", textureRequest.Result);
				
			}

			Resources.UnloadUnusedAssets();
		}
	}
}
