#region Namespaces

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using Utilities;
using Utilities.Inputs;
using MVC.Core;
using MVC.Utilities;
using MVC.Utilities.Internal;

#endregion

namespace MVC.Demo
{
	public class PoliceAISiren : ToolkitBehaviourExtension<Vehicle>
	{
		#region Modules

		[Serializable]
		public class LightRenderer
		{
			#region Variables

			public MeshRenderer Renderer
			{
				get
				{
					return renderer;
				}
				set
				{
					if (value && !value.transform.IsChildOf(vehicle.transform))
					{
						ToolkitDebug.Warning("Cannot assign new the Mesh Renderer to this vehicle as it's not a part of its body or wheel models.", vehicle.gameObject, false);

						return;
					}

					renderer = value;
				}
			}
			public int MaterialIndex
			{
				get
				{
					if (!Renderer)
						return -1;

					return math.max(materialIndex, 0);
				}
				set
				{
					if (!Renderer)
						return;

					materialIndex = math.clamp(value, 0, Renderer.sharedMaterials.Length);
				}
			}
			public Color EmissionColor
			{
				get
				{
					return emissionColor;
				}
				set
				{
					emissionColor = value;
				}
			}

			[SerializeField]
			private MeshRenderer renderer;
			[SerializeField]
			private int materialIndex;
			[SerializeField]
			private Color emissionColor;
			[SerializeField]
			private readonly Vehicle vehicle;

			#endregion

			#region Constructors

			public LightRenderer(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}
			public LightRenderer(Vehicle vehicle, MeshRenderer renderer, int materialIndex, Color emissionColor) : this(vehicle)
			{
				this.renderer = renderer;
				this.materialIndex = materialIndex;
				this.emissionColor = emissionColor;
			}
			public LightRenderer(Vehicle vehicle, LightRenderer renderer) : this(vehicle, renderer.Renderer, renderer.MaterialIndex, renderer.EmissionColor) { }

			#endregion
		}

		#endregion

		#region Variables

		#region Fields

		public AudioMixerGroup sirenClipsMixer;
		public AudioClip[] SirenClips
		{
			get
			{
				sirenClips ??= new AudioClip[] { };

				return sirenClips;
			}
			set
			{
				sirenClips = value;
			}
		}
		public LightRenderer[] BlueLightRenderers
		{
			get
			{
				blueLightRenderers ??= new LightRenderer[] { };

				return blueLightRenderers;
			}
			set
			{
				blueLightRenderers = value;
			}
		}
		public LightRenderer[] RedLightRenderers
		{
			get
			{
				redLightRenderers ??= new LightRenderer[] { };

				return redLightRenderers;
			}
			set
			{
				redLightRenderers = value;
			}
		}


		[SerializeField]
		private AudioClip[] sirenClips;
		[SerializeField]
		private LightRenderer[] blueLightRenderers;
		[SerializeField]
		private LightRenderer[] redLightRenderers;
		private readonly Key sirenKey = Key.H;
		private readonly double sirenPeriod = .5d;

		#endregion

		#region Temp & Stats

		public int CurrentSirenClipIndex
		{
			get
			{
				return currentSirenClipIndex;
			}
		}

		private VehicleAudioSource[] sirenSources;
		private VehicleAudioSource currentSource;
		private double timeDecimals;
		private bool awaken;
		private bool blueLightsOn;
		private bool redLightsOn;
		private bool lastBlueLightsOn;
		private bool lastRedLightsOn;
		private bool forceSirenOn;
		private bool isSirenOn;
		private int currentSirenClipIndex;

		#endregion

		#endregion

		#region Awake

