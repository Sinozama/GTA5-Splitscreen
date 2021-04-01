using Benjamin94;
using Benjamin94.Input;
using GTA;
using GTA.Math;
using GTA.Native;
using Microsoft.CSharp.RuntimeBinder;
using NativeUI;
using SharpDX.DirectInput;
using SharpDX.XInput;
using Split;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

internal class TwoPlayerMod : Script
{
	public static string ScriptName;

	private const string ToggleMenuKey = "ToggleMenuKey";

	private const string CharacterHashKey = "CharacterHash";

	private const string ControllerKey = "Controller";

	private const string CustomCameraKey = "CustomCamera";

	private const string BlipSpriteKey = "BlipSprite";

	private const string BlipColorKey = "BlipColor";

	private const string EnabledKey = "Enabled";

	private Player player;

	public static Ped player1;

	public static List<PlayerPed> playerPeds;

	private UIMenu menu;

	private MenuPool menuPool = new MenuPool();

	private Keys toggleMenuKey = Keys.F11;

	private static bool respawn;

	public static bool customCamera;

	private Camera camera;

	public static TwoPlayerMod.SA_Direction CamDirection;

	public static bool splitmode;

	public static Ped secondP;

	public static Camera vcam;

	private static Overlay fsplit;

	public static Split.Splitter splitHandle;

	public static float _polarAngleDeg;

	public static float _azimuthAngleDeg;

	public static float _radius;

	public static bool samecar;

	private static bool died;

	private readonly UserIndex[] userIndices = new UserIndex[] { typeof(<PrivateImplementationDetails>).GetField("7037807198C22A7D2B0807371D763779A84FDFCF").FieldHandle };

	private bool resetWalking = false;

	static TwoPlayerMod()
	{
		TwoPlayerMod.ScriptName = "TwoPlayerMod";
		TwoPlayerMod.playerPeds = new List<PlayerPed>();
		TwoPlayerMod.respawn = true;
		TwoPlayerMod.customCamera = false;
		TwoPlayerMod.CamDirection = TwoPlayerMod.SA_Direction.South;
		TwoPlayerMod.splitmode = false;
		TwoPlayerMod.fsplit = null;
		TwoPlayerMod.splitHandle = new Split.Splitter();
		TwoPlayerMod._polarAngleDeg = 0f;
		TwoPlayerMod._azimuthAngleDeg = 90f;
		TwoPlayerMod._radius = 3.5f;
		TwoPlayerMod.samecar = false;
		TwoPlayerMod.died = false;
	}

	public TwoPlayerMod()
	{
		TwoPlayerMod.ScriptName = base.get_Name();
		this.player = Game.get_Player();
		TwoPlayerMod.player1 = this.player.get_Character();
		TwoPlayerMod.player1.get_Task().ClearAll();
		this.LoadSettings();
		this.SetupMenu();
		base.add_KeyDown(new KeyEventHandler(this.TwoPlayerMod_KeyDown));
		base.add_Tick(new EventHandler(this.TwoPlayerMod_Tick));
	}

	public Vector3 CenterOfVectors(params Vector3[] vectors)
	{
		Vector3 zero = Vector3.get_Zero();
		Vector3[] vector3Array = vectors;
		for (int i = 0; i < (int)vector3Array.Length; i++)
		{
			zero += vector3Array[i];
		}
		return zero / (float)((int)vectors.Length);
	}

	private void Clean()
	{
		Function.Call(-9114364950456553926L, new InputArgument[0]);
		this.CleanCamera();
		this.CleanUpPlayerPeds();
	}

	private void CleanCamera()
	{
		World.DestroyAllCameras();
		World.set_RenderingCamera(null);
	}

	private void CleanUpPlayerPeds()
	{
		TwoPlayerMod.playerPeds.ForEach((PlayerPed playerPed) => playerPed.Clean());
		TwoPlayerMod.playerPeds.Clear();
		if (TwoPlayerMod.player1 != null)
		{
			TwoPlayerMod.player1.get_Task().ClearAllImmediately();
		}
	}

