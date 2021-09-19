/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2021
*/

using System.Linq;
using ItSeez3D.AvatarSdk.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItSeez3D.AvatarSdkSamples.Core
{
	public static class FullbodySamplesRenderingSettings
	{
		public static void Setup()
		{
			var renderingPipeline = RenderingPipelineTraits.GetRenderingPipeLine();
			if (renderingPipeline == RenderingPipelineTraits.RenderingPipeline.URP || renderingPipeline == RenderingPipelineTraits.RenderingPipeline.LWRP)
			{
				UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
				var directionalLightObj = activeScene.GetRootGameObjects().FirstOrDefault(o => o.GetComponent<Light>() != null && o.GetComponent<Light>().type == LightType.Directional);
				if (directionalLightObj == null)
					return;

				Light directionalLight = directionalLightObj.GetComponent<Light>();
				if (directionalLight == null)
					return;
				directionalLight.intensity = 1.0f;

				RenderPipelineAsset renderPipelineAsset = Resources.Load<RenderPipelineAsset>("urp_quality_settings/avatar_sdk_urp_settings");
				if (renderPipelineAsset != null)
					QualitySettings.renderPipeline = renderPipelineAsset;
				else
					Debug.LogError("Unable to find settings for URP");
			}
			else
			{
				QualitySettings.shadows = ShadowQuality.All;
				QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
				QualitySettings.shadowDistance = 5.0f;
				QualitySettings.antiAliasing = 8;
			}
			QualitySettings.skinWeights = SkinWeights.FourBones;
		}
	}
}
