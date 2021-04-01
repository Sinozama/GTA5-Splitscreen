using GTA.Math;
using SharpDX.XInput;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Benjamin94.Input
{
	internal class XInputManager : InputManager
	{
		private Controller device;

		public override string DeviceGuid
		{
			get
			{
				return this.DeviceName;
			}
		}

		public override string DeviceName
		{
			get
			{
				string str = string.Concat("XInput controller: ", this.device.get_UserIndex());
				return str;
			}
		}

		public XInputManager(Controller device)
		{
			this.device = device;
			DeviceState state = this.GetState();
			this.X_CENTER_L = state.LeftThumbStick.X;
			this.Y_CENTER_L = state.LeftThumbStick.Y;
			this.X_CENTER_R = state.RightThumbStick.X;
			this.Y_CENTER_R = state.RightThumbStick.Y;
		}

		public override void Cleanup()
		{
		}

		public static List<Controller> GetDevices()
		{
			List<Controller> controllers = new List<Controller>();
			foreach (UserIndex value in Enum.GetValues(typeof(UserIndex)))
			{
				if (value != 255)
				{
					Controller controller = new Controller(value);
					if (controller.get_IsConnected())
					{
						controllers.Add(controller);
					}
				}
			}
			return controllers;
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

		public override DeviceState GetState()
		{
			DeviceState deviceState;
			try
			{
				Gamepad gamepad = this.device.GetState().Gamepad;
				DeviceState deviceState1 = new DeviceState()
				{
					LeftThumbStick = XInputManager.NormalizeThumbStick(gamepad.LeftThumbX, gamepad.LeftThumbY, 7849),
					RightThumbStick = XInputManager.NormalizeThumbStick(gamepad.RightThumbX, gamepad.RightThumbY, 8689)
				};
				if (gamepad.LeftTrigger > 30)
				{
					deviceState1.Buttons.Add(DeviceButton.LeftTrigger);
				}
				if (gamepad.RightTrigger > 30)
				{
					deviceState1.Buttons.Add(DeviceButton.RightTrigger);
				}
				if (gamepad.Buttons != 0)
				{
					if ((gamepad.Buttons & 1) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.DPadUp);
					}
					if ((gamepad.Buttons & 4) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.DPadLeft);
					}
					if ((gamepad.Buttons & 8) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.DPadRight);
					}
					if ((gamepad.Buttons & 2) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.DPadDown);
					}
					if ((gamepad.Buttons & 4096) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.A);
					}
					if ((gamepad.Buttons & 8192) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.B);
					}
					if ((gamepad.Buttons & 16384) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.X);
					}
					if ((gamepad.Buttons & -32768) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.Y);
					}
					if ((gamepad.Buttons & 16) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.Start);
					}
					if ((gamepad.Buttons & 32) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.Back);
					}
					if ((gamepad.Buttons & 256) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.LeftShoulder);
					}
					if ((gamepad.Buttons & 512) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.RightShoulder);
					}
					if ((gamepad.Buttons & 64) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.LeftStick);
					}
					if ((gamepad.Buttons & 128) != 0)
					{
						deviceState1.Buttons.Add(DeviceButton.RightStick);
					}
				}
				deviceState = deviceState1;
			}
			catch (Exception exception)
			{
				deviceState = new DeviceState();
			}
			return deviceState;
		}

		private static Vector2 NormalizeThumbStick(short x, short y, int deadZone)
		{
			int num = x;
			int num1 = y;
			if (num * num < deadZone * deadZone)
			{
				x = 0;
			}
			if (num1 * num1 < deadZone * deadZone)
			{
				y = 0;
			}
			return new Vector2((x < 0 ? -((float)x / -32768f) : (float)x / 32767f), (y < 0 ? -((float)y / -32768f) : (float)y / 32767f));
		}
	}
}