	private UIMenuListItem ConstructSettingsListItem<TEnum>(UserIndex player, string text, string description, string settingsKey, TEnum defaultValue)
	{
		TwoPlayerMod.<>c__DisplayClass35_0<TEnum> variable = null;
		List<object> dynamicEnumList = this.GetDynamicEnumList<TEnum>();
		TEnum enumValue = PlayerSettings.GetEnumValue<TEnum>(player, settingsKey, defaultValue.ToString());
		UIMenuListItem uIMenuListItem = new UIMenuListItem(text, dynamicEnumList, dynamicEnumList.IndexOf(enumValue.ToString()), description);
		uIMenuListItem.add_OnListChanged(new ItemListEvent(variable, (UIMenuListItem s, int i) => {
			if (s.get_Enabled())
			{
				if (TwoPlayerMod.<>o__35<TEnum>.<>p__1 == null)
				{
					TwoPlayerMod.<>o__35<TEnum>.<>p__1 = CallSite<Func<CallSite, object, TEnum>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(TEnum), typeof(TwoPlayerMod)));
				}
				!0 target = TwoPlayerMod.<>o__35<TEnum>.<>p__1.Target;
				CallSite<Func<CallSite, object, TEnum>> u003cu003ep_1 = TwoPlayerMod.<>o__35<TEnum>.<>p__1;
				if (TwoPlayerMod.<>o__35<TEnum>.<>p__0 == null)
				{
					TwoPlayerMod.<>o__35<TEnum>.<>p__0 = CallSite<Func<CallSite, Type, Type, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(CSharpBinderFlags.None, "Parse", null, typeof(TwoPlayerMod), (IEnumerable<CSharpArgumentInfo>)(new CSharpArgumentInfo[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) })));
				}
				TEnum tEnum = target(u003cu003ep_1, TwoPlayerMod.<>o__35<TEnum>.<>p__0.Target(TwoPlayerMod.<>o__35<TEnum>.<>p__0, typeof(Enum), typeof(TEnum), s.IndexToItem(s.get_Index())));
				PlayerSettings.SetValue(this.player, this.settingsKey, tEnum.ToString());
			}
		}));
		return uIMenuListItem;
	}

	protected override void Dispose(bool A_0)
	{
		this.Clean();
		base.Dispose(A_0);
	}

	private bool Enabled()
	{
		return TwoPlayerMod.playerPeds.Count > 0;
	}

	private List<dynamic> GetDynamicEnumList<TEnum>()
	{
		List<object> objs = new List<object>();
		foreach (Enum value in Enum.GetValues(typeof(TEnum)))
		{
			objs.Add(value.ToString());
		}
		objs.Sort();
		return objs;
	}

	private string GetIniFile()
	{
		return string.Concat("scripts//", base.get_Name(), ".ini");
	}

	public static void HandleEnterVehicle(Ped ped)
	{
		Vehicle closestVehicle = World.GetClosestVehicle(ped.get_Position(), 15f);
		if (closestVehicle != null)
		{
			Ped pedOnSeat = closestVehicle.GetPedOnSeat(-1);
			if (closestVehicle.get_Model().get_IsBike())
			{
				closestVehicle.PlaceOnGround();
			}
			if ((pedOnSeat == null ? true : !pedOnSeat.IsPlayerPed()))
			{
				if (pedOnSeat != null)
				{
					ped.get_Task().FightAgainst(pedOnSeat);
				}
				ped.get_Task().EnterVehicle(closestVehicle, -2);
			}
			else
			{
				ped.get_Task().EnterVehicle(closestVehicle, -2);
			}
		}
	}

	private void LoadSettings()
	{
		Keys key;
		try
		{
			key = Keys.F11;
			this.toggleMenuKey = (Keys)(new KeysConverter()).ConvertFromString(base.get_Settings().GetValue(base.get_Name(), "ToggleMenuKey", key.ToString()));
		}
		catch (Exception exception)
		{
			this.toggleMenuKey = Keys.F11;
			UI.Notify("Failed to read 'ToggleMenuKey', reverting to default ToggleMenuKey F11");
			key = Keys.F11;
			base.get_Settings().SetValue(base.get_Name(), "ToggleMenuKey", key.ToString());
			base.get_Settings().Save();
		}
		try
		{
			TwoPlayerMod.customCamera = bool.Parse(base.get_Settings().GetValue(base.get_Name(), "CustomCamera", "False"));
		}
		catch (Exception exception1)
		{
			TwoPlayerMod.customCamera = false;
			UI.Notify("Failed to read 'CustomCamera', reverting to default CustomCamera False");
			base.get_Settings().SetValue(base.get_Name(), "CustomCamera", "False");
			base.get_Settings().Save();
		}
		try
		{
			TwoPlayerMod.respawn = bool.Parse(base.get_Settings().GetValue(base.get_Name(), "RESPAWN", "True"));
		}
		catch (Exception exception2)
		{
			TwoPlayerMod.respawn = true;
			base.get_Settings().SetValue(base.get_Name(), "RESPAWN", "True");
			base.get_Settings().Save();
		}
	}

	private bool Player1Available()
	{
		bool flag;
		bool flag1;
		if (!TwoPlayerMod.player1.get_IsDead())
		{
			if (!Function.Call<bool>(4074147724792802446L, new InputArgument[] { this.player, true }))
			{
				if (Function.Call<bool>(4074147724792802446L, new InputArgument[] { this.player, false }))
				{
					flag1 = false;
					flag = flag1;
					return flag;
				}
				flag1 = !Function.Call<bool>(8753528501838783119L, new InputArgument[0]);
				flag = flag1;
				return flag;
			}
		}
		flag1 = false;
		flag = flag1;
		return flag;
	}

	private void RefreshSubItems(UIMenuItem parentItem, UIMenu menu, bool enabled)
	{
		foreach (UIMenuItem menuItem in menu.MenuItems)
		{
			if (!menuItem.Equals(parentItem))
			{
				menuItem.set_Enabled(enabled);
			}
		}
	}

	public static Vector3 SA_Corrector(Vector3 offset)
	{
		float x;
		switch (TwoPlayerMod.CamDirection)
		{
			case TwoPlayerMod.SA_Direction.North:
			{
				offset.X = -offset.X;
				offset.Y = -offset.Y;
				break;
			}
			case TwoPlayerMod.SA_Direction.East:
			{
				x = offset.X;
				offset.X = -offset.Y;
				offset.Y = x;
				break;
			}
			case TwoPlayerMod.SA_Direction.West:
			{
				x = offset.X;
				offset.X = offset.Y;
				offset.Y = -x;
				break;
			}
		}
		return offset;
	}

	private void SetupCamera()
	{
		this.camera = World.CreateCamera(TwoPlayerMod.player1.GetOffsetInWorldCoords(new Vector3(0f, 10f, 10f)), Vector3.get_Zero(), GameplayCamera.get_FieldOfView());
	}

	private void SetupMenu()
	{
		TwoPlayerMod.<>c__DisplayClass33_0 variable = null;
		TwoPlayerMod.<>c__DisplayClass33_1 variable1 = null;
		TwoPlayerMod.<>c__DisplayClass33_2 variable2 = null;
		TwoPlayerMod.<>c__DisplayClass33_3 variable3 = null;
		if (this.menuPool != null)
		{
			this.menuPool.ToList().ForEach((UIMenu menu) => menu.Clear());
		}
		this.menu = new UIMenu("Two Player Mod", (this.Enabled() ? "~g~Enabled" : "~r~Disabled"));
		this.menuPool.Add(this.menu);
		UIMenuItem uIMenuItem = new UIMenuItem("Toggle mod", "Toggle Two Player mode");
		uIMenuItem.add_Activated(new ItemActivatedEvent(this, TwoPlayerMod.ToggleMod_Activated));
		this.menu.AddItem(uIMenuItem);
		UIMenu uIMenu = this.menuPool.AddSubMenu(this.menu, "Players");
		this.menu.MenuItems.FirstOrDefault<UIMenuItem>((UIMenuItem item) => item.get_Text().Equals("Players")).set_Description("Configure players");
		UserIndex[] userIndexArray = this.userIndices;
		for (int num = 0; num < (int)userIndexArray.Length; num++)
		{
			UserIndex userIndex = userIndexArray[num];
			bool flag = false;
			bool flag1 = bool.Parse(PlayerSettings.GetValue(userIndex, "Enabled", flag.ToString()));
			UIMenu uIMenu1 = this.menuPool.AddSubMenu(uIMenu, string.Concat("Player ", userIndex));
			UIMenuItem uIMenuItem1 = uIMenu.MenuItems.FirstOrDefault<UIMenuItem>((UIMenuItem item) => item.get_Text().Equals(string.Concat("Player ", this.player)));
			string value = PlayerSettings.GetValue(userIndex, "Controller", "");
			uIMenuItem1.set_Description(string.Concat("Configure player ", userIndex));
			if (!string.IsNullOrEmpty(value))
			{
				uIMenuItem1.SetRightBadge(18);
			}
			UIMenuCheckboxItem uIMenuCheckboxItem = new UIMenuCheckboxItem(string.Concat("Toggle player ", userIndex), flag1, "Enables/disables this player");
			uIMenuCheckboxItem.add_CheckboxEvent(new ItemCheckboxEvent(variable1, (UIMenuCheckboxItem s, bool enabled) => {
				PlayerSettings.SetValue(this.CS$<>8__locals1.player, "Enabled", enabled.ToString());
				this.CS$<>8__locals1.<>4__this.RefreshSubItems(this.togglePlayerItem, this.playerMenu, enabled);
			}));
			UIMenuCheckboxItem uIMenuCheckboxItem1 = new UIMenuCheckboxItem("God Mode", flag1, "Turn On/Off God Mode for this player");
			uIMenuCheckboxItem1.add_CheckboxEvent(new ItemCheckboxEvent(variable, (UIMenuCheckboxItem s, bool enabled) => PlayerSettings.SetValue(this.player, "INVINCIBLE", enabled.ToString())));
			uIMenu1.AddItem(uIMenuCheckboxItem);
			uIMenu1.AddItem(uIMenuCheckboxItem1);
			uIMenu1.AddItem(this.ConstructSettingsListItem<PedHash>(userIndex, "Character", string.Concat("Select a character for player ", userIndex), "CharacterHash", -1686040670));
			uIMenu1.AddItem(this.ConstructSettingsListItem<BlipSprite>(userIndex, "Blip sprite", string.Concat("Select a blip sprite for player ", userIndex), "BlipSprite", 1));
			uIMenu1.AddItem(this.ConstructSettingsListItem<BlipColor>(userIndex, "Blip color", string.Concat("Select a blip color for player ", userIndex), "BlipColor", 2));
			UIMenu uIMenu2 = this.menuPool.AddSubMenu(uIMenu1, "Assign controller");
			uIMenu1.MenuItems.FirstOrDefault<UIMenuItem>((UIMenuItem item) => item.get_Text().Equals("Assign controller")).set_Description(string.Concat("Assign controller to player ", userIndex));
			foreach (InputManager availableInputManager in InputManager.GetAvailableInputManagers())
			{
				UIMenuItem uIMenuItem2 = new UIMenuItem(availableInputManager.DeviceName, string.Concat("Assign this controller to player ", userIndex));
				string deviceGuid = availableInputManager.DeviceGuid;
				if (PlayerSettings.GetValue(userIndex, "Controller", "").Equals(deviceGuid))
				{
					uIMenuItem2.SetRightBadge(18);
				}
				if (availableInputManager is DirectInputManager)
				{
					DirectInputManager directInputManager = (DirectInputManager)availableInputManager;
					bool flag2 = DirectInputManager.IsConfigured(directInputManager.device, this.GetIniFile());
					uIMenuItem2.set_Enabled(flag2);
					if (!flag2)
					{
						uIMenuItem2.set_Description("Please configure this controller first from the main menu");
					}
				}
				uIMenuItem2.add_Activated(new ItemActivatedEvent(variable2, (UIMenu s, UIMenuItem i) => {
					if (i.get_Enabled())
					{
						PlayerSettings.SetValue(this.CS$<>8__locals2.CS$<>8__locals1.player, "Controller", this.guid);
						List<UIMenuItem> menuItems = this.CS$<>8__locals2.controllerMenu.MenuItems;
						Action<UIMenuItem> u003cu003e9_7 = this.<>9__7;
						if (u003cu003e9_7 == null)
						{
							Action<UIMenuItem> action = (UIMenuItem item) => {
								if ((object)item != (object)this.controllerItem)
								{
									item.SetRightBadge(0);
								}
								else
								{
									item.SetRightBadge(18);
								}
							};
							Action<UIMenuItem> action1 = action;
							this.<>9__7 = action;
							u003cu003e9_7 = action1;
						}
						menuItems.ForEach(u003cu003e9_7);
					}
				}));
				uIMenu2.AddItem(uIMenuItem2);
			}
			this.RefreshSubItems(uIMenuCheckboxItem, uIMenu1, flag1);
		}
		UIMenuCheckboxItem uIMenuCheckboxItem2 = new UIMenuCheckboxItem("Respawn", TwoPlayerMod.respawn, "Turn off to respawn at hospital upon death");
		uIMenuCheckboxItem2.add_CheckboxEvent(new ItemCheckboxEvent(this, (UIMenuCheckboxItem s, bool i) => {
			TwoPlayerMod.respawn = !TwoPlayerMod.respawn;
			base.get_Settings().SetValue(base.get_Name(), "RESPAWN", TwoPlayerMod.respawn.ToString());
			base.get_Settings().Save();
		}));
		this.menu.AddItem(uIMenuCheckboxItem2);
		UIMenuCheckboxItem uIMenuCheckboxItem3 = new UIMenuCheckboxItem("GTA:SA style camera", TwoPlayerMod.customCamera, "Enables/disables the GTA:SA style camera");
		uIMenuCheckboxItem3.add_CheckboxEvent(new ItemCheckboxEvent(this, (UIMenuCheckboxItem s, bool i) => {
			TwoPlayerMod.customCamera = !TwoPlayerMod.customCamera;
			base.get_Settings().SetValue(base.get_Name(), "CustomCamera", TwoPlayerMod.customCamera.ToString());
			base.get_Settings().Save();
		}));
		this.menu.AddItem(uIMenuCheckboxItem3);
		UIMenu uIMenu3 = this.menuPool.AddSubMenu(this.menu, "Configure controllers");
		this.menu.MenuItems.FirstOrDefault<UIMenuItem>((UIMenuItem item) => item.get_Text().Equals("Configure controllers")).set_Description("Configure controllers before assigning them to players");
		foreach (Joystick device in DirectInputManager.GetDevices())
		{
			UIMenuItem uIMenuItem3 = new UIMenuItem(device.get_Information().ProductName, string.Concat("Configure ", device.get_Information().ProductName));
			uIMenu3.AddItem(uIMenuItem3);
			uIMenuItem3.add_Activated(new ItemActivatedEvent(variable3, (UIMenu s, UIMenuItem i) => {
				if (!(new ControllerWizard(this.stick)).StartConfiguration(this.<>4__this.GetIniFile()))
				{
					UI.Notify("Controller configuration canceled, please configure your controller before playing");
				}
				else
				{
					UI.Notify("Controller successfully configured, you can now assign this controller");
				}
				this.<>4__this.SetupMenu();
			}));
		}
		this.menu.RefreshIndex();
	}

	private void SetupPlayerPeds()
	{
		UserIndex[] userIndexArray = this.userIndices;
		for (int i = 0; i < (int)userIndexArray.Length; i++)
		{
			UserIndex userIndex = userIndexArray[i];
			if (bool.Parse(PlayerSettings.GetValue(userIndex, "Enabled", false.ToString())))
			{
				string value = PlayerSettings.GetValue(userIndex, "Controller", "");
				foreach (InputManager availableInputManager in InputManager.GetAvailableInputManagers())
				{
					if (availableInputManager.DeviceGuid.Equals(value))
					{
						InputManager inputManager = availableInputManager;
						if (availableInputManager is DirectInputManager)
						{
							inputManager = DirectInputManager.LoadConfig(((DirectInputManager)availableInputManager).device, this.GetIniFile());
						}
						PedHash pedHash = -1686040670;
						PedHash enumValue = PlayerSettings.GetEnumValue<PedHash>(userIndex, "CharacterHash", pedHash.ToString());
						BlipSprite blipSprite = 1;
						BlipSprite enumValue1 = PlayerSettings.GetEnumValue<BlipSprite>(userIndex, "BlipSprite", blipSprite.ToString());
						BlipColor blipColor = 2;
						BlipColor blipColor1 = PlayerSettings.GetEnumValue<BlipColor>(userIndex, "BlipColor", blipColor.ToString());
						bool flag = false;
						bool flag1 = bool.Parse(PlayerSettings.GetValue(userIndex, "INVINCIBLE", flag.ToString()));
						PlayerPed playerPed = new PlayerPed(userIndex, enumValue, enumValue1, blipColor1, TwoPlayerMod.player1, inputManager, flag1);
						TwoPlayerMod.playerPeds.Add(playerPed);
						break;
					}
				}
			}
		}
	}

	public static void SplitUpdater()
	{
		UI.ShowSubtitle("Split");
	}

	public static void StartOverlay()
	{
		TwoPlayerMod.fsplit = new Overlay();
		Application.Run(TwoPlayerMod.fsplit);
	}

	private void ToggleMod_Activated(UIMenu sender, UIMenuItem selectedItem)
	{
		if (this.Enabled())
		{
			this.Clean();
		}
		else
		{
			UI.ShowSubtitle("Like this mod? Please consider donating!", 10000);
			this.SetupPlayerPeds();
			if (TwoPlayerMod.playerPeds.Count == 0)
			{
				UI.Notify("Please assign a controller to at least one player");
				UI.Notify("Also make sure you have configured at least one controller");
				return;
			}
			this.SetupCamera();
		}
		this.menu.get_Subtitle().set_Caption((this.Enabled() ? "~g~Enabled" : "~r~Disabled"));
	}

	private void TwoPlayerMod_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == this.toggleMenuKey)
		{
			this.menu.set_Visible(!this.menu.get_Visible());
		}
		if (e.KeyCode == Keys.T)
		{
			try
			{
				TwoPlayerMod.secondP = World.GetNearbyPeds(Game.get_Player().get_Character(), 8f)[0];
			}
			catch
			{
				TwoPlayerMod.secondP = Game.get_Player().get_Character();
			}
			TwoPlayerMod.vcam = World.CreateCamera(TwoPlayerMod.secondP.get_Position(), Vector3.get_Zero(), 65f);
			(new Thread(new ThreadStart(TwoPlayerMod.StartOverlay))).Start();
			TwoPlayerMod.splitmode = true;
		}
		if (e.KeyCode == Keys.R)
		{
			TwoPlayerMod.fsplit.Dispose();
			TwoPlayerMod.splitmode = false;
			World.set_RenderingCamera(null);
		}
		if (e.KeyCode == Keys.E)
		{
			Split.Splitter.mrse.Set();
		}
	}

	private void TwoPlayerMod_Tick(object sender, EventArgs e)
	{
		this.menuPool.ProcessMenus();
		if (this.Enabled())
		{
			if ((!TwoPlayerMod.player1.get_IsDead() ? false : TwoPlayerMod.respawn))
			{
				Function.Call(-7077668788463384353L, new InputArgument[] { "respawn_controller" });
				Function.Call(2449877097777288033L, new InputArgument[] { true });
				Function.Call(3182695371859369073L, new InputArgument[] { true });
				Function.Call(-5409425576074413179L, new InputArgument[0]);
				Function.Call(5339263777479621510L, new InputArgument[] { false });
				TwoPlayerMod.player1.set_Health(1);
				Function.Call(-5290859026539076793L, new InputArgument[] { TwoPlayerMod.player1 });
				Function.Call(-4563742946046569651L, new InputArgument[0]);
				Function.Call(3243683805626065110L, new InputArgument[] { Game.get_Player() });
				TwoPlayerMod.died = true;
			}
			if ((!TwoPlayerMod.died ? false : TwoPlayerMod.respawn))
			{
				if (Game.IsControlPressed(0, 22))
				{
					Function.Call(-1575199258904908549L, new InputArgument[] { TwoPlayerMod.player1.get_Position().X, TwoPlayerMod.player1.get_Position().Y, TwoPlayerMod.player1.get_Position().Z, 0, 0, 0 });
					Function.Call(-4563742946046569651L, new InputArgument[0]);
					Function.Call(3243683805626065110L, new InputArgument[] { Game.get_Player() });
					Function.Call(-6473562613796048854L, new InputArgument[] { true });
					TwoPlayerMod.player1.set_FreezePosition(false);
					TwoPlayerMod.player1.set_Health(TwoPlayerMod.player1.get_MaxHealth());
					TwoPlayerMod.died = false;
				}
			}
			if (!this.Player1Available())
			{
				this.CleanCamera();
				while (true)
				{
					if ((!this.Player1Available() ? false : !Function.Call<bool>(6653025866621830517L, new InputArgument[0])))
					{
						break;
					}
					Script.Wait(1000);
				}
				TwoPlayerMod.playerPeds.ForEach((PlayerPed playerPed) => {
					playerPed.Respawn();
					Script.Wait(500);
				});
				this.SetupCamera();
			}
			TwoPlayerMod.playerPeds.ForEach((PlayerPed playerPed) => playerPed.Tick());
			if (Split.Splitter.cycle == 4)
			{
				Split.Splitter.cycle = 1;
				Split.Splitter.mrse.Set();
			}
			if (TwoPlayerMod.splitmode)
			{
				TwoPlayerMod.splitHandle.Tick(TwoPlayerMod.secondP, TwoPlayerMod._radius, TwoPlayerMod._polarAngleDeg, TwoPlayerMod._azimuthAngleDeg);
			}
			if (!TwoPlayerMod.splitmode)
			{
				this.UpdateCamera();
			}
			if ((!TwoPlayerMod.customCamera ? false : TwoPlayerMod.player1.get_IsOnFoot()))
			{
				if (Game.IsControlPressed(0, 22))
				{
					TwoPlayerMod.player1.get_Task().Climb();
				}
				Vector2 zero = Vector2.get_Zero();
				if (Game.IsControlPressed(0, 32))
				{
					zero.Y += 1f;
				}
				if (Game.IsControlPressed(0, 34))
				{
					zero.X -= 1f;
				}
				if (Game.IsControlPressed(0, 35))
				{
					zero.X += 1f;
				}
				if (Game.IsControlPressed(0, 33))
				{
					zero.Y -= 1f;
				}
				if (zero != Vector2.get_Zero())
				{
					Vector3 position = Vector3.get_Zero();
					position = TwoPlayerMod.player1.get_Position() - TwoPlayerMod.SA_Corrector(new Vector3(zero.X * 10f, zero.Y * 10f, 0f));
					if (!Game.IsControlPressed(0, 21))
					{
						TwoPlayerMod.player1.get_Task().GoTo(position, true, -1);
					}
					else
					{
						TwoPlayerMod.player1.get_Task().RunTo(position, true, -1);
					}
					this.resetWalking = true;
				}
				else if (this.resetWalking)
				{
					TwoPlayerMod.player1.get_Task().ClearAll();
					this.resetWalking = false;
				}
			}
			if ((!TwoPlayerMod.player1.get_IsOnFoot() ? false : Game.IsControlJustReleased(0, 75)))
			{
				TwoPlayerMod.HandleEnterVehicle(TwoPlayerMod.player1);
			}
		}
	}

	private void UpdateCamera()
	{
		if (TwoPlayerMod.playerPeds.TrueForAll((PlayerPed p) => (p.Ped.get_CurrentVehicle() == null ? false : p.Ped.get_CurrentVehicle() == TwoPlayerMod.player1.get_CurrentVehicle())))
		{
			World.set_RenderingCamera(null);
			Function.Call(-9114364950456553926L, new InputArgument[] { 0 });
		}
		else if (!TwoPlayerMod.customCamera)
		{
			World.set_RenderingCamera(null);
			Function.Call(-9114364950456553926L, new InputArgument[] { 0 });
		}
		else
		{
			Function.Call(2999307995311693915L, new InputArgument[] { 0 });
			PlayerPed playerPed1 = (
				from playerPed in TwoPlayerMod.playerPeds
				orderby TwoPlayerMod.player1.get_Position().DistanceTo(playerPed.Ped.get_Position()) descending
				select playerPed).FirstOrDefault<PlayerPed>();
			Vector3 vector3 = this.CenterOfVectors(new Vector3[] { TwoPlayerMod.player1.get_Position(), playerPed1.Ped.get_Position() });
			World.set_RenderingCamera(this.camera);
			this.camera.PointAt(vector3);
			Vector3 position = playerPed1.Ped.get_Position();
			float single = position.DistanceTo(TwoPlayerMod.player1.get_Position());
			switch (TwoPlayerMod.CamDirection)
			{
				case TwoPlayerMod.SA_Direction.North:
				{
					ref float y = ref vector3.Y;
					y = y - (5f + single / 1.6f);
					break;
				}
				case TwoPlayerMod.SA_Direction.South:
				{
					ref float singlePointer = ref vector3.Y;
					singlePointer = singlePointer + (5f + single / 1.6f);
					break;
				}
				case TwoPlayerMod.SA_Direction.East:
				{
					ref float x = ref vector3.X;
					x = x - (5f + single / 1.6f);
					break;
				}
				case TwoPlayerMod.SA_Direction.West:
				{
					ref float x1 = ref vector3.X;
					x1 = x1 + (5f + single / 1.6f);
					break;
				}
			}
			ref float z = ref vector3.Z;
			z = z + (2f + single / 1.4f);
			this.camera.set_Position(vector3);
		}
	}

	public enum SA_Direction
	{
		North,
		South,
		East,
		West
	}
}