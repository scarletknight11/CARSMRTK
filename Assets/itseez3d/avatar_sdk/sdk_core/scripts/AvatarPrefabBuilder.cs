/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace ItSeez3D.AvatarSdk.Core
{
	public class AvatarPrefabBuilder
	{
		#region signletion staff
		protected static AvatarPrefabBuilder instance = null;

		protected AvatarPrefabBuilder() { }

		public static AvatarPrefabBuilder Instance
		{
			get
			{
				if (instance == null)
					instance = new AvatarPrefabBuilder();
				return instance;
			}
		}
		#endregion

		public void CreateAvatarPrefab (GameObject avatarObject, string headObjectName, string haircutObjectName, string avatarId, 
			string haircutId, List<Type> removedObjectsTypes, int levelOfDetails = 0)
		{
			string prefabDir = Path.Combine(PluginStructure.GetPluginDirectoryPath(PluginStructure.PREFABS_DIR, PathOriginOptions.RelativeToAssetsFolder), avatarId);
			PluginStructure.CreatePluginDirectory(prefabDir);
			GameObject instantiatedAvatarObject = GameObject.Instantiate(avatarObject);
			SaveMeshAndMaterialForAvatarObject(prefabDir, instantiatedAvatarObject, headObjectName, haircutObjectName, avatarId, haircutId, levelOfDetails);

			CopyBlendshapesWeights(avatarObject, instantiatedAvatarObject, headObjectName);

			string prefabPath = prefabDir + "/avatar.prefab";
			if (removedObjectsTypes != null)
			{
				foreach (Type t in removedObjectsTypes)
				{
					var component = instantiatedAvatarObject.GetComponent(t);
					if (component != null)
						GameObject.DestroyImmediate(component);
				}
			}
			PrefabUtility.SaveAsPrefabAsset(instantiatedAvatarObject, prefabPath);

			GameObject.DestroyImmediate(instantiatedAvatarObject);
			EditorUtility.DisplayDialog ("Prefab created successfully!", string.Format ("You can find your prefab in '{0}' folder", prefabDir), "Ok");
		}

		protected void SaveMeshAndMaterialForAvatarObject(string prefabDir, GameObject avatarObject, string headObjectName, string haircutObjectName, 
			string avatarId, string haircutId, int levelOfDetails = 0)
		{
			GameObject headObject = GetChildByName(avatarObject, headObjectName);
			GameObject hairObject = GetChildByName(avatarObject, haircutObjectName);

			if (headObject != null)
			{
				string meshFilePath = Path.Combine(prefabDir, "head.fbx");
				string textureFilePath = CoreTools.GetOutputTextureFilename(meshFilePath);
				SkinnedMeshRenderer headMeshRenderer = headObject.GetComponentInChildren<SkinnedMeshRenderer>();
				headMeshRenderer.sharedMesh = SaveHeadMeshAsFbxAsset(avatarId, meshFilePath, levelOfDetails);
				SaveMaterialTextures(headMeshRenderer.sharedMaterial, prefabDir, "head");
				headMeshRenderer.sharedMaterial = InstantiateAndSaveMaterial(headMeshRenderer.sharedMaterial, Path.Combine(prefabDir, "head_material.mat"));

				for (int i = 0; i < headMeshRenderer.sharedMesh.blendShapeCount; i++)
					headMeshRenderer.SetBlendShapeWeight(i, 0.0f);
			}

			if (hairObject != null && !string.IsNullOrEmpty(haircutId))
			{
				string haircutMeshFile = Path.Combine(prefabDir, "haircut.fbx");
				string haircutTextureFile = CoreTools.GetOutputTextureFilename(haircutMeshFile);
				MeshRenderer hairMeshRenderer = hairObject.GetComponentInChildren<MeshRenderer>();
				if (hairMeshRenderer != null)
				{
					hairObject.GetComponentInChildren<MeshFilter>().mesh = SaveHaircutMeshAsFbxAsset(avatarId, haircutId, haircutMeshFile);
					SaveMaterialTextures(hairMeshRenderer.sharedMaterial, prefabDir, "haircut");
					hairMeshRenderer.sharedMaterial = InstantiateAndSaveMaterial(hairMeshRenderer.sharedMaterial, Path.Combine(prefabDir, "haircut_material.mat"));
				}
				else
				{
					SkinnedMeshRenderer hairSkinnedMeshRenderer = hairObject.GetComponentInChildren<SkinnedMeshRenderer>();
					if (hairSkinnedMeshRenderer != null)
					{
						hairSkinnedMeshRenderer.sharedMesh = SaveHaircutMeshAsFbxAsset(avatarId, haircutId, haircutMeshFile);
						SaveMaterialTextures(hairSkinnedMeshRenderer.sharedMaterial, prefabDir, "haircut");
						hairSkinnedMeshRenderer.sharedMaterial = InstantiateAndSaveMaterial(hairSkinnedMeshRenderer.sharedMaterial, Path.Combine(prefabDir, "haircut_material.mat"));
					}
				}
			}

			AssetDatabase.SaveAssets();
		}

		protected Material InstantiateAndSaveMaterial(Material material, string assetPath)
		{
			Material instanceMat = GameObject.Instantiate(material);
			AssetDatabase.CreateAsset(instanceMat, assetPath);
			return instanceMat;
		}

		protected Texture2D LoadTextureAsset(string texturePath)
		{
			return (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));
		}

		protected Texture2D SaveTextureAsset(Texture2D texture, string texturePath, bool isNormalMap)
		{
			ImageUtils.ExportTexture(texture, texturePath);
			AssetDatabase.Refresh();

			TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(texturePath);
			textureImporter.isReadable = true;
			if (isNormalMap)
				textureImporter.textureType = TextureImporterType.NormalMap;
			textureImporter.SaveAndReimport();

			return LoadTextureAsset(texturePath);
		}

		protected void SaveMaterialTextures(Material material, string outputDir, string textureNamePrefix)
		{
			string[] texturesNames = material.GetTexturePropertyNames();
			foreach (string textureName in texturesNames)
			{
				Texture2D texture = material.GetTexture(textureName) as Texture2D;
				if (texture != null)
				{
					string textureAssetPath = AssetDatabase.GetAssetPath(texture);
					if (string.IsNullOrEmpty(textureAssetPath))
					{
						string extension = texture.format == TextureFormat.RGB24 ? "jpg" : "png";
						textureAssetPath = Path.Combine(outputDir, string.Format("{0}{1}.{2}", textureNamePrefix, textureName, extension));
						Texture2D savedTexture = SaveTextureAsset(texture, textureAssetPath, textureName.Contains("BumpMap"));
						material.SetTexture(textureName, savedTexture);
					}
				}
			}
		}

		protected Mesh SaveHeadMeshAsFbxAsset(string avatarId, string fbxPath, int levelOfDetails)
		{
			CoreTools.SaveAvatarMesh(null, avatarId, fbxPath, MeshFileFormat.FBX, false, true, levelOfDetails: levelOfDetails, saveTextures: false);
			AssetDatabase.Refresh();

			ModelImporter modelImporter = ModelImporter.GetAtPath(fbxPath) as ModelImporter;
			modelImporter.isReadable = true;
			modelImporter.SaveAndReimport();

			Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath(fbxPath, typeof(Mesh));
			return mesh;
		}

		protected Mesh SaveHaircutMeshAsFbxAsset(string avatarId, string haircutId, string fbxPath)
		{
			CoreTools.HaircutPlyToFbx(avatarId, haircutId, fbxPath, saveTextures: false);
			AssetDatabase.Refresh();

			ModelImporter modelImporter = ModelImporter.GetAtPath(fbxPath) as ModelImporter;
			modelImporter.isReadable = true;
			modelImporter.SaveAndReimport();

			Mesh mesh = (Mesh)AssetDatabase.LoadAssetAtPath(fbxPath, typeof(Mesh));
			return mesh;
		}

		protected GameObject GetChildByName (GameObject obj, string name)
		{
			var children = obj.GetComponentsInChildren<Transform> ();
			foreach (var child in children) {
				if (child.name.ToLower () == name.ToLower ())
					return child.gameObject;
			}

			return null;
		}

		protected void CopyBlendshapesWeights(GameObject srcAvatarObject, GameObject dstAvatarObject, string headObjectName)
		{
			GameObject srcAvatarHead = GetChildByName(srcAvatarObject, headObjectName);
			GameObject dstAvatarHead = GetChildByName(dstAvatarObject, headObjectName);

			SkinnedMeshRenderer srcMeshRenderer = srcAvatarHead.GetComponentInChildren<SkinnedMeshRenderer>();
			SkinnedMeshRenderer dstMeshRenderer = dstAvatarHead.GetComponentInChildren<SkinnedMeshRenderer>();

			for (int i = 0; i < dstMeshRenderer.sharedMesh.blendShapeCount; i++)
				dstMeshRenderer.SetBlendShapeWeight(i, 0.0f);

			for (int i=0; i<srcMeshRenderer.sharedMesh.blendShapeCount; i++)
			{
				string blendshapeName = srcMeshRenderer.sharedMesh.GetBlendShapeName(i);
				int idx = dstMeshRenderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
				if (idx >= 0)
				{
					dstMeshRenderer.SetBlendShapeWeight(idx, srcMeshRenderer.GetBlendShapeWeight(i));
				}
			}
		}
	}
}
#endif
