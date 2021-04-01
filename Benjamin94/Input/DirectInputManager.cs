using GTA;
using GTA.Math;
using SharpDX.DirectInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Benjamin94.Input
{
	internal class DirectInputManager : InputManager
	{
		public const string DpadTypeKey = "DpadType";

		public Joystick device;

		private List<Tuple<int, DeviceButton>> config;

		private const int JOY_32767 = 32767;

		private const int JOY_0 = 0;

		private const int JOY_65535 = 65535;

		public override string DeviceGuid
		{
			get
			{
				return this.device.get_Information().ProductGuid.ToString();
			}
		}

		public override string DeviceName
		{
			get
			{
				return this.device.get_Information().ProductName;
			}
		}

		public DirectInputManager(Joystick device)
		{
			this.device = device;
			if (device != null)
			{
				device.Acquire();
			}
			this.config = new List<Tuple<int, DeviceButton>>();
			DeviceState state = this.GetState();
			this.X_CENTER_L = state.LeftThumbStick.X;
			this.Y_CENTER_L = state.LeftThumbStick.Y;
			this.X_CENTER_R = state.RightThumbStick.X;
			this.Y_CENTER_R = state.RightThumbStick.Y;
		}

		public override void Cleanup()
		{
			this.device.Unacquire();
			this.device = null;
			this.config = null;
		}

		public static List<Joystick> GetDevices()
		{
			DirectInput directInput = new DirectInput();
			List<Joystick> joysticks = new List<Joystick>();
			foreach (DeviceInstance device in directInput.GetDevices(4, 1))
			{
				Joystick joystick = new Joystick(directInput, device.InstanceGuid);
				string lower = joystick.get_Information().ProductName.ToLower();
				if ((lower.Contains("xbox") || lower.Contains("360") ? false : !lower.Contains("xinput")))
				{
					joysticks.Add(joystick);
				}
			}
			return joysticks;
		}

		protected override Direction GetDirection(float X, float Y, float xCenter, float yCenter)
		{
			Direction direction;
			if ((X >= xCenter ? false : Y > yCenter))
			{
				direction = Direction.ForwardLeft;
			}
			else if ((X <= xCenter ? false : Y > yCenter))
			{
				direction = Direction.ForwardRight;
			}
			else if ((X >= xCenter ? false : Y == yCenter))
			{
				direction = Direction.Left;
			}
			else if ((X != xCenter ? false : Y > yCenter))
			{
				direction = Direction.Forward;
			}
			else if ((X != xCenter ? false : Y < yCenter))
			{
				direction = Direction.Backward;
			}
			else if ((X >= xCenter ? false : Y < yCenter))
			{
				direction = Direction.BackwardLeft;
			}
			else if ((X <= xCenter ? true : Y >= yCenter))
			{
				direction = ((X <= xCenter ? true : Y != yCenter) ? Direction.None : Direction.Right);
			}
			else
			{
				direction = Direction.BackwardRight;
			}
			return direction;
		}

		public int GetDpadValue()
		{
			int num;
			try
			{
				this.device.Poll();
				this.device.GetCurrentState();
				int[] pointOfViewControllers = this.device.GetCurrentState().get_PointOfViewControllers();
				int num1 = 0;
				while (num1 < (int)pointOfViewControllers.Length)
				{
					int num2 = pointOfViewControllers[num1];
					if (num2 == -1)
					{
						num1++;
					}
					else
					{
						num = num2;
						return num;
					}
				}
			}
			catch (Exception exception)
			{
			}
			num = -1;
			return num;
		}

		public int GetPressedButton()
		{
			int num;
			try
			{
				JoystickState currentState = this.device.GetCurrentState();
				if (currentState != null)
				{
					bool[] buttons = currentState.get_Buttons();
					int num1 = 0;
					while (num1 < (int)buttons.Length)
					{
						int num2 = num1 + 1;
						if (!buttons[num1])
						{
							num1++;
						}
						else
						{
							num = num2;
							return num;
						}
					}
				}
				else
				{
					num = -1;
					return num;
				}
			}
			catch (Exception exception)
			{
			}
			num = -1;
			return num;
		}

		public override DeviceState GetState()
		{
			DeviceState deviceState;
			try
			{
				this.device.Poll();
				JoystickState currentState = this.device.GetCurrentState();
				DeviceState deviceState1 = new DeviceState();
				bool[] buttons = currentState.get_Buttons();
				for (int i = 0; i < (int)buttons.Length; i++)
				{
					if (buttons[i])
					{
						Tuple<int, DeviceButton> tuple = this.config.FirstOrDefault<Tuple<int, DeviceButton>>((Tuple<int, DeviceButton> item) => item.Item1 == i + 1);
						if (tuple != null)
						{
							deviceState1.Buttons.Add(tuple.Item2);
						}
					}
				}
				int[] pointOfViewControllers = currentState.get_PointOfViewControllers();
				for (int j = 0; j < (int)pointOfViewControllers.Length; j++)
				{
					int num = pointOfViewControllers[j];
					Tuple<int, DeviceButton> tuple1 = this.config.FirstOrDefault<Tuple<int, DeviceButton>>((Tuple<int, DeviceButton> item) => item.Item1 == num);
					if (tuple1 != null)
					{
						deviceState1.Buttons.Add(tuple1.Item2);
					}
				}
				deviceState1.LeftThumbStick = DirectInputManager.NormalizeThumbStick((float)currentState.get_X(), (float)currentState.get_Y());
				deviceState1.RightThumbStick = DirectInputManager.NormalizeThumbStick((float)currentState.get_Z(), (float)currentState.get_RotationZ());
				deviceState = deviceState1;
				return deviceState;
			}
			catch (Exception exception1)
			{
				try
				{
					this.device.Acquire();
				}
				catch (Exception exception)
				{
				}
			}
			deviceState = null;
			return deviceState;
		}

		public static bool IsConfigured(Joystick stick, string file)
		{
			bool flag;
			try
			{
				DirectInputManager.LoadConfig(stick, file);
				flag = true;
			}
			catch (Exception exception)
			{
				flag = false;
			}
			return flag;
		}

		public static DirectInputManager LoadConfig(Joystick stick, string file)
		{
			DirectInputManager directInputManager = new DirectInputManager(stick);
			try
			{
				string str = stick.get_Information().ProductGuid.ToString();
				ScriptSettings scriptSetting = ScriptSettings.Load(file);
				DpadType dpadType = DpadType.Unknown;
				try
				{
					DpadType dpadType1 = DpadType.Unknown;
					dpadType = (DpadType)Enum.Parse(typeof(DpadType), scriptSetting.GetValue(str, "DpadType", dpadType1.ToString()));
				}
				catch (Exception exception)
				{
					throw new Exception("Invalid controller config, unknown DpadType reconfigure your controller from the menu.");
				}
				foreach (DeviceButton value in Enum.GetValues(typeof(DeviceButton)))
				{
					int num = scriptSetting.GetValue<int>(str, value.ToString(), -1);
					try
					{
						directInputManager.config.Add(new Tuple<int, DeviceButton>(num, value));
					}
					catch (Exception exception1)
					{
						throw new Exception("Invalid controller config, please reconfigure your controller from the menu.");
					}
				}
			}
			catch (Exception exception2)
			{
				throw new Exception("Error reading controller config, make sure the file contains a valid controller config.");
			}
			return directInputManager;
		}

		private static Vector2 NormalizeThumbStick(float x, float y)
		{
			Vector2 vector2 = new Vector2((x - 32767f) / 32767f, -(y - 32767f) / 32767f);
			if (Math.Abs(vector2.X) < 0.4f)
			{
				vector2.X = 0f;
			}
			if (Math.Abs(vector2.Y) < 0.4f)
			{
				vector2.Y = 0f;
			}
			return vector2;
		}
	}
}