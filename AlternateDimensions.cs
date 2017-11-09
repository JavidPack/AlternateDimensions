using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static AlternateDimensions.AlternateDimensionsWorld;

namespace AlternateDimensions
{
	//Potention Problem in Projectile.Update: if (this.position.X <= Main.leftWorld || this.position.X + (float)this.width >= Main.rightWorld || this.position.Y <= Main.topWorld || this.position.Y + (float)this.height >= Main.bottomWorld)

	internal class AlternateDimensions : Mod
	{
		internal static AlternateDimensions modInstance;
		internal static AlternateDimensionsWorld worldInstance;

		double pressedSwapWorldsHotKeyTime;

		public AlternateDimensions()
		{
			Properties = new ModProperties() { Autoload = true };
		}

		public override void Load()
		{
			RegisterHotKey("Swap Worlds", "O");
			modInstance = this;
			worldInstance = (AlternateDimensionsWorld)GetModWorld("AlternateDimensionsWorld");
			worldInstance.Load();
		}

		public override void Unload()
		{
			worldInstance.Unload();
		}

		//public static void SendTextToPlayer(string msg, int playerIndex, Color color = new Color())
		//{
		//	NetMessage.SendData(25, playerIndex, -1, msg, 255, color.R, color.G, color.B, 0);
		//}

		//public static void SendTextToAllPlayers(string msg, Color color = new Color())
		//{
		//	NetMessage.SendData(25, -1, -1, msg, 255, color.R, color.G, color.B, 0);
		//}

		public override void HotKeyPressed(string name)
		{
			if (name == "Swap Worlds")
			{
				if (Math.Abs(Main.time - pressedSwapWorldsHotKeyTime) > 100)
				{
					pressedSwapWorldsHotKeyTime = Main.time;
					worldInstance.Switch();
				}
			}
		}

		//public override void ChatInput(string text)
		//{
		//	worldInstance.ChatInput(text);
		//}


		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			MessageType msgType = (MessageType)reader.ReadByte();
			//	DebugText($"Ded: {Main.dedServ} whoAmI:{whoAmI} Main.myPlayer{Main.myPlayer} msgType: {msgType}");
			switch (msgType)
			{
				case MessageType.RequestSwapTeleport:
					int x = reader.ReadInt32();
					int y = reader.ReadInt32();
					Vector2 destination = new Vector2(x * 16, y * 16);
					Main.player[whoAmI].Teleport(destination, 1, 0);
					RemoteClient.CheckSection(whoAmI, destination, 1);
					NetMessage.SendData(65, -1, -1, null, 0, whoAmI, destination.X, destination.Y, 1, 0, 0);
					break;
				//case MessageType.DimensionsData: // Server has sent Dimension data
				//	worldInstance.ReceiveDimensionData(reader);
				//	break;
				//case MessageType.RequestDimensionData: // Client wants data
				//	world.PlayerJoined(whoAmI);
				//	break;
				default:
					DebugText("Unknown Message type: " + msgType);
					break;
			}
			//DebugText($"Complete " + msgType);
		}
	}

	public static class AlternateDimensionInterface
	{
		//public static void SetSection(Dimension section)
		//{
		//	if (!Main.dedServ/*mod != null && mod is AlternateDimensions*/)
		//	{
		//		AlternateDimensions.worldInstance.SetSection(section);
		//	}
		//}

		public static void RegisterDimension(string modname, string areaname, Action<Rectangle> generatorCode)
		{
			//if (!Main.dedServ) // <-- ??
			{
				AlternateDimensions.worldInstance.RegisterDimension(modname, areaname, generatorCode);
			}
		}

		public static bool DimensionGenerated(string modname, string areaname)
		{
			//if (!Main.dedServ)
			{
				return AlternateDimensions.worldInstance.DimensionGenerated(modname, areaname);
			}
			//return false;
		}

		public static void DimensionSwap(string modname, string areaname)
		{
			//if (!Main.dedServ)
			{
				AlternateDimensions.worldInstance.DimensionSwap(modname, areaname);
			}
		}

		// I think only clients and SP will call Dimension Swap
		public static void DimensionSwapTeleport(string modname, string areaname, int x, int y, bool relative = true)
		{
			Rectangle rect = AlternateDimensions.worldInstance.DimensionRectangle(modname, areaname);
			if (relative)
			{
				x += rect.X;
				y += rect.Y;
			}

			if (Main.netMode == 0) // SP
			{
				AlternateDimensions.worldInstance.DimensionSwap(modname, areaname);
				Vector2 destination = new Vector2(x * 16, y * 16);
				Main.player[Main.myPlayer].Teleport(destination, 1, 0);
			}
			else if (Main.netMode == 1) // Client
			{
				AlternateDimensions.worldInstance.DimensionSwap(modname, areaname);
				var message = AlternateDimensions.modInstance.GetPacket();
				message.Write((byte)MessageType.RequestSwapTeleport);
				message.Write(x);
				message.Write(y);
				message.Send();
			}
			else // SERVER
			{
				DebugText("Server calling DimensionSwapTeleport??");
			}
		}

		public static Rectangle DimensionRectangle(string modname, string areaname)
		{
			//if (!Main.dedServ)
			{
				return AlternateDimensions.worldInstance.DimensionRectangle(modname, areaname);
			}
			//return new Rectangle();
		}

		public static void CurrentDimension(out string modname, out string areaname)
		{
			modname = AlternateDimensions.worldInstance.currentDimension.modname;
			areaname = AlternateDimensions.worldInstance.currentDimension.areaname;
			return;
		}
	}

	enum MessageType : byte
	{
		//DimensionsData,
		//RequestDimensionData,
		RequestSwapTeleport
	}
}

//Main.maxTilesX = 16800;
//   Main.tile = new Tile[Main.maxTilesX + 1, Main.maxTilesY + 1];
//Main.Map = new Terraria.Map.WorldMap(Main.maxTilesX, Main.maxTilesY);
//	Main.leftWorld = 0f;
//	Main.rightWorld = Main.maxTilesX * 16;
//	Main.topWorld = 0f;
//	Main.bottomWorld = Main.maxTilesY * 16;
//	Main.maxTilesX = (int)Main.rightWorld / 16 + 1;
//Main.maxTilesY = (int)Main.bottomWorld / 16 + 1;
//	Main.maxSectionsX = Main.maxTilesX / 200;
//	Main.maxSectionsY = Main.maxTilesY / 150;



//public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
//{
//	if (CheckIncomingData(ref messageType, ref reader, playerNumber))
//	{
//		return true;
//	}
//	return false;
//}

//public override void UpdateMusic(ref int music)
//{
//	world.CheckReceived();
//}

//public bool CheckIncomingData(ref byte msgType, ref BinaryReader binaryReader, int playerNumber)
//{
//	long readerPos = binaryReader.BaseStream.Position;
//	switch (msgType)
//	{
//		case 12:
//			DebugText("Hijack 12 AltDim");
//			if (Main.netMode == 2)
//			{
//				if (Netplay.Clients[playerNumber].State == 3)
//				{
//					PlayerJoined(playerNumber);
//				}
//			}
//			break;
//	}
//	//we need to set the stream position back to where it was before we got it
//	binaryReader.BaseStream.Position = readerPos;
//	return false;
//}

//private void PlayerJoined(int playerNumber)
//{
//	try
//	{
//		SendTextToPlayer("Testing, you should receive the AltDim sync soon", playerNumber, Color.Red);
//		//world.PlayerJoined(playerNumber);
//	}
//	catch
//	{
//		DebugText("Catch");
//	}
//}