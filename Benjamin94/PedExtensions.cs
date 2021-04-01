using GTA;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Benjamin94
{
	public static class PedExtensions
	{
		public static bool IsPlayerPed(this Ped ped)
		{
			bool flag;
			if (TwoPlayerMod.player1 != ped)
			{
				foreach (PlayerPed playerPed in TwoPlayerMod.playerPeds)
				{
					if (playerPed.Ped == ped)
					{
						flag = true;
						return flag;
					}
				}
				flag = false;
			}
			else
			{
				flag = true;
			}
			return flag;
		}
	}
}