using Benjamin94.Input;
using GTA;
using GTA.Math;
using GTA.Native;
using SharpDX.XInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Benjamin94
{
	internal class PlayerPed
	{
		private static WeaponHash[] meleeWeapons;

		private static WeaponHash[] throwables;

		private static WeaponHash[] weaponset;

		private static WeaponHash[] weaponchain;

		private static WeaponHash[] unavailable;

		private readonly GTA.Ped Player1;

		public readonly SharpDX.XInput.UserIndex UserIndex = 255;

		private VehicleAction LastVehicleAction = VehicleAction.Brake;

		private GTA.Ped[] Targets = null;

		private int MaxHealth = 100;

		private WeaponHash[] currentset = PlayerPed.weaponset;

		private int weaponIndex = 0;

		private GTA.Ped[] instTarget = null;

		private float speed = 0f;

		private Vector2 prevState = Vector2.get_Zero();

		private Vector3 prevDest = Vector3.get_Zero();

		private Vector3 dest = Vector3.get_Zero();

		private Vector2 prevAim = Vector2.get_Zero();

		private Vector3 aimoffset = Vector3.get_Zero();

		private Vector3 aim = Vector3.get_Zero();

		private Vector3 lasersight = Vector3.get_Zero();

		private Vector3 instaim = Vector3.get_Zero();

		private DateTime carflip = new DateTime();

		private DateTime wptime = new DateTime();

		private bool jump = false;

		private float polaraim = 0f;

		private float azimaim = 0f;

		private int targetIndex = 0;

		private Dictionary<PlayerPedAction, int> lastActions = new Dictionary<PlayerPedAction, int>();

		public readonly InputManager Input;

		private readonly PedHash CharacterHash;

		private readonly Color MarkerColor;

		private readonly BlipColor blipcolor;

		private readonly bool god;

		private readonly string pmodel;

		private bool resetWalking = false;

		public GTA.Ped Ped
		{
			get;
			internal set;
		}

		private int TargetIndex
		{
			get
			{
				if (this.targetIndex < 0)
				{
					this.targetIndex = (int)this.Targets.Length - 1;
				}
				if (this.targetIndex >= (int)this.Targets.Length)
				{
					this.targetIndex = 0;
				}
				return this.targetIndex;
			}
			set
			{
				this.targetIndex = value;
			}
		}

		private int WeaponIndex
		{
			get
			{
				if (this.weaponIndex < 0)
				{
					this.weaponIndex = (int)this.currentset.Length - 1;
				}
				if (this.weaponIndex >= (int)this.currentset.Length)
				{
					this.weaponIndex = 0;
				}
				return this.weaponIndex;
			}
			set
			{
				this.weaponIndex = value;
			}
		}

		static PlayerPed()
		{
			PlayerPed.meleeWeapons = new WeaponHash[] { typeof(<PrivateImplementationDetails>).GetField("B9D158F61B91570025273ED0EB048FBCD4067ED8").FieldHandle };
			PlayerPed.throwables = new WeaponHash[] { typeof(<PrivateImplementationDetails>).GetField("F7823D129A9FA60145F9A2F0CBC6BF50CD54227B").FieldHandle };
			PlayerPed.weaponset = new WeaponHash[] { typeof(<PrivateImplementationDetails>).GetField("9667E0C9DC0F44E05FD04566D6FDAA2C32E5D838").FieldHandle };
			PlayerPed.weaponchain = new WeaponHash[] { typeof(<PrivateImplementationDetails>).GetField("FD35D38C99ACC1B7DA47EF67ABF3D8150FC7F129").FieldHandle };
			PlayerPed.unavailable = new WeaponHash[] { typeof(<PrivateImplementationDetails>).GetField("6C9B0FC6DAEB099963C205D100DAECA8C68CFF64").FieldHandle };
		}

		public PlayerPed(SharpDX.XInput.UserIndex userIndex, PedHash characterHash, BlipSprite blipSprite, BlipColor blipColor, GTA.Ped player1, InputManager input, bool godmode)
		{
			this.UserIndex = userIndex;
			this.CharacterHash = characterHash;
			this.pmodel = PlayerSettings.GetValue(userIndex, "ADDON", "NONE");
			this.Player1 = player1;
			this.Input = input;
			this.blipcolor = blipColor;
			this.god = godmode;
			this.SetupPed(this.god);
			foreach (PlayerPedAction value in Enum.GetValues(typeof(PlayerPedAction)))
			{
				this.lastActions[value] = Game.get_GameTime();
			}
			BlipColor blipColor1 = blipColor;
			switch (blipColor1)
			{
				case 0:
				{
					this.MarkerColor = Color.White;
					break;
				}
				case 1:
				{
					this.MarkerColor = Color.Red;
					break;
				}
				case 2:
				{
					this.MarkerColor = Color.Green;
					break;
				}
				case 3:
				{
					this.MarkerColor = Color.Blue;
					break;
				}
				default:
				{
					if (blipColor1 == 66)
					{
						this.MarkerColor = Color.Yellow;
						break;
					}
					else
					{
						this.MarkerColor = Color.OrangeRed;
						break;
					}
				}
			}
			this.UpdateBlip(blipSprite, blipColor);
		}

		private bool CanDoAction(PlayerPedAction action, int time)
		{
			bool gameTime = Game.get_GameTime() - this.lastActions[action] >= time;
			return gameTime;
		}

		public void Clean()
		{
			if (this.Ped != null)
			{
				this.Ped.Delete();
				this.Ped = null;
			}
			if (this.Input != null)
			{
				this.Input.Cleanup();
			}
		}

		private GTA.Ped[] GetMeleeTargets()
		{
			GTA.Ped[] array = (
				from p in World.GetNearbyPeds(this.Ped, 15f)
				where this.IsValidTarget(p)
				orderby p.get_Position().DistanceTo(this.Ped.get_Position())
				select p).ToArray<GTA.Ped>();
			return array;
		}

		private GTA.Ped[] GetTargets()
		{
			GTA.Ped[] array = (
				from p in World.GetNearbyPeds(this.Ped, 50f)
				where this.IsValidTarget(p)
				orderby p.get_Position().DistanceTo(this.Ped.get_Position())
				select p).ToArray<GTA.Ped>();
			return array;
		}

		private VehicleAction GetVehicleAction(Vehicle v)
		{
			VehicleAction vehicleAction;
			Direction direction = this.Input.GetDirection(DeviceButton.LeftStick);
			if (this.Input.isPressed(DeviceButton.A))
			{
				if (!this.Input.IsDirectionLeft(direction))
				{
					vehicleAction = (!this.Input.IsDirectionRight(direction) ? VehicleAction.HandBrakeStraight : VehicleAction.HandBrakeRight);
				}
				else
				{
					vehicleAction = VehicleAction.HandBrakeLeft;
				}
			}
			else if (this.Input.isPressed(DeviceButton.RightTrigger))
			{
				if (!this.Input.IsDirectionLeft(direction))
				{
					vehicleAction = (!this.Input.IsDirectionRight(direction) ? VehicleAction.GoForwardStraightFast : VehicleAction.GoForwardRight);
				}
				else
				{
					vehicleAction = VehicleAction.GoForwardLeft;
				}
			}
			else if (this.Input.isPressed(DeviceButton.LeftTrigger))
			{
				if (!this.Input.IsDirectionLeft(direction))
				{
					vehicleAction = (!this.Input.IsDirectionRight(direction) ? VehicleAction.ReverseStraight : VehicleAction.ReverseRight);
				}
				else
				{
					vehicleAction = VehicleAction.ReverseLeft;
				}
			}
			else if (!this.Input.IsDirectionLeft(direction))
			{
				vehicleAction = (!this.Input.IsDirectionRight(direction) ? VehicleAction.RevEngine : VehicleAction.SwerveRight);
			}
			else
			{
				vehicleAction = VehicleAction.SwerveLeft;
			}
			return vehicleAction;
		}

		private bool IsMelee(WeaponHash hash)
		{
			return PlayerPed.meleeWeapons.Contains<WeaponHash>(hash);
		}

		private bool IsThrowable(WeaponHash hash)
		{
			return PlayerPed.throwables.Contains<WeaponHash>(hash);
		}

		private bool IsValidTarget(GTA.Ped target)
		{
			return (!(target != null) || target.IsPlayerPed() || !(target != this.Player1) || !target.get_IsAlive() ? false : target.get_IsOnScreen());
		}

		public Vector3 NC_Get_Cam_Rotation()
		{
			Vector3 vector3;
			try
			{
				vector3 = Function.Call<Vector3>(-8973591984652881733L, new InputArgument[] { 0 });
				return vector3;
			}
			catch
			{
			}
			vector3 = new Vector3(0f, 0f, 0f);
			return vector3;
		}

		private void NotifyWeapon()
		{
			UI.ShowSubtitle(string.Concat("Player weapon: ~g~", this.currentset[this.WeaponIndex]));
		}

		private void PerformVehicleAction(GTA.Ped ped, Vehicle vehicle, VehicleAction action)
		{
			Function.Call(-4311672250463297239L, new InputArgument[] { ped, vehicle, action, -1 });
		}

		public void Respawn()
		{
			if (this.Ped != null)
			{
				this.Ped.Delete();
			}
			this.SetupPed(this.god);
			this.UpdateBlip(1, this.blipcolor);
			this.weaponIndex = 0;
		}

		private void SelectWeapon(GTA.Ped p, WeaponHash weaponHash)
		{
			this.WeaponIndex = Array.IndexOf<WeaponHash>(this.currentset, weaponHash);
			Function.Call(-5911376166256149492L, new InputArgument[] { p, new InputArgument(weaponHash), true });
			this.UpdateLastAction(PlayerPedAction.SelectWeapon);
		}

		private void SetupPed(bool god)
		{
			Vector3 position;
			if (!this.pmodel.Equals("NONE"))
			{
				Model model = this.pmodel;
				position = this.Player1.get_Position();
				this.Ped = World.CreatePed(model, position.Around(2f));
			}
			else
			{
				Model characterHash = this.CharacterHash;
				position = this.Player1.get_Position();
				this.Ped = World.CreatePed(characterHash, position.Around(2f));
			}
			this.MaxHealth = this.Ped.get_Health();
			while (!this.Ped.Exists())
			{
				Script.Wait(100);
			}
			this.Ped.get_Task().ClearAllImmediately();
			this.Ped.set_AlwaysKeepTask(true);
			this.Ped.set_NeverLeavesGroup(true);
			this.Ped.set_CanBeTargetted(true);
			this.Ped.set_RelationshipGroup(this.Player1.get_RelationshipGroup());
			this.Ped.set_CanRagdoll(true);
			this.Ped.set_CanWrithe(true);
			this.Ped.set_IsEnemy(false);
			this.Ped.set_DrownsInWater(false);
			this.Ped.set_DiesInstantlyInWater(false);
			this.Ped.set_DropsWeaponsOnDeath(false);
			this.Ped.set_IsInvincible((god ? true : false));
			this.Ped.set_CanFlyThroughWindscreen(true);
			this.Ped.set_Armor(100);
			Function.Call(-6950556924876694540L, new InputArgument[] { this.Ped, true });
			Function.Call(8116279360099375049L, new InputArgument[] { this.Ped, 0, 0 });
			Function.Call(-6955927877681029095L, new InputArgument[] { this.Ped, 46, true });
			foreach (WeaponHash value in Enum.GetValues(typeof(WeaponHash)))
			{
				try
				{
					Weapon weapon = this.Ped.get_Weapons().Give(value, 2147483647, true, true);
					weapon.set_InfiniteAmmo(true);
					weapon.set_InfiniteAmmoClip(true);
					if (value == 100416529)
					{
						weapon.SetComponent(-1489156508, true);
					}
				}
				catch (Exception exception)
				{
				}
			}
			this.SelectWeapon(this.Ped, this.currentset[this.WeaponIndex]);
		}

		public void Tick()
		{
			if ((!TwoPlayerMod.splitmode ? false : !this.Input.isPressed(DeviceButton.LeftShoulder)))
			{
				Vector2 rightThumbStick = this.Input.GetState().RightThumbStick;
				if (this.Input.isPressed(DeviceButton.DPadUp))
				{
					TwoPlayerMod._radius -= 0.25f;
				}
				if (this.Input.isPressed(DeviceButton.DPadDown))
				{
					TwoPlayerMod._radius += 0.25f;
				}
				if (TwoPlayerMod._radius < 1f)
				{
					TwoPlayerMod._radius = 1f;
				}
				TwoPlayerMod._polarAngleDeg = TwoPlayerMod._polarAngleDeg + rightThumbStick.X * 5f;
				if ((TwoPlayerMod._polarAngleDeg >= 360f ? true : TwoPlayerMod._polarAngleDeg <= -360f))
				{
					TwoPlayerMod._polarAngleDeg = 0f;
				}
				TwoPlayerMod._azimuthAngleDeg = TwoPlayerMod._azimuthAngleDeg + rightThumbStick.Y * 5f;
				if (TwoPlayerMod._azimuthAngleDeg >= 179f)
				{
					TwoPlayerMod._azimuthAngleDeg = 179f;
				}
				if (TwoPlayerMod._azimuthAngleDeg <= 90f)
				{
					TwoPlayerMod._azimuthAngleDeg = 90f;
				}
				if (TwoPlayerMod._azimuthAngleDeg >= 360f)
				{
					TwoPlayerMod._azimuthAngleDeg = 0f;
				}
			}
			if (this.Ped.get_IsInParachuteFreeFall())
			{
				if (!this.Ped.get_Weapons().HasWeapon(-72657034))
				{
					this.Ped.get_Weapons().Give(-72657034, 1, true, true);
				}
				this.Ped.get_Weapons().Select(-72657034, true);
				if (this.Input.isPressed(DeviceButton.X))
				{
					Function.Call(1649494491004346913L, new InputArgument[] { this.Ped });
				}
			}
			if (!this.Ped.IsInVehicle())
			{
				this.UpdateFoot();
				goto Label0;
			}
			else
			{
				if (TwoPlayerMod.customCamera)
				{
					this.UpdateCombat(() => this.Input.isPressed(DeviceButton.LeftShoulder), () => this.Input.isPressed(DeviceButton.RightShoulder));
				}
				Vehicle currentVehicle = this.Ped.get_CurrentVehicle();
				if (currentVehicle.get_HasCollision())
				{
					if ((!(Game.get_Player().get_Character().get_LastVehicle() == currentVehicle) || !(Game.get_Player().get_Character().get_CurrentVehicle() == null) || !Game.get_Player().get_Character().get_IsFalling() ? false : currentVehicle.get_Speed() > 15f))
					{
						Function.Call(5024833729465002049L, new InputArgument[] { this.Ped });
					}
				}
				if (currentVehicle.GetPedOnSeat(-1) != this.Ped)
				{
					this.UpdateCombat(() => this.Input.isPressed(DeviceButton.LeftShoulder), () => this.Input.isPressed(DeviceButton.RightShoulder));
				}
				else if (currentVehicle.get_Model().get_IsPlane())
				{
					if (!this.Input.isPressed(DeviceButton.LeftShoulder))
					{
						currentVehicle.set_Rotation(new Vector3(Function.Call<float>(-3144143070260239874L, new InputArgument[] { currentVehicle }), 0f, currentVehicle.get_Rotation().Z));
						this.Updatedrive(currentVehicle);
						Vector2 leftThumbStick = this.Input.GetState().LeftThumbStick;
						if (leftThumbStick.Y < -0.9f)
						{
							currentVehicle.set_Rotation(new Vector3(Function.Call<float>(-3144143070260239874L, new InputArgument[] { currentVehicle }) + 2f, 0f, currentVehicle.get_Rotation().Z));
						}
						else if (leftThumbStick.Y < -0.4f)
						{
							currentVehicle.set_Rotation(new Vector3(Function.Call<float>(-3144143070260239874L, new InputArgument[] { currentVehicle }) + 1f, 0f, currentVehicle.get_Rotation().Z));
						}
						if (leftThumbStick.Y > 0.9f)
						{
							currentVehicle.set_Rotation(new Vector3(Function.Call<float>(-3144143070260239874L, new InputArgument[] { currentVehicle }) - 2f, 0f, currentVehicle.get_Rotation().Z));
						}
						else if (leftThumbStick.Y > 0.4f)
						{
							currentVehicle.set_Rotation(new Vector3(Function.Call<float>(-3144143070260239874L, new InputArgument[] { currentVehicle }) - 1f, 0f, currentVehicle.get_Rotation().Z));
						}
						if (leftThumbStick.X > 0.4f)
						{
							currentVehicle.set_Rotation(new Vector3(currentVehicle.get_Rotation().X, 0f, currentVehicle.get_Rotation().Z - 1f));
						}
						if (leftThumbStick.X < -0.4f)
						{
							currentVehicle.set_Rotation(new Vector3(currentVehicle.get_Rotation().X, 0f, currentVehicle.get_Rotation().Z + 1f));
						}
						if (this.Input.isPressed(DeviceButton.LeftTrigger))
						{
							if (DateTime.Now < this.carflip)
							{
								return;
							}
							if (currentVehicle.get_LandingGear() != 0)
							{
								currentVehicle.set_LandingGear(0);
							}
							else
							{
								currentVehicle.set_LandingGear(1);
							}
							this.carflip = DateTime.Now + TimeSpan.FromMilliseconds(1000);
						}
					}
					else
					{
						Direction direction = this.Input.GetDirection(DeviceButton.LeftStick);
						if (this.Input.IsDirectionLeft(direction))
						{
							this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.ReverseLeft);
						}
						else if (!this.Input.IsDirectionRight(direction))
						{
							this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.BrakeReverseFast);
						}
						else
						{
							this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.ReverseRight);
						}
						currentVehicle.set_Speed(-5f);
					}
				}
				else if (!this.Input.isPressed(DeviceButton.LeftStick))
				{
					VehicleAction vehicleAction = this.GetVehicleAction(currentVehicle);
					if ((vehicleAction == this.LastVehicleAction ? false : TwoPlayerMod.customCamera))
					{
						this.LastVehicleAction = vehicleAction;
						this.PerformVehicleAction(this.Ped, currentVehicle, vehicleAction);
					}
					else if (!TwoPlayerMod.customCamera)
					{
						this.Updatedrive(currentVehicle);
						if (this.Input.isPressed(DeviceButton.LeftShoulder))
						{
							Direction direction1 = this.Input.GetDirection(DeviceButton.LeftStick);
							if (this.Input.IsDirectionLeft(direction1))
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.ReverseLeft);
							}
							else if (!this.Input.IsDirectionRight(direction1))
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.BrakeReverseFast);
							}
							else
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.ReverseRight);
							}
						}
						if (this.Input.isPressed(DeviceButton.RightShoulder))
						{
							Vector2 vector2 = this.Input.GetState().LeftThumbStick;
							if ((vector2.X <= 0.1f ? true : !this.Input.isPressed(DeviceButton.RightShoulder)))
							{
								if ((vector2.X >= -0.1f ? false : this.Input.isPressed(DeviceButton.RightShoulder)))
								{
									this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.HandBrakeLeft);
									return;
								}
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.HandBrakeStraight);
							}
							else
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.HandBrakeRight);
								return;
							}
						}
						if (this.Input.isPressed(DeviceButton.LeftTrigger))
						{
							Vector2 leftThumbStick1 = this.Input.GetState().LeftThumbStick;
							if ((leftThumbStick1.X >= -0.95f ? false : leftThumbStick1.Y < 0.55f))
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.GoForwardLeft);
							}
							else if (leftThumbStick1.X < -0.1f)
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.SwerveLeft);
							}
							else if ((leftThumbStick1.X <= 0.95f ? false : leftThumbStick1.Y < 0.55f))
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.GoForwardRight);
							}
							else if (leftThumbStick1.X > 0.1f)
							{
								this.PerformVehicleAction(this.Ped, currentVehicle, VehicleAction.SwerveRight);
							}
						}
					}
				}
				else if (DateTime.Now >= this.carflip)
				{
					this.carflip = DateTime.Now + TimeSpan.FromMilliseconds(2000);
					this.speed = this.Input.GetState().LeftThumbStick.Y * 40f + 20f;
					InputArgument[] ped = new InputArgument[] { this.Ped, currentVehicle, Game.get_Player().get_Character().get_Position().X, Game.get_Player().get_Character().get_Position().Y, 0, this.speed, 1f, default(InputArgument), default(InputArgument), default(InputArgument), default(InputArgument) };
					ped[7] = currentVehicle.get_Model().get_Hash();
					ped[8] = 16777216;
					ped[9] = 1f;
					ped[10] = true;
					Function.Call(-2115941754365708377L, ped);
					return;
				}
				else
				{
					return;
				}
				goto Label0;
			}
			return;
		Label0:
			if (this.Input.isPressed(DeviceButton.Y))
			{
				if (!this.Ped.IsInVehicle())
				{
					TwoPlayerMod.HandleEnterVehicle(this.Ped);
				}
				else
				{
					Vehicle vehicle = this.Ped.get_CurrentVehicle();
					if (vehicle.get_Speed() <= 7f)
					{
						this.Ped.get_Task().LeaveVehicle();
					}
					else
					{
						Function.Call(-3180721793039024638L, new InputArgument[] { this.Ped, vehicle, 4160 });
					}
				}
			}
			if (this.Input.isPressed(DeviceButton.RightStick))
			{
				this.aim = this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 700f, 0f));
				this.polaraim = this.Ped.get_Rotation().Z;
				this.azimaim = 90f;
			}
			if (this.Input.isPressed(DeviceButton.Start))
			{
				if (DateTime.Now < this.wptime)
				{
					return;
				}
				this.wptime = DateTime.Now + TimeSpan.FromMilliseconds(1000);
				TwoPlayerMod.customCamera = !TwoPlayerMod.customCamera;
				if (!TwoPlayerMod.customCamera)
				{
					UI.Notify("In Normal MODE");
				}
				if (TwoPlayerMod.customCamera)
				{
					UI.Notify("In SA MODE");
				}
			}
			if ((!this.Input.isPressed(DeviceButton.DPadUp) ? false : !this.Input.isPressed(DeviceButton.LeftShoulder)))
			{
				TwoPlayerMod.CamDirection = TwoPlayerMod.SA_Direction.North;
			}
			else if ((!this.Input.isPressed(DeviceButton.DPadDown) ? false : !this.Input.isPressed(DeviceButton.LeftShoulder)))
			{
				TwoPlayerMod.CamDirection = TwoPlayerMod.SA_Direction.South;
			}
			else if ((!this.Input.isPressed(DeviceButton.DPadRight) ? false : !this.Input.isPressed(DeviceButton.LeftShoulder)))
			{
				TwoPlayerMod.CamDirection = TwoPlayerMod.SA_Direction.East;
			}
			else if ((!this.Input.isPressed(DeviceButton.DPadLeft) ? false : !this.Input.isPressed(DeviceButton.LeftShoulder)))
			{
				TwoPlayerMod.CamDirection = TwoPlayerMod.SA_Direction.West;
			}
			if (this.Ped.get_IsDead())
			{
				UI.ShowSubtitle(string.Concat("Player ~g~", this.UserIndex, "~w~ press ~g~ Select ~w~ to respawn~w~."), 1000);
			}
			if ((this.Ped.IsInVehicle() ? false : this.Input.isPressed(DeviceButton.Back)))
			{
				this.Respawn();
				return;
			}
			else
			{
				return;
			}
		}

		public void UpdateBlip(BlipSprite sprite, BlipColor color)
		{
			this.Ped.get_CurrentBlip().Remove();
			this.Ped.AddBlip().set_Sprite(sprite);
			this.Ped.get_CurrentBlip().set_Color(color);
		}

		private void UpdateCombat(Func<bool> firstButton, Func<bool> secondButton)
		{
			if ((!this.Input.isPressed(DeviceButton.LeftTrigger) ? false : !this.Ped.IsInVehicle()))
			{
				WeaponHash[] weaponHashArray = PlayerPed.throwables;
				for (int i = 0; i < (int)weaponHashArray.Length; i++)
				{
					WeaponHash weaponHash = weaponHashArray[i];
					Function.Call(-266889792322137628L, new InputArgument[] { this.Ped, new InputArgument(weaponHash), true });
				}
			}
			if ((!this.Input.isPressed(DeviceButton.RightTrigger) ? false : !this.Ped.IsInVehicle()))
			{
				if (DateTime.Now < this.wptime)
				{
					return;
				}
				this.wptime = DateTime.Now + TimeSpan.FromMilliseconds(1000);
				this.weaponIndex = 0;
				if (this.currentset != PlayerPed.weaponset)
				{
					this.currentset = PlayerPed.weaponset;
					UI.ShowSubtitle("Default set selected ", 1000);
				}
				else
				{
					this.currentset = PlayerPed.weaponchain;
					UI.ShowSubtitle("Chain set selected ", 1000);
				}
			}
			if (this.Input.isPressed(DeviceButton.B))
			{
				if (this.CanDoAction(PlayerPedAction.SelectWeapon, 300))
				{
					if (this.UpdateWeaponIndex())
					{
						this.SelectWeapon(this.Ped, this.currentset[this.WeaponIndex]);
					}
					this.NotifyWeapon();
				}
			}
			if (TwoPlayerMod.customCamera)
			{
				if (!firstButton())
				{
					this.TargetIndex = 0;
				}
				else
				{
					if (!secondButton())
					{
						this.Targets = this.GetTargets();
					}
					if (this.CanDoAction(PlayerPedAction.SelectTarget, 500))
					{
						Direction direction = this.Input.GetDirection(DeviceButton.RightStick);
						if (this.Input.IsDirectionLeft(direction))
						{
							this.TargetIndex = this.TargetIndex - 1;
							this.UpdateLastAction(PlayerPedAction.SelectTarget);
						}
						if (this.Input.IsDirectionRight(direction))
						{
							this.TargetIndex = this.TargetIndex + 1;
							this.UpdateLastAction(PlayerPedAction.SelectTarget);
						}
					}
					GTA.Ped ped = this.Targets.ElementAtOrDefault<GTA.Ped>(this.TargetIndex);
					if (ped == null)
					{
						return;
					}
					if (!ped.get_IsAlive())
					{
						this.Targets = this.GetTargets();
					}
					if (ped != null)
					{
						World.DrawMarker(0, ped.GetBoneCoord(31086) + new Vector3(0f, 0f, 1f), GameplayCamera.get_Direction(), GameplayCamera.get_Rotation(), new Vector3(1f, 1f, 1f), Color.OrangeRed);
						if (!secondButton())
						{
							this.Ped.get_Task().ClearAll();
							this.Ped.get_Task().AimAt(ped, 100);
						}
						else
						{
							this.SelectWeapon(this.Ped, this.currentset[this.WeaponIndex]);
							if (this.IsThrowable(this.currentset[this.WeaponIndex]))
							{
								if (this.CanDoAction(PlayerPedAction.ThrowTrowable, 1500))
								{
									Function.Call(8252165847224375889L, new InputArgument[] { this.Ped, ped.get_Position().X, ped.get_Position().Y, ped.get_Position().Z });
									this.UpdateLastAction(PlayerPedAction.ThrowTrowable);
								}
							}
							else if (this.CanDoAction(PlayerPedAction.Shoot, 500))
							{
								if (this.Ped.IsInVehicle())
								{
									Function.Call(3425815346453651825L, new InputArgument[] { this.Ped, ped, 0, 0, 0, 0, 50f, 100, 1, -957453492 });
								}
								else if (!this.IsMelee(this.currentset[this.WeaponIndex]))
								{
									this.Ped.get_Task().ShootAt(ped, 500, -957453492);
								}
								else
								{
									this.Ped.get_Task().FightAgainst(ped, -1);
								}
								this.UpdateLastAction(PlayerPedAction.Shoot);
							}
						}
					}
					if (this.Ped.get_IsOnScreen())
					{
						Vector3 boneCoord = this.Ped.GetBoneCoord(31086);
						boneCoord.Z += 0.3f;
						boneCoord.X += 0.1f;
						UIRectangle uIRectangle = new UIRectangle(UI.WorldToScreen(boneCoord), new Size(this.MaxHealth / 2, 5), Color.Black);
						uIRectangle.Draw();
						uIRectangle.set_Size(new Size(this.Ped.get_Health() / 2, 5));
						uIRectangle.set_Color(Color.LimeGreen);
						uIRectangle.Draw();
					}
				}
			}
			else if ((!this.Ped.IsInVehicle() ? false : this.Ped.get_CurrentVehicle().GetPedOnSeat(-1) != this.Ped))
			{
				if (!firstButton())
				{
					this.TargetIndex = 0;
				}
				else
				{
					if (!secondButton())
					{
						this.Targets = this.GetTargets();
					}
					if (this.CanDoAction(PlayerPedAction.SelectTarget, 500))
					{
						Direction direction1 = this.Input.GetDirection(DeviceButton.RightStick);
						if (this.Input.IsDirectionLeft(direction1))
						{
							this.TargetIndex = this.TargetIndex - 1;
							this.UpdateLastAction(PlayerPedAction.SelectTarget);
						}
						if (this.Input.IsDirectionRight(direction1))
						{
							this.TargetIndex = this.TargetIndex + 1;
							this.UpdateLastAction(PlayerPedAction.SelectTarget);
						}
					}
					GTA.Ped ped1 = this.Targets.ElementAtOrDefault<GTA.Ped>(this.TargetIndex);
					if (ped1 == null)
					{
						return;
					}
					if (!ped1.get_IsAlive())
					{
						this.Targets = this.GetTargets();
					}
					if (ped1 != null)
					{
						World.DrawMarker(0, ped1.GetBoneCoord(31086) + new Vector3(0f, 0f, 1f), GameplayCamera.get_Direction(), GameplayCamera.get_Rotation(), new Vector3(1f, 1f, 1f), Color.OrangeRed);
						if (!secondButton())
						{
							this.Ped.get_Task().ClearAll();
						}
						else
						{
							this.SelectWeapon(this.Ped, this.currentset[this.WeaponIndex]);
							if (this.IsThrowable(this.currentset[this.WeaponIndex]))
							{
								if (this.CanDoAction(PlayerPedAction.ThrowTrowable, 1500))
								{
									Function.Call(8252165847224375889L, new InputArgument[] { this.Ped, ped1.get_Position().X, ped1.get_Position().Y, ped1.get_Position().Z });
									this.UpdateLastAction(PlayerPedAction.ThrowTrowable);
								}
							}
							else if (this.CanDoAction(PlayerPedAction.Shoot, 500))
							{
								Function.Call(3425815346453651825L, new InputArgument[] { this.Ped, ped1, 0, 0, 0, 0, 0f, 100, 1, -957453492 });
							}
						}
					}
				}
			}
			else if (firstButton())
			{
				this.SelectWeapon(this.Ped, this.currentset[this.WeaponIndex]);
				if (!this.IsMelee(this.currentset[this.WeaponIndex]))
				{
					if (!this.IsThrowable(this.currentset[this.WeaponIndex]))
					{
						Entity entity = Function.Call<Entity>(4267453750986192380L, new InputArgument[] { this.Ped });
						Vector3 offsetInWorldCoords = entity.GetOffsetInWorldCoords(new Vector3(0f, 0f, 0f));
						Function.Call(7742345298724472448L, new InputArgument[] { offsetInWorldCoords.X, offsetInWorldCoords.Y, offsetInWorldCoords.Z, this.aim.X, this.aim.Y, this.aim.Z, 124, 252, 0, 250 });
					}
					Vector2 rightThumbStick = this.Input.GetState().RightThumbStick;
					if (rightThumbStick != Vector2.get_Zero())
					{
						this.prevAim = rightThumbStick;
						if (((double)rightThumbStick.X > 0.999 ? false : (double)rightThumbStick.X >= -0.999))
						{
							this.polaraim += rightThumbStick.X;
						}
						else
						{
							this.polaraim = this.polaraim + rightThumbStick.X * 3.5f;
						}
						if ((this.polaraim >= 360f ? true : this.polaraim <= -360f))
						{
							this.polaraim = 0f;
						}
						this.azimaim = this.azimaim + rightThumbStick.Y * 1.05f;
						if (this.azimaim >= 179f)
						{
							this.azimaim = 179f;
						}
						if (this.azimaim <= 45f)
						{
							this.azimaim = 45f;
						}
						if (this.azimaim >= 360f)
						{
							this.azimaim = 0f;
						}
					}
					if (this.Input.isPressed(DeviceButton.DPadUp))
					{
						this.azimaim += 0.25f;
					}
					else if (this.Input.isPressed(DeviceButton.DPadDown))
					{
						this.azimaim -= 0.25f;
					}
					else if (this.Input.isPressed(DeviceButton.DPadLeft))
					{
						this.polaraim -= 0.25f;
					}
					else if (this.Input.isPressed(DeviceButton.DPadRight))
					{
						this.polaraim += 0.25f;
					}
					double num = (double)this.polaraim * 3.14159265358979 / 180;
					double num1 = (double)this.azimaim * 3.14159265358979 / 180;
					float x = this.Ped.get_Position().X + 50f * (float)(Math.Sin(num1) * Math.Cos(num));
					float y = this.Ped.get_Position().Y - 50f * (float)(Math.Sin(num1) * Math.Sin(num));
					float z = this.Ped.get_Position().Z - 50f * (float)Math.Cos(num1);
					this.aim = new Vector3(x, y, z);
					if (this.Input.isPressed(DeviceButton.LeftTrigger))
					{
						Vector3 vector3 = this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 10f, 0f));
						try
						{
							this.instTarget = (
								from p in World.GetNearbyPeds(vector3, 10f)
								where this.IsValidTarget(p)
								orderby p.get_Position().DistanceTo(vector3)
								select p).ToArray<GTA.Ped>();
							if (this.instTarget != null)
							{
								this.aim = this.instTarget[0].GetBoneCoord(31086);
							}
						}
						catch
						{
						}
					}
					if (!secondButton())
					{
						Function.Call(-1578961307445201860L, new InputArgument[0]);
						this.Ped.get_Task().AimAt(this.aim, 500);
					}
					else if (this.IsThrowable(this.currentset[this.WeaponIndex]))
					{
						if (this.CanDoAction(PlayerPedAction.ThrowTrowable, 1500))
						{
							this.SelectWeapon(this.Ped, this.currentset[this.WeaponIndex]);
							Function.Call(8252165847224375889L, new InputArgument[] { this.Ped, this.Ped.get_Position().X, this.Ped.get_Position().Y, this.Ped.get_Position().Z });
							this.UpdateLastAction(PlayerPedAction.ThrowTrowable);
						}
					}
					else if (this.currentset[this.WeaponIndex] != 100416529)
					{
						this.Ped.get_Task().ShootAt(this.aim, 500, -957453492);
					}
					else
					{
						Function.Call(1954045745482663201L, new InputArgument[] { 1.5 });
						this.Ped.get_Task().ShootAt(this.aim, 500, -957453492);
					}
					if (this.Ped.get_IsOnScreen())
					{
						Vector3 boneCoord1 = this.Ped.GetBoneCoord(31086);
						boneCoord1.Z += 0.3f;
						boneCoord1.X += 0.1f;
						UIRectangle uIRectangle1 = new UIRectangle(UI.WorldToScreen(boneCoord1), new Size(this.MaxHealth / 2, 5), Color.Black);
						uIRectangle1.Draw();
						uIRectangle1.set_Size(new Size(this.Ped.get_Health() / 2, 5));
						uIRectangle1.set_Color(Color.LimeGreen);
						uIRectangle1.Draw();
					}
				}
				else
				{
					if (!secondButton())
					{
						this.Ped.get_Task().ClearAll();
						this.Targets = this.GetMeleeTargets();
					}
					if (this.Targets.ElementAtOrDefault<GTA.Ped>(this.TargetIndex) != null)
					{
						GTA.Ped ped2 = this.Targets.ElementAtOrDefault<GTA.Ped>(this.TargetIndex);
						World.DrawMarker(0, ped2.GetBoneCoord(31086) + new Vector3(0f, 0f, 1f), GameplayCamera.get_Direction(), GameplayCamera.get_Rotation(), new Vector3(1f, 1f, 1f), Color.OrangeRed);
						if (this.CanDoAction(PlayerPedAction.SelectTarget, 400))
						{
							Direction direction2 = this.Input.GetDirection(DeviceButton.RightStick);
							if (this.Input.IsDirectionLeft(direction2))
							{
								this.TargetIndex = this.TargetIndex - 1;
								this.UpdateLastAction(PlayerPedAction.SelectTarget);
							}
							if (this.Input.IsDirectionRight(direction2))
							{
								this.TargetIndex = this.TargetIndex + 1;
								this.UpdateLastAction(PlayerPedAction.SelectTarget);
							}
						}
						if (!secondButton())
						{
							this.Ped.get_Task().ClearAll();
						}
						else
						{
							this.Ped.get_Task().FightAgainst(ped2, -1);
							Function.Call(119513419683673053L, new InputArgument[] { this.Ped, 3f });
						}
					}
				}
			}
			else if (secondButton())
			{
				this.instaim.X = this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 8f, -0.2f)).X;
				this.instaim.Y = this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 8f, -0.2f)).Y;
				if ((!this.Input.isPressed(DeviceButton.DPadUp) ? false : this.instaim.Z < this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 8f, -0.2f)).Z + 5f))
				{
					this.instaim.Z += 1f;
				}
				else if ((!this.Input.isPressed(DeviceButton.DPadDown) ? false : this.instaim.Z > this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 8f, -0.2f)).Z + -5f))
				{
					this.instaim.Z -= 1f;
				}
				this.Ped.get_Task().ShootAt(this.instaim, 500, -957453492);
			}
			else if (!secondButton())
			{
				this.instaim.Z = this.Ped.GetOffsetInWorldCoords(new Vector3(0f, 8f, -0.2f)).Z;
			}
		}

		private void Updatedrive(Vehicle v)
		{
			Vector2 leftThumbStick = this.Input.GetState().LeftThumbStick;
			if (this.Input.isPressed(DeviceButton.RightTrigger))
			{
				if (v.get_Model().get_IsPlane())
				{
					Vector2 vector2 = this.Input.GetState().LeftThumbStick;
					Vector3 offsetInWorldCoords = v.GetOffsetInWorldCoords(new Vector3(0f, 3f, 0f));
					Function.Call(2553607858489408392L, new InputArgument[] { this.Ped, v, 0, 0, offsetInWorldCoords.X, offsetInWorldCoords.Y, offsetInWorldCoords.Z, 4, 100f, 0f, 90f, 0, -5000f });
				}
				else if (!this.Input.isPressed(DeviceButton.A))
				{
					Function.Call(-4311672250463297239L, new InputArgument[] { this.Ped, v, 32, -1 });
				}
				else
				{
					Function.Call(-4311672250463297239L, new InputArgument[] { this.Ped, v, 9, -1 });
				}
			}
			else if (!v.get_Model().get_IsPlane())
			{
				Function.Call(-4311672250463297239L, new InputArgument[] { this.Ped, v, 1, -1 });
			}
			else if ((!v.get_IsOnAllWheels() ? false : !this.Input.isPressed(DeviceButton.LeftShoulder)))
			{
				Function.Call(2553607858489408392L, new InputArgument[] { this.Ped, v, 0, 0, v.get_Position().X, v.get_Position().Y, v.get_Position().Z, 5, 0f, 0f, 90f, 0, -5000f });
			}
		}

		private void UpdateFoot()
		{
			try
			{
				this.UpdateCombat(() => this.Input.isPressed(DeviceButton.LeftShoulder), () => this.Input.isPressed(DeviceButton.RightShoulder));
			}
			catch (Exception exception)
			{
			}
			Vector2 leftThumbStick = this.Input.GetState().LeftThumbStick;
			if (!this.jump)
			{
				if (this.Input.isPressed(DeviceButton.LeftStick))
				{
					this.prevState = leftThumbStick;
					this.dest = Game.get_Player().get_Character().get_Position();
					if (!this.Input.isPressed(DeviceButton.A))
					{
						this.Ped.get_Task().GoTo(this.dest, true, -1);
					}
					else
					{
						Function.Call(-2924147101113880693L, new InputArgument[] { this.Ped, this.dest.X, this.dest.Y, this.dest.Z, 4f, -1, 0f, 0f });
						Function.Call(119513419683673053L, new InputArgument[] { this.Ped, 3f });
					}
					this.resetWalking = true;
				}
				else if (leftThumbStick != Vector2.get_Zero())
				{
					if (TwoPlayerMod.customCamera)
					{
						this.dest = this.Ped.get_Position() - TwoPlayerMod.SA_Corrector(new Vector3(leftThumbStick.X * 10f, leftThumbStick.Y * 10f, 0f));
					}
					else if (TwoPlayerMod.splitmode)
					{
						this.prevDest = this.Ped.get_Rotation();
						this.Ped.set_Rotation(TwoPlayerMod.vcam.get_Rotation());
						this.dest = this.Ped.GetOffsetInWorldCoords(new Vector3(leftThumbStick.X * 25f, leftThumbStick.Y * 25f, 0f));
						this.Ped.set_Rotation(this.prevDest);
						this.prevState = leftThumbStick;
					}
					else if (((float)Math.Abs(leftThumbStick.X - this.prevState.X) >= 0.02f ? true : (float)Math.Abs(leftThumbStick.Y - this.prevState.Y) >= 0.02f))
					{
						this.prevDest = this.Ped.get_Rotation();
						this.Ped.set_Rotation(this.NC_Get_Cam_Rotation());
						this.dest = this.Ped.GetOffsetInWorldCoords(new Vector3(leftThumbStick.X * 25f, leftThumbStick.Y * 25f, 0f));
						this.Ped.set_Rotation(this.prevDest);
						this.prevState = leftThumbStick;
					}
					if (!this.Input.isPressed(DeviceButton.LeftShoulder))
					{
						if (!this.Input.isPressed(DeviceButton.A))
						{
							this.Ped.get_Task().GoTo(this.dest, true, -1);
						}
						else
						{
							Function.Call(-2924147101113880693L, new InputArgument[] { this.Ped, this.dest.X, this.dest.Y, this.dest.Z, 4f, -1, 0f, 0f });
							Function.Call(119513419683673053L, new InputArgument[] { this.Ped, 3f });
						}
					}
					this.resetWalking = true;
				}
				else if (this.resetWalking)
				{
					this.Ped.get_Task().ClearAll();
					this.resetWalking = false;
					this.prevState = Vector2.get_Zero();
				}
			}
			if ((!this.Input.isPressed(DeviceButton.X) ? false : !this.Ped.get_IsInParachuteFreeFall()))
			{
				if (this.Ped.get_IsWalking())
				{
					this.Ped.get_Task().Climb();
					this.carflip = DateTime.Now + TimeSpan.FromMilliseconds(1000);
					this.jump = true;
				}
				else
				{
					if (DateTime.Now < this.carflip)
					{
						return;
					}
					Function.Call(-5275535984294217708L, new InputArgument[] { this.Ped });
					this.carflip = DateTime.Now + TimeSpan.FromMilliseconds(2000);
				}
			}
			if ((!this.Ped.get_IsVaulting() || !this.Ped.get_IsClimbing() ? false : DateTime.Now > this.carflip))
			{
				this.Ped.get_Task().ClearAllImmediately();
			}
			else if ((DateTime.Now <= this.carflip ? false : this.jump))
			{
				this.jump = false;
			}
		}

		private void UpdateLastAction(PlayerPedAction action)
		{
			this.lastActions[action] = Game.get_GameTime();
		}

		private bool UpdateWeaponIndex()
		{
			this.WeaponIndex = this.WeaponIndex + 1;
			return true;
		}
	}
}