using GTA;
using SharpDX.DirectInput;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Benjamin94.Input
{
	internal class ControllerWizard
	{
		private readonly DeviceButton[] dpads = new DeviceButton[] { typeof(<PrivateImplementationDetails>).GetField("02E4414E7DFA0F3AA2387EE8EA7AB31431CB406A").FieldHandle };

		private Joystick stick;

		public ControllerWizard(Joystick stick)
		{
			this.stick = stick;
		}

		private bool Configure(DeviceButton btn, ScriptSettings data, DirectInputManager input, string guid)
		{
			bool flag;
			while (true)
			{
				if (input.GetPressedButton() != -1)
				{
					int pressedButton = input.GetPressedButton();
					UI.ShowSubtitle(string.Concat("Please hold the ", this.GetBtnText(btn), " button to confirm it."));
					Script.Wait(1000);
					if (pressedButton == input.GetPressedButton())
					{
						data.SetValue<int>(guid, btn.ToString(), pressedButton);
						while (input.GetPressedButton() != -1)
						{
							UI.ShowSubtitle("Now let go the button to configure the next one.");
							Script.Wait(100);
						}
						Script.Wait(1000);
					}
					else
					{
						UI.ShowSubtitle(string.Concat("Now hold the ", this.GetBtnText(btn), " button to confirm."));
						Script.Wait(1000);
						this.Configure(btn, data, input, guid);
					}
					flag = true;
					break;
				}
				else if (!Game.IsKeyPressed(Keys.Escape))
				{
					UI.ShowSubtitle(string.Concat("Press and hold the ", this.GetBtnText(btn), " button on the controller for 1 second. Press the Esc key to cancel."), 120);
					Script.Wait(100);
				}
				else
				{
					flag = false;
					break;
				}
			}
			return flag;
		}

		private bool ConfigureDigitalDpadButton(DeviceButton btn, ScriptSettings data, DirectInputManager input, string guid)
		{
			bool flag;
			while (true)
			{
				if (input.GetDpadValue() != -1)
				{
					int dpadValue = input.GetDpadValue();
					UI.ShowSubtitle(string.Concat("Please hold the ", this.GetBtnText(btn), " button to confirm it."));
					Script.Wait(1000);
					if (dpadValue != -1)
					{
						data.SetValue<int>(guid, btn.ToString(), dpadValue);
						while (input.GetDpadValue() != -1)
						{
							UI.ShowSubtitle("Now let go the button to configure the next one.");
							Script.Wait(100);
						}
						Script.Wait(1000);
					}
					else
					{
						UI.ShowSubtitle(string.Concat("Now hold the ", this.GetBtnText(btn), " button to confirm."));
						Script.Wait(1000);
						this.ConfigureDigitalDpadButton(btn, data, input, guid);
					}
					flag = true;
					break;
				}
				else if (!Game.IsKeyPressed(Keys.Escape))
				{
					UI.ShowSubtitle(string.Concat("Press and hold the ", this.GetBtnText(btn), " button on the controller for 1 second. Press the Esc key to cancel."), 120);
					Script.Wait(100);
				}
				else
				{
					flag = false;
					break;
				}
			}
			return flag;
		}

		private DpadType DetermineDpadType(DirectInputManager input)
		{
			DpadType dpadType;
			while (true)
			{
				if (!(input.GetPressedButton() != -1 ? false : input.GetDpadValue() == -1))
				{
					UI.ShowSubtitle("Now keep holding that Dpad button.");
					Script.Wait(1000);
					int pressedButton = input.GetPressedButton();
					if (input.GetDpadValue() != -1)
					{
						dpadType = DpadType.DigitalDpad;
						break;
					}
					else if (pressedButton == -1)
					{
						dpadType = DpadType.Unknown;
						break;
					}
					else
					{
						dpadType = DpadType.ButtonsDpad;
						break;
					}
				}
				else if (!Game.IsKeyPressed(Keys.Escape))
				{
					UI.ShowSubtitle("Press and hold at least one Dpad button for 1 second. Press the Esc key to cancel.", 120);
					Script.Wait(100);
				}
				else
				{
					dpadType = DpadType.DigitalDpad | DpadType.Unknown;
					break;
				}
			}
			return dpadType;
		}

		private string GetBtnText(DeviceButton btn)
		{
			return string.Concat("~g~'", btn, "'~w~");
		}

		public bool StartConfiguration(string iniFile)
		{
			bool flag;
			DirectInputManager directInputManager = new DirectInputManager(this.stick);
			ScriptSettings scriptSetting = ScriptSettings.Load(iniFile);
			string str = this.stick.get_Information().ProductGuid.ToString();
			DpadType dpadType = this.DetermineDpadType(directInputManager);
			if (dpadType == (DpadType.DigitalDpad | DpadType.Unknown))
			{
				flag = false;
			}
			else if (dpadType != DpadType.Unknown)
			{
				scriptSetting.SetValue(str, "DpadType", dpadType.ToString());
				while (directInputManager.GetDpadValue() != -1)
				{
					UI.ShowSubtitle("Please let go the Dpad button.");
					Script.Wait(100);
				}
				Script.Wait(1000);
				UI.ShowSubtitle(string.Concat("Determined Dpad type: ", dpadType), 2500);
				Script.Wait(2500);
				foreach (DeviceButton value in Enum.GetValues(typeof(DeviceButton)))
				{
					if ((!Array.Exists<DeviceButton>(this.dpads, (DeviceButton item) => item == value) ? false : dpadType == DpadType.DigitalDpad))
					{
						if (!this.ConfigureDigitalDpadButton(value, scriptSetting, directInputManager, str))
						{
							flag = false;
							return flag;
						}
					}
					else if (!this.Configure(value, scriptSetting, directInputManager, str))
					{
						flag = false;
						return flag;
					}
					UI.Notify(string.Concat(this.GetBtnText(value), " button configured."));
				}
				scriptSetting.Save();
				flag = true;
			}
			else
			{
				UI.Notify("Unknown Dpad type, controller configuration stopped.");
				flag = false;
			}
			return flag;
		}
	}
}