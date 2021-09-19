/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, May 2021
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItSeez3D.AvatarSdkSamples.Core
{
	/// <summary>
	/// Helper class to deal with humanoid animations.
	/// </summary>
	public class FullbodyAnimationManager : AnimationManager
	{
		private Avatar animatorAvatar = null;
		private Avatar animatorAvatarOnHeels = null;

		private readonly List<string> outfitsWithHeels = new List<string>() { "outfit_0", "outfit_2", "outfit_0_lowpoly", "outfit_2_lowpoly" };

		/// <summary>
		/// Creates humanoid animator for the fullbody avatar
		/// </summary>
		/// <param name="obj">Fullbody avatar object</param>
		/// <param name="meshRenderer">Body skinned mesh</param>
		public void CreateHumanoidAnimator(GameObject obj, SkinnedMeshRenderer meshRenderer, BodyAppearanceController bodyAppearanceController)
		{
			animator = obj.AddComponent<Animator>();
			animator.applyRootMotion = true;
			animator.runtimeAnimatorController = animatorController;
			animatorAvatar = AvatarBuilder.BuildHumanAvatar(obj, BuildHumanDescription(meshRenderer, false));
			animatorAvatarOnHeels = AvatarBuilder.BuildHumanAvatar(obj, BuildHumanDescription(meshRenderer, true));

			if (bodyAppearanceController != null)
			{
				bodyAppearanceController.activeOutfitChanged += OnActiveOutfitChanged;
				OnActiveOutfitChanged(bodyAppearanceController.ActiveOutfitName);
			}
			else
				animator.avatar = animatorAvatar;

			currentAnimationIdx = 0;
			if (currentAnimationText != null)
				currentAnimationText.text = animations[currentAnimationIdx].Replace('_', ' ');
		}

		private HumanDescription BuildHumanDescription(SkinnedMeshRenderer meshRenderer, bool modelOnHeels)
		{
			HumanDescription description = new HumanDescription();
			description.armStretch = 0.05f;
			description.legStretch = 0.05f;
			description.upperArmTwist = 0.5f;
			description.lowerArmTwist = 0.5f;
			description.upperLegTwist = 0.5f;
			description.lowerLegTwist = 0.5f;
			description.feetSpacing = 0;

			List<HumanBone> humanBones = new List<HumanBone>();
			TextAsset humanBonesContent = Resources.Load<TextAsset>("human_bones");
			string[] lines = humanBonesContent.text.Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++)
			{
				string[] names = lines[i].Split(',');
				humanBones.Add(new HumanBone() { boneName = names[0], humanName = names[1], limit = new HumanLimit() { useDefaultValues = true } });
			}
			description.human = humanBones.ToArray();

			List<Transform> bones = meshRenderer.bones.ToList();
			Matrix4x4[] bindPoses = meshRenderer.sharedMesh.bindposes;
			List<SkeletonBone> skeletonBones = new List<SkeletonBone>();
			for (int i = 0; i < bones.Count; i++)
			{
				Matrix4x4 boneLocalPosition = bindPoses[i].inverse;
				int parentIdx = bones.FindIndex(b => b.name == bones[i].parent.name);
				if (parentIdx > 0)
					boneLocalPosition = boneLocalPosition * bindPoses[parentIdx];

				SkeletonBone bone = new SkeletonBone()
				{
					name = bones[i].name,
					position = boneLocalPosition.GetPosition(),
					rotation = boneLocalPosition.GetRotation(),
					scale = boneLocalPosition.GetScale()
				};

				if (modelOnHeels)
				{
					if (bone.name == "L_Ankle" || bone.name == "R_Ankle")
						bone.rotation = Quaternion.Euler(20.0f, 0.0f, 0.0f);

					if (bone.name == "L_Foot" || bone.name == "R_Foot")
						bone.rotation = Quaternion.Euler(-20.0f, 0.0f, 0.0f);
				}

				skeletonBones.Add(bone);
			}
			description.skeleton = skeletonBones.ToArray();

			return description;
		}

		private void OnActiveOutfitChanged(string activeOutfitName)
		{
			if (outfitsWithHeels.Contains(activeOutfitName))
				animator.avatar = animatorAvatarOnHeels;
			else
				animator.avatar = animatorAvatar;
		}
	}
}
