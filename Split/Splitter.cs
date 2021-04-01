using GTA;
using GTA.Math;
using System;
using System.Threading;

namespace Split
{
	public class Splitter
	{
		public static bool change;

		public static bool changed;

		public static int cycle;

		public static AutoResetEvent mrse;

		static Splitter()
		{
			Splitter.change = false;
			Splitter.changed = false;
			Splitter.cycle = 1;
			Splitter.mrse = new AutoResetEvent(false);
		}

		public Splitter()
		{
		}

		private static Vector3 polar3DToWorld3D(Vector3 entityPosition, float radius, float polarAngleDeg, float azimuthAngleDeg)
		{
			double num = (double)polarAngleDeg * 3.14159265358979 / 180;
			double num1 = (double)azimuthAngleDeg * 3.14159265358979 / 180;
			float x = entityPosition.X + radius * (float)(Math.Sin(num1) * Math.Cos(num));
			float y = entityPosition.Y - radius * (float)(Math.Sin(num1) * Math.Sin(num));
			float z = entityPosition.Z - radius * (float)Math.Cos(num1);
			return new Vector3(x, y, z);
		}

		public void Tick(Ped ped, float _radius, float _polarAngleDeg, float _azimuthAngleDeg)
		{
			if (Splitter.cycle == 2)
			{
				Splitter.cycle = 4;
			}
			if ((!Splitter.change ? false : Splitter.changed))
			{
				Splitter.changed = false;
				World.set_RenderingCamera(TwoPlayerMod.vcam);
				Vector3 world3D = Splitter.polar3DToWorld3D(TwoPlayerMod.secondP.get_Position(), TwoPlayerMod._radius, TwoPlayerMod._polarAngleDeg, TwoPlayerMod._azimuthAngleDeg);
				TwoPlayerMod.vcam.set_Position(new Vector3(world3D.X, world3D.Y, world3D.Z));
				TwoPlayerMod.vcam.PointAt(TwoPlayerMod.secondP);
				Splitter.cycle = 2;
			}
			else if ((Splitter.change ? false : Splitter.changed))
			{
				Splitter.changed = false;
				World.set_RenderingCamera(null);
				Splitter.cycle = 2;
			}
		}
	}
}