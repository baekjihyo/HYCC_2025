#region Namespaces

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Utilities;
using UnityEditor;
using MVC.Editor;
using Unity.Mathematics;

#endregion

namespace MVC.Demo.Editor
{
	[CustomEditor(typeof(PoliceAISiren))]
	public class PoliceAISirenEditor : ToolkitBehaviourEditor
	{
		#region Variables

		#region Static Variables

		private static bool sirensFoldout;
		private static bool redLightFoldout;
		private static bool blueLightFoldout;

		#endregion

		#region Global Variables

		private PoliceAISiren Instance
		{
			get
			{
				if (instance == null)
					instance = target as PoliceAISiren;

				return instance;
			}
		}
		private PoliceAISiren instance;

		#endregion

		#endregion

		#region Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Police AI Siren Configurations", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			EditorGUI.indentLevel++;

			EditorGUILayout.BeginVertical(GUI.skin.box);

			AudioClip[] newSirenClips = Instance.SirenClips;

			ArrayField(new GUIContent("Siren Clips", "The police siren clips list that will be looped through in play mode clip by clip and played"), ref newSirenClips, ref sirensFoldout);

			if (Instance.SirenClips != newSirenClips && Instance.SirenClips.Length == newSirenClips.Length)
				Instance.SirenClips = newSirenClips;

			if (sirensFoldout)
			{
				AudioMixerGroup newSirenClipsMixer = EditorGUILayout.ObjectField(new GUIContent("Sirens Mixer", "The police siren clips audio mixer group"), Instance.sirenClipsMixer, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (Instance.sirenClipsMixer != newSirenClipsMixer)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

					Instance.sirenClipsMixer = newSirenClipsMixer;

					EditorUtility.SetDirty(Instance);
				}

				if (!EditorApplication.isPlaying)
					EditorGUILayout.Space();
			}

			if (EditorApplication.isPlaying)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Current Siren", Instance.CurrentSirenClipIndex > -1 ? Instance.SirenClips[Instance.CurrentSirenClipIndex] : null, typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			MeshRenderer[] newBlueRenderers = Instance.BlueLightRenderers.Select(renderer => renderer.Renderer).ToArray();
			Color[] newBlueEmissionColors = Instance.BlueLightRenderers.Select(renderer => renderer.EmissionColor).ToArray();
			int[] newBlueMaterialIndexes = Instance.BlueLightRenderers.Select(renderer => renderer.MaterialIndex).ToArray();

			if (ArrayField(new GUIContent("Blue Light Renderers", "The police blue light mesh renderers list"), ref newBlueRenderers, ref newBlueMaterialIndexes, ref newBlueEmissionColors, ref blueLightFoldout))
			{
				Instance.BlueLightRenderers = new PoliceAISiren.LightRenderer[newBlueRenderers.Length];

				for (int i = 0; i < newBlueRenderers.Length; i++)
					Instance.BlueLightRenderers[i] = new PoliceAISiren.LightRenderer(Instance.Base, newBlueRenderers[i], newBlueMaterialIndexes[i], newBlueEmissionColors[i]);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			MeshRenderer[] newRedRenderers = Instance.RedLightRenderers.Select(renderer => renderer.Renderer).ToArray();
			Color[] newRedEmissionColors = Instance.RedLightRenderers.Select(renderer => renderer.EmissionColor).ToArray();
			int[] newRedMaterialIndexes = Instance.RedLightRenderers.Select(renderer => renderer.MaterialIndex).ToArray();

			if (ArrayField(new GUIContent("Red Light Renderers", "The police red light mesh renderers list"), ref newRedRenderers, ref newRedMaterialIndexes, ref newRedEmissionColors, ref redLightFoldout))
			{
				Instance.RedLightRenderers = new PoliceAISiren.LightRenderer[newRedRenderers.Length];

				for (int i = 0; i < newRedRenderers.Length; i++)
					Instance.RedLightRenderers[i] = new PoliceAISiren.LightRenderer(Instance.Base, newRedRenderers[i], newRedMaterialIndexes[i], newRedEmissionColors[i]);
			}

			EditorGUILayout.EndVertical();

			EditorGUI.indentLevel--;

			if (Instance.SirenClips.Length < 1)
				EditorGUILayout.HelpBox("Please add at least one police siren clip to the list above.", MessageType.Warning);

			EditorGUILayout.Space();
		}

		private bool ArrayField(GUIContent label, ref MeshRenderer[] renderers, ref int[] materialIndexes, ref Color[] emissionColors, ref bool foldout)
		{
			foldout = EditorGUILayout.Foldout(foldout, label, true);

			if (foldout)
			{
				EditorGUI.indentLevel++;

				int newLength = EditorGUILayout.IntField("Length", renderers.Length);

				if (renderers.Length != newLength)
				{
					newLength = math.max(newLength, 0);

					Undo.RegisterCompleteObjectUndo(Instance, "Change Length");
					Array.Resize(ref renderers, newLength);
					Array.Resize(ref materialIndexes, newLength);
					Array.Resize(ref emissionColors, newLength);
					EditorUtility.SetDirty(Instance);
				}

				for (int i = 0; i < renderers.Length; i++)
				{
					MeshRenderer newLightRenderer = EditorGUILayout.ObjectField($"Renderer {i}", renderers[i], typeof(MeshRenderer), true) as MeshRenderer;

					if (renderers[i] != newLightRenderer)
					{
						Undo.RegisterCompleteObjectUndo(Instance, $"Change Renderer");

						renderers[i] = newLightRenderer;

						EditorUtility.SetDirty(Instance);
					}

					if (renderers[i])
					{
						EditorGUI.indentLevel++;

						int newLightMaterialIndex = EditorGUILayout.Popup("Material", materialIndexes[i], renderers[i].sharedMaterials.Select((material, i) => $"{i + 1}. {material.name}").ToArray());

						if (materialIndexes[i] != newLightMaterialIndex)
						{
							Undo.RegisterCompleteObjectUndo(Instance, $"Change Material");

							materialIndexes[i] = newLightMaterialIndex;

							EditorUtility.SetDirty(Instance);
						}

						Color newLightEmissionColor = EditorGUILayout.ColorField(new GUIContent("Emission Color"), emissionColors[i], false, false, true);

						if (emissionColors[i] != newLightEmissionColor)
						{
							Undo.RegisterCompleteObjectUndo(Instance, $"Change Emission");

							emissionColors[i] = newLightEmissionColor;

							EditorUtility.SetDirty(Instance);
						}

						EditorGUI.indentLevel--;
					}
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
			}

			return EditorUtility.IsDirty(Instance);
		}
		private void ArrayField<T>(GUIContent label, ref T[] array, ref bool foldout) where T : UnityEngine.Object
		{
			foldout = EditorGUILayout.Foldout(foldout, label, true);

			if (foldout)
			{
				EditorGUI.indentLevel++;

				int newLength = EditorGUILayout.IntField("Length", array.Length);

				if (array.Length != newLength)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Length");
					Array.Resize(ref array, newLength);
					EditorUtility.SetDirty(Instance);
				}

				for (int i = 0; i < array.Length; i++)
				{
					T newElement = EditorGUILayout.ObjectField($"{typeof(T).Name} {i}", array[i], typeof(T), true) as T;

					if (array[i] != newElement)
					{
						Undo.RegisterCompleteObjectUndo(Instance, $"Change {typeof(T).Name}");

						array[i] = newElement;

						EditorUtility.SetDirty(Instance);
					}
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
			}
		}

		#endregion
	}
}
