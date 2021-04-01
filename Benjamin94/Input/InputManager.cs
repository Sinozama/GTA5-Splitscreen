using GTA.Math;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;

namespace Benjamin94.Input
{
	public abstract class InputManager
	{
		protected float X_CENTER_L = 0f;

		protected float Y_CENTER_L = 0f;

		protected float X_CENTER_R = 0f;

		protected float Y_CENTER_R = 0f;

		public abstract string DeviceGuid
		{
			get;
		}

		public abstract string DeviceName
		{
			get;
		}

		protected InputManager()
		{
		}

		public abstract void Cleanup();

		public static List<InputManager> GetAvailableInputManagers()
		{
			List<InputManager> inputManagers = new List<InputManager>();
			foreach (Controller device in XInputManager.GetDevices())
			{
				inputManagers.Add(new XInputManager(device));
			}
			foreach (Joystick joystick in DirectInputManager.GetDevices())
			{
				inputManagers.Add(new DirectInputManager(joystick));
			}
			return inputManagers;
		}

		public Direction GetDirection(DeviceButton stick)
		{
			Direction direction;
			DeviceState state = this.GetState();
			if (stick != DeviceButton.LeftStick)
			{
				direction = (stick != DeviceButton.RightStick ? Direction.None : this.GetDirection(state.RightThumbStick.X, state.RightThumbStick.Y, this.X_CENTER_R, this.Y_CENTER_R));
			}
			else
			{
				direction = this.GetDirection(state.LeftThumbStick.X, state.LeftThumbStick.Y, this.X_CENTER_L, this.Y_CENTER_L);
			}
			return direction;
		}

		protected abstract Direction GetDirection(float X, float Y, float xCenter, float yCenter);

		public abstract DeviceState GetState();

		public bool isAllPressed(params DeviceButton[] btns)
		{
			return this.IsPressed(true, btns);
		}

		public bool isAnyPressed(params DeviceButton[] btns)
		{
			return this.IsPressed(false, btns);
		}

		public bool IsDirectionLeft(Direction dir)
		{
			return (dir == Direction.Left || dir == Direction.BackwardLeft ? true : dir == Direction.ForwardLeft);
		}

		public bool IsDirectionRight(Direction dir)
		{
			return (dir == Direction.Right || dir == Direction.BackwardRight ? true : dir == Direction.ForwardRight);
		}

		public bool isPressed(DeviceButton btn)
		{
			return this.GetState().Buttons.Contains(btn);
		}

		private bool IsPressed(bool allPressed, params DeviceButton[] btns)
		{
			bool flag;
			DeviceState state = this.GetState();
			if (!allPressed)
			{
				DeviceButton[] deviceButtonArray = btns;
				int num = 0;
				while (num < (int)deviceButtonArray.Length)
				{
					DeviceButton deviceButton = deviceButtonArray[num];
					if (!state.Buttons.Contains(deviceButton))
					{
						num++;
					}
					else
					{
						flag = true;
						return flag;
					}
				}
				flag = false;
			}
			else
			{
				DeviceButton[] deviceButtonArray1 = btns;
				int num1 = 0;
				while (num1 < (int)deviceButtonArray1.Length)
				{
					DeviceButton deviceButton1 = deviceButtonArray1[num1];
					allPressed = state.Buttons.Contains(deviceButton1);
					if (allPressed)
					{
						num1++;
					}
					else
					{
						flag = false;
						return flag;
					}
				}
				flag = allPressed;
			}
			return flag;
		}
	}
}