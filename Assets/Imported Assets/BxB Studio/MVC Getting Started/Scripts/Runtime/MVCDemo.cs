#region Namespaces

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities.Inputs;
using TMPro;
using MVC.Core;
using MVC.Utilities;
using MVC.Utilities.Internal;

#endregion

namespace MVC.Demo
{
	[DefaultExecutionOrder(10000)]
	public class MVCDemo : ToolkitBehaviour
	{
		#region Variables

		[Tooltip("The UI text to display tutorial")]
		public TextMeshProUGUI tutorialText;
		[Tooltip("Player vehicle index at startup")]
		public int playerVehicleIndexAtStart;
		[Tooltip("Set AI as player if no vehicles are available")]
		public bool followAI = true;

		// List of active non-AI vehicles
		private Vehicle[] activeVehicles;
		// Player index change flag
		private bool playerChanged;
		// Count of active AI or non-AI vehicles
		private int vehiclesCount;
		// Current player vehicle index
		private int playerVehicleIndex;

		#endregion

		#region Methods

		private void Start()
		{
			// Disable auto Player vehicle refresh if set to 'true'
			if (Manager.autoRefreshPlayer)
				Manager.autoRefreshPlayer = false;

			// Find the active non-AI vehicles
			activeVehicles = Manager.ActiveVehicles.Where(vehicle => !vehicle.IsAI).ToArray();
			// Get the active vehicles count; If 'activeVehicles' array is empty, then get the AI vehicles count
			vehiclesCount = activeVehicles.Length > 0 ? activeVehicles.Length : followAI ? Manager.AIVehicles.Length : default;
			// Clamp the player vehicle index to the vehicles count
			playerVehicleIndex = Mathf.Clamp(playerVehicleIndexAtStart, 0, vehiclesCount);

			// Construct tutorial log string
			string consoleMessage = "The MVC Demo has started! <b><i>Click here for tutorial...</i></b>\r\n\r\nKeys:\r\n";
			string tutorial = $"{consoleMessage}";

			tutorial += "Change vehicle: V or B\r\n";

			// If using InputsManager, then print keys; Else, Unity's legacy input system doesn't allow us to get keys & input parameters per default
			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager)
			{
				// Checking for input existing before getting its parameters
				if (InputsManager.IndexOf(Settings.engineStartSwitchInput) > -1)
					tutorial += $"Turn engine on/off: {InputsManager.GetInput(Settings.engineStartSwitchInput).Main.Positive}\r\n";

				if (InputsManager.IndexOf(Settings.steerInput) > -1)
					tutorial += $"Steer left & right: {InputsManager.GetInput(Settings.steerInput).Main.Negative} & {InputsManager.GetInput(Settings.steerInput).Main.Positive}\r\n";

				if (InputsManager.IndexOf(Settings.fuelInput) > -1)
					tutorial += $"Fuel pedal: {InputsManager.GetInput(Settings.fuelInput).Main.Positive}\r\n";

				if (InputsManager.IndexOf(Settings.brakeInput) > -1)
					tutorial += $"Brake pedal: {InputsManager.GetInput(Settings.brakeInput).Main.Positive}\r\n";

				if (InputsManager.IndexOf(Settings.handbrakeInput) > -1)
					tutorial += $"Handbrake: {InputsManager.GetInput(Settings.handbrakeInput).Main.Positive}\r\n";

				if (InputsManager.IndexOf(Settings.clutchInput) > -1)
					tutorial += $"Clutch pedal: {InputsManager.GetInput(Settings.clutchInput).Main.Positive}\r\n";

				if (InputsManager.IndexOf(Settings.changeCameraButtonInput) > -1)
					tutorial += $"Change camera: {InputsManager.GetInput(Settings.changeCameraButtonInput).Main.Positive}\r\n";

				// If transmission shifting is manual, then print gear shifting keys
				if (Settings.transmissionType == ToolkitSettings.TransmissionType.Manual)
				{
					if (InputsManager.IndexOf(Settings.changeCameraButtonInput) > -1)
						tutorial += $"Gear shift up: {InputsManager.GetInput(Settings.gearShiftUpButtonInput).Main.Positive}\r\n";

					if (InputsManager.IndexOf(Settings.changeCameraButtonInput) > -1)
						tutorial += $"Gear shift down: {InputsManager.GetInput(Settings.gearShiftDownButtonInput).Main.Positive}\r\n";
				}

				if (tutorialText)
				{
					tutorialText.text = tutorial.Replace(consoleMessage, "");
					tutorialText.text += "Hide/Show tutorial: Escape";
				}

				tutorial += $"More keys & input definitions available on the MVC Settings panel (Tools > Multiversal Vehicle Controller > Edit Settings... > Player Inputs).\r\n";
			}
			else if (tutorialText)
			{
				tutorialText.text = tutorial.Replace(consoleMessage, "");
				tutorialText.text += "Hide/Show tutorial: Escape";
			}

			// Print tutorial on console log
			ToolkitDebug.Log(tutorial, false);
		}
		private void Update()
		{
			// Checking if the MVC is all set-up and if there are active vehicles on the scene; Else, exit execution
			if (HasInternalErrors || !IsSetupDone || vehiclesCount < 2 || activeVehicles.Length < 1 && (!followAI || Manager.AIVehicles.Length < 1))
				return;

			// Check if 'V' key is pressed
			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager && InputsManager.InputKeyDown(Key.V) ||
				Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager && UnityEngine.Input.GetKeyDown(KeyCode.V))
			{
				// Increase the vehicle player index by 1
				playerVehicleIndex++;
				// Set the player change flag to 'true'
				playerChanged = true;
			}

			// Check if 'B' key is pressed
			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager && InputsManager.InputKeyDown(Key.B) ||
				Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager && UnityEngine.Input.GetKeyDown(KeyCode.B))
			{
				// Decrease the vehicle player index by 1
				playerVehicleIndex--;
				// Set the player change flag to 'true'
				playerChanged = true;
			}

			// Check if 'Escape' key is pressed
			if (tutorialText && (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager && InputsManager.InputKeyDown(Key.Escape) ||
				Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager && UnityEngine.Input.GetKeyDown(KeyCode.Escape)))
				// Activate/Deactivate the tutorial UI text
				tutorialText.gameObject.SetActive(!tutorialText.gameObject.activeSelf);

			// Check if any changes to the player index have been made
			if (playerChanged)
			{
				// Check if the player index is greater than the vehicles count or smaller than zero & then clamp it to prevent getting IndexOutOfRange exceptions
				while (playerVehicleIndex >= vehiclesCount)
					playerVehicleIndex -= vehiclesCount;

				while (playerVehicleIndex < 0)
					playerVehicleIndex += vehiclesCount;

				// Assign the player vehicle to the manager
				// If 'activeVehicles' is empty, assign a new AI vehicle
				Manager.PlayerTarget = activeVehicles.Length > 0 ? activeVehicles[playerVehicleIndex] : Manager.AIVehicles[playerVehicleIndex];

				// Refresh the manager current player
				Manager.RefreshPlayer();

				// Set the player change flag to 'false' to prevent this if statement from getting executed without any changes happening
				playerChanged = false;
			}
		}

		#endregion
	}
}