		public void Restart()
		{
			awaken = false;

			Initialize();

			if (!Base || !Base.IsAI || !Base.ActiveAIBehaviour)
			{
				ToolkitDebug.Error($"The `Police AI` component has to have a `Vehicle` and/or a `Vehicle AI` component attached to the same GameObject!", gameObject, false);

				return;
			}
			else if (SirenClips.Length < 1)
			{
				ToolkitDebug.Warning($"We couldn't start the `Police AI` component for \"{name}\" because the `Siren Clips` list is empty!", gameObject, false);

				return;
			}

			sirenSources = VehicleAudio.NewAudioSources(SirenClips, Base, Base.Bounds.center + Vector3.up * Base.Bounds.extents.y, "siren", 50f, 150f, 1f, false, false, true, default, sirenClipsMixer);
			awaken = true;
		}

		private void Initialize()
		{
			currentSource = null;

			for (int i = 0; i < sirenSources?.Length; i++)
				Utility.Destroy(true, sirenSources[i]);

			sirenSources = null;
			currentSirenClipIndex = -1;
		}
		private void Awake()
		{
			Restart();
		}

		#endregion

		#region Update

		private void Update()
		{
			if (!awaken)
				return;

			Calculations();
			UpdateAudio();
			UpdateLightRenderers(BlueLightRenderers, blueLightsOn, ref lastBlueLightsOn);
			UpdateLightRenderers(RedLightRenderers, redLightsOn, ref lastRedLightsOn);
		}
		private void Calculations()
		{
			if (InputsManager.InputKeyDown(sirenKey))
				forceSirenOn = !forceSirenOn;

			currentSource = currentSirenClipIndex > -1 ? sirenSources[currentSirenClipIndex] : null;
			timeDecimals = Time.timeAsDouble;
			timeDecimals /= sirenPeriod != 0d ? sirenPeriod : 1d;
			timeDecimals -= Math.Truncate(timeDecimals);
			isSirenOn = /*!Base.AI && */forceSirenOn/* || Base.AI && !Base.AI.IsChaseTargetLost*/;
			blueLightsOn = isSirenOn && (timeDecimals < .125d || timeDecimals >= .25d && timeDecimals < .375d);
			redLightsOn = isSirenOn && timeDecimals >= .5d && (timeDecimals < .625d || timeDecimals >= .75d && timeDecimals < .875d);
		}
		private void UpdateAudio()
		{
			if (!isSirenOn)
			{
				if (currentSource && currentSource.source.isPlaying)
					currentSource.PauseAndDisable();

				return;
			}
			else if (currentSource && currentSource.source.isPlaying)
				return;
			else if (currentSource && currentSource.source.time > 0 && currentSource.source.time < currentSource.source.clip.length)
			{
				currentSource.UnPauseAndEnable();

				return;
			}

			currentSirenClipIndex++;

			if (currentSirenClipIndex >= sirenSources.Length)
				currentSirenClipIndex = 0;

			currentSource = sirenSources[currentSirenClipIndex];

			currentSource.PlayAndEnable();
		}
		private void UpdateLightRenderers(LightRenderer[] renderers, bool state, ref bool oldState)
		{
			if (state == oldState)
				return;

			foreach (LightRenderer renderer in renderers)
			{
				Material material = renderer.Renderer.materials[renderer.MaterialIndex];
				string emissionColorPropertyName = default;

				if (material.shader.name.StartsWith("HDRP/Lit"))
					emissionColorPropertyName = "_EmissiveColor";
				else if (material.shader.name.StartsWith("Universal Render Pipeline/Lit"))
					emissionColorPropertyName = "_EmissionColor";
				else if (material.shader.name.StartsWith("Standard"))
					emissionColorPropertyName = "_EmissionColor";
				else if (material.shader.name.StartsWith("Particles/Standard"))
					emissionColorPropertyName = "_Color";
				else if (!HasInternalErrors)
					emissionColorPropertyName = Settings.customLightShaderEmissionColorProperty;

				if (emissionColorPropertyName.IsNullOrEmpty())
					continue;

				material.SetColor(emissionColorPropertyName, state ? renderer.EmissionColor : Color.black);
			}

			oldState = state;
		}

		#endregion
	}
}
