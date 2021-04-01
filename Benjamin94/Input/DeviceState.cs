using GTA.Math;
using System;
using System.Collections.Generic;

namespace Benjamin94.Input
{
	public class DeviceState
	{
		public Vector2 LeftThumbStick;

		public Vector2 RightThumbStick;

		public List<DeviceButton> Buttons = new List<DeviceButton>();

		public DeviceState()
		{
		}

		public override string ToString()
		{
			return string.Concat(new object[] { "LeftStick: ", this.LeftThumbStick, ", RightStick: ", this.RightThumbStick, ", Buttons: ", string.Join<DeviceButton>(",", this.Buttons) });
		}
	}
}