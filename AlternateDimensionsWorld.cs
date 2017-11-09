using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Generation;
using System;
using System.Linq;

namespace AlternateDimensions
{
	// When the mod is loaded: register generation functions
	// When the world is generating: migrate worldsections from unused to used by calling them.
	// When a world is loading: populate worldsections, migrate sections if needed?

	internal class AlternateDimensionsWorld : ModWorld
	{
		private const int saveVersion = 0;
		List<Dimension> loadedDimensions = new List<Dimension>(); // Represent sections loaded by other mods.
		List<Dimension> existsInWorldDimensions = new List<Dimension>(); // Represent what actually is there.
		internal Dimension currentDimension;

		/*
		Goal: 

		Change borders when player is in area.
		Allow Mod to build in area. (callback?)
		Allow Mod control over area.
		Expand with new mod?
		Save: ModName, AreaName
		Traveling to and from? -- mod takes care of that?

		// concessions: everyone needs to be in same section?
		// only at world gen?
		*/

		// When the mod is loaded
		internal void Load()
		{
			DebugText($"Load AltDimWorld called: ded:{Main.dedServ}, myplayer:{Main.myPlayer}, mode:{Main.netMode}");

			loadedDimensions.Clear();
		}
		internal void Unload()
		{
		}

		// Called before World is loaded
		// ? is this called both in SP and MP, when in MP?
		// In SP, Initialize then Load. In MP, 
		public override void Initialize()
		{
			DebugText($"Initialize AltDimWorld called: ded:{Main.dedServ}, myplayer:{Main.myPlayer}, mode:{Main.netMode}");

			if (Main.netMode == 1) // if MP client...
			{
				Dimension vanilla = existsInWorldDimensions.FirstOrDefault(x => x.modname == "vanilla");
				if (vanilla != null)
				{
					SetSection(vanilla); // TODO, called before or after setworldsize?
					DebugText("Loading Vanilla: " + vanilla.ToString());
				}
				else
				{
					DebugText("Uhoh, no vanilla!");
				}
			}
			else
			{
				existsInWorldDimensions.Clear();
			}
			//received = false;
			//DebugText("Received: " + received);
		}

		// version 0: version, # segments, [int #, string modname, string areaname, rectangle area]*
		public override void SaveCustomData(BinaryWriter writer)
		{
			DebugText($"SaveCustomData AltDimWorld called: ded:{Main.dedServ}, myplayer:{Main.myPlayer}, mode:{Main.netMode}");


			// Existing world, not generated with this. No previous custom data?
			if (existsInWorldDimensions.Count == 0)
			{
				existsInWorldDimensions.Add(new Dimension(0, "vanilla", "vanillaarea", new Rectangle(0, 0, Main.maxTilesX, Main.maxTilesY)));
			}

			writer.Write(saveVersion); // necessary.
			writer.Write(existsInWorldDimensions.Count);
			for (int i = 0; i < existsInWorldDimensions.Count; i++)
			{
				writer.Write(Dimension.saveversion);
				writer.Write(i);
				writer.Write(existsInWorldDimensions[i].modname);
				writer.Write(existsInWorldDimensions[i].areaname);
				writer.Write(existsInWorldDimensions[i].area.X);
				writer.Write(existsInWorldDimensions[i].area.Y);
				writer.Write(existsInWorldDimensions[i].area.Width);
				writer.Write(existsInWorldDimensions[i].area.Height);
				DebugText($"Writing out {existsInWorldDimensions[i]}");
			}
			DebugText($"Wrote out {existsInWorldDimensions.Count} sections");
		}

		public override void LoadCustomData(BinaryReader reader)
		{
			DebugText($"LoadCustomData AltDimWorld called: ded:{Main.dedServ}, myplayer:{Main.myPlayer}, mode:{Main.netMode}");

			int version = reader.ReadInt32();
			if (version == 0)
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					int worldSectionVersion = reader.ReadInt32();
					if (worldSectionVersion == 0)
					{
						int index = reader.ReadInt32();
						string modname = reader.ReadString();
						string areaname = reader.ReadString();
						int x = reader.ReadInt32();
						int y = reader.ReadInt32();
						int width = reader.ReadInt32();
						int height = reader.ReadInt32();
						Rectangle area = new Rectangle(x, y, width, height);
						existsInWorldDimensions.Add(new Dimension(index, modname, areaname, area));
						DebugText("Loaded " + existsInWorldDimensions[i]);
					}
					else
					{
						DebugText("Unhandled worldSectionVersion: " + worldSectionVersion);
					}
				}
				Dimension vanilla = existsInWorldDimensions.FirstOrDefault(x => x.modname == "vanilla");
				if (vanilla != null)
				{
					if (!Main.dedServ)
					{
						SetSection(vanilla); // TODO, called before or after setworldsize?
						DebugText("Loading Vanilla: " + vanilla.ToString());
					}
				}
				else
				{
					DebugText("Uhoh, no vanilla!");
				}
			}
		}

		public override void NetSend(BinaryWriter writer)
		{
			DebugText($"SendCustomData AltDimWorld called: ded:{Main.dedServ}, myplayer:{Main.myPlayer}, mode:{Main.netMode}");

			writer.Write(existsInWorldDimensions.Count);
			for (int i = 0; i < existsInWorldDimensions.Count; i++)
			{
				writer.Write(Dimension.saveversion);
				writer.Write(i);
				writer.Write(existsInWorldDimensions[i].modname);
				writer.Write(existsInWorldDimensions[i].areaname);
				writer.Write(existsInWorldDimensions[i].area.X);
				writer.Write(existsInWorldDimensions[i].area.Y);
				writer.Write(existsInWorldDimensions[i].area.Width);
				writer.Write(existsInWorldDimensions[i].area.Height);
				DebugText($"ded:{Main.dedServ} {existsInWorldDimensions[i]}");
			}
		}

		// Happens fairly often, but most importantly at join.
		// To Test: will data still be there when go between SP and MP?
		public override void NetReceive(BinaryReader reader)
		{
			DebugText($"ReceiveCustomData AltDimWorld called: ded:{Main.dedServ}, myplayer:{Main.myPlayer}, mode:{Main.netMode}");

			if (Main.dedServ)
			{
				DebugText("ReceiveDimensionData on dedserv");
			}
			//if (existsInWorldDimensions.Count > 0)
			//{
			//	DebugText("ReceiveDimensionData existsCount: " + existsInWorldDimensions.Count);
			//}
			DebugText("Receving existsInWorldDimensions");

			existsInWorldDimensions.Clear();

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				int worldSectionVersion = reader.ReadInt32();
				if (worldSectionVersion == 0)
				{
					int index = reader.ReadInt32();
					string modname = reader.ReadString();
					string areaname = reader.ReadString();
					int x = reader.ReadInt32();
					int y = reader.ReadInt32();
					int width = reader.ReadInt32();
					int height = reader.ReadInt32();
					Rectangle area = new Rectangle(x, y, width, height);
					existsInWorldDimensions.Add(new Dimension(index, modname, areaname, area));
				}
				else
				{
					DebugText("Unhandled worldSectionVersion during ReceiveDimensionData: " + worldSectionVersion);
				}
			}
			DebugText("Received existsInWorldDimensions: " + existsInWorldDimensions.Count);
			//received = true;
		}

		// HOOKS Section
		internal void DimensionSwap(string modname, string areaname)
		{
			Dimension current = existsInWorldDimensions.FirstOrDefault(x => x.modname == modname && x.areaname == areaname);
			if (current != null)
			{
				SetSection(current);
			}
			else
			{
				DebugText("existsInWorldDimensions.Count" + existsInWorldDimensions.Count);
				foreach (var item in existsInWorldDimensions)
				{
					DebugText(item.ToString());
				}
				DebugText("Uhoh, DimensionSwap called on nonexistant Dimension: " + modname + " " + areaname);
			}
		}

		internal Rectangle DimensionRectangle(string modname, string areaname)
		{
			Dimension current = existsInWorldDimensions.FirstOrDefault(x => x.modname == modname && x.areaname == areaname);
			if (current != null)
			{
				DebugText(Main.dedServ + " DimRec cur: " + current);
				return current.area;
			}
			else
			{
				DebugText("Uhoh, DimensionRectangle called on nonexistant Dimension: " + modname + " " + areaname);
				return new Rectangle();
			}
		}

		internal bool DimensionGenerated(string modname, string areaname)
		{
			if (existsInWorldDimensions.Any(x => x.modname == modname && x.areaname == areaname))
			{
				return true;
			}
			return false;
		}

		// registers a dimension. Not necessarily added to sections, since that happens during world gen
		internal bool RegisterDimension(string modname, string areaname, Action<Rectangle> generatorCode)
		{
			//if (DimensionGenerated(modname, areaname))
			//{
			//	WorldSection current = sections.FirstOrDefault(x => x.modname == modname && x.areaname == areaname);
			//	if (current != null)
			//	{
			//		current.generateCode = generatorCode;
			//	}
			//	else
			//	{
			//		DebugText("Uhoh, Dimension Generated but Find false?? " + modname + " " + areaname);
			//	}
			//}
			//else
			{
				loadedDimensions.Add(new Dimension(modname, areaname, generatorCode));
			}
			return true;
		}

		// ModNet messages? Mods calling SetSection?

		internal void SetSection(string modname, string areaname)
		{

		}

		private void SetSection(Dimension section)
		{
			currentDimension = section;
			if (Main.dedServ)
			{
				DebugText($"Warning: Server attempting to change it's dimension: {section.modname} {section.areaname}");
			}
			// TODO: only on client? Sync?
			Main.leftWorld = section.area.Left * 16;
			Main.rightWorld = section.area.Right * 16;
			Main.topWorld = section.area.Top * 16;
			Main.bottomWorld = section.area.Bottom * 16;

			// TODO. Set surface and rock layer?
			if(section.area.Left == 0)
			{

			}

			DebugText($"SetSection: {section}");
		}
		// END HOOKS

		static bool debug = false;
		// DEBUG SECTION
		internal static void DebugText(string message)
		{
			if (debug)
			{
				//if (Main.dedServ)
				//{
				//	Console.WriteLine(message);
				//}
				//else
				{
					if (Main.gameMenu)
					{
						ErrorLogger.Log(Main.myPlayer + ": " + message);
					}
					else
					{
						Main.NewText(message);
					}

				}
			}
		}

		internal void ChatInput(string text)
		{
			if (text[0] != '/')
			{
				return;
			}
			text = text.Substring(1);
			int index = text.IndexOf(' ');
			string command;
			string[] args;
			if (index < 0)
			{
				command = text;
				args = new string[0];
			}
			else
			{
				command = text.Substring(0, index);
				args = text.Substring(index + 1).Split(' ');
			}
			if (command == "var")
			{
				ChangeVariable(args);
			}
		}

		public void ChangeVariable(string[] args)
		{
			// TODO, debug console commands
		}

		int swapWorldsIndex = 0;
		internal void Switch()
		{
			//if (a == 0)
			//{
			//	Main.leftWorld = 0f;
			//	Main.rightWorld = 4200 * 16;
			//}
			//else if (a == 1)
			//{
			//	Main.leftWorld = 4200 * 16;
			//	Main.rightWorld = (4200 + 500) * 16;
			//}
			//else if (a == 2)
			//{
			//	Main.leftWorld = (4200 + 500) * 16;
			//	Main.rightWorld = (4200 + 1000) * 16;
			//}
			//else
			//{
			//	Main.leftWorld = (4200 + 1000) * 16;
			//	Main.rightWorld = (4200 + 1500) * 16;
			//	Main.worldSurface = 1200;
			//	Main.rockLayer = 1200;
			//}

			// Bug if none loaded
			DebugText("Switch");
			DebugText("Switch: " + existsInWorldDimensions.Count);
			swapWorldsIndex = (swapWorldsIndex + 1) % existsInWorldDimensions.Count;
			DebugText($"Switch Called: {swapWorldsIndex} --> {existsInWorldDimensions[swapWorldsIndex]}");
			SetSection(existsInWorldDimensions[swapWorldsIndex]);
		}
		// END DEBUG

		// ACTUAL WORLDGEN
		// Actually, should I let this happen eariler?
		public override void PostWorldGen()
		{
			existsInWorldDimensions.Add(new Dimension(0, "vanilla", "vanillaarea", new Rectangle(0, 0, Main.maxTilesX, Main.maxTilesY)));
			foreach (Dimension dimension in loadedDimensions)
			{
				// maxTiles should be even number at this point
				Rectangle area = new Rectangle(Main.maxTilesX, 0, 500, Main.maxTilesY);
				//ErrorLogger.Log("1" + Main.tile.GetHashCode() + " " + Main.tile.GetLength(0));
				Main.tile = ResizeArray<Tile>(Main.tile, Main.maxTilesX + 500 + 1, Main.maxTilesY + 1); // add one because originally it wa that
																										//ErrorLogger.Log("2" + Main.tile.GetHashCode() + " " + Main.tile.GetLength(0));

				Action<int, int> newTile = (x, y) => Main.tile[x, y] = new Tile();
				Action<int, int> activate = (x, y) => { Main.tile[x, y].active(true); };
				Action<int, int> defaultFrame = (x, y) => { Main.tile[x, y].frameX = -1; Main.tile[x, y].frameY = -1; };

				//Action<int, int> log = (x, y) =>
				//{
				//	if (x % 5 == 0 && y % 5 == 0)
				//	{
				//		Main.tile[x, y].active(true);
				//		Main.tile[x, y].frameX = -1;
				//		Main.tile[x, y].frameY = -1;
				//		Main.tile[x, y].type = TileID.Mud;
				//	//	ErrorLogger.Log(x + " " + y + Main.tile[x, y].active()+ " " + Main.tile.GetHashCode() + " " + Main.tile.GetLength(0));
				//	}
				//};

				DoXInRange(area.Left, area.Top, area.Right, area.Bottom, newTile /*activate*/ /*defaultFrame*/);
				DoXAroundBorder(area.Left, area.Top, area.Right, area.Bottom, activate, defaultFrame, (x, y) => { Main.tile[x, y].type = TileID.Stone; });
				try
				{
					Main.maxTilesX += 500;

					dimension.generateCode(area);
					existsInWorldDimensions.Add(new Dimension(existsInWorldDimensions.Count, dimension.modname, dimension.areaname, area));
					DebugText($"GenerateCode Success for Mod:{dimension.modname} and Area:{dimension.areaname}");


					// needed?
					Main.bottomWorld = (float)(Main.maxTilesY * 16);
					Main.rightWorld = (float)(Main.maxTilesX * 16);
					Main.maxSectionsX = Main.maxTilesX / 200;
					Main.maxSectionsY = Main.maxTilesY / 150;
				}
				catch
				{
					DebugText($"GenerateCode failed for Mod:{dimension.modname} Area:{dimension.areaname}");
				}
			}
			DebugText($"PostWorldGenComplete");
		}

		private void DoXInRange(int x1, int y1, int x2, int y2, params Action<int, int>[] actions)
		{
			try
			{
				//ErrorLogger.Log("aDox" + x1 + " " + y1 + " " + x2 + " " + y2);
				for (int i = x1; i < x2; i++)
				{
					for (int j = y1; j < y2; j++)
					{
						//if (i % 50 == 0 && j % 50 == 0)
						//{
						//	ErrorLogger.Log("D22ox action " + i + " " + j);
						//}
						foreach (var action in actions)
						{
							action(i, j);
						}
					}
				}
			}
			catch (Exception e)
			{
				ErrorLogger.Log(e.Message);
			}
		}

		private void DoXAroundBorder(int x1, int y1, int x2, int y2, params Action<int, int>[] actions)
		{
			for (int i = x1; i < x2; i++)
			{
				for (int j = y1; j < y2; j++)
				{
					if (i == x1 || i == x2 - 1)
					{
						foreach (var action in actions)
						{
							action(i, j);
						}
					}
					else
					{
						if (j == y1 || j == y2 - 1)
						{
							foreach (var action in actions)
							{
								action(i, j);
							}
						}
					}
				}
			}
		}

		/*
		//Main.tile = ResizeArray<Tile>(Main.tile, Main.maxTilesX + 500 + 1, Main.maxTilesY + 1);
			//Main.tile = ResizeArray<Tile>(Main.tile, Main.maxTilesX + 500 + 1, Main.maxTilesY + 1);
			Main.maxTilesX = Main.maxTilesX + 1500;

			for (int i = Main.maxTilesX - 1500; i < Main.maxTilesX - 1000; i++)
			{
				for (int j = 0; j < Main.maxTilesY; j++)
				{
					Main.tile[i, j] = new Tile();

					if (j > 300)
					{
						Main.tile[i, j].active(true);
						Main.tile[i, j].type = 0;
						Main.tile[i, j].frameX = -1;
						Main.tile[i, j].frameY = -1;
					}
				}
			}

			for (int i = Main.maxTilesX - 1500; i < Main.maxTilesX - 1460; i++)
			{
				for (int j = 0; j < Main.maxTilesY; j++)
				{
					Main.tile[i, j].active(true);
					Main.tile[i, j].type = TileID.Stone;
					Main.tile[i, j].frameX = -1;
					Main.tile[i, j].frameY = -1;
				}
			}

			for (int i = Main.maxTilesX - 1000; i < Main.maxTilesX - 500; i++)
			{
				for (int j = 0; j < Main.maxTilesY; j++)
				{
					Main.tile[i, j] = new Tile();

					if (j > 400)
					{
						Main.tile[i, j].active(true);
						Main.tile[i, j].type = TileID.Mud;
						Main.tile[i, j].frameX = -1;
						Main.tile[i, j].frameY = -1;
					}
				}
			}

			for (int i = Main.maxTilesX - 1000; i < Main.maxTilesX - 960; i++)
			{
				for (int j = 0; j < Main.maxTilesY; j++)
				{
					Main.tile[i, j].active(true);
					Main.tile[i, j].type = TileID.Stone;
					Main.tile[i, j].frameX = -1;
					Main.tile[i, j].frameY = -1;
				}
			}

			for (int i = Main.maxTilesX - 500; i < Main.maxTilesX; i++)
			{
				for (int j = 0; j < Main.maxTilesY; j++)
				{
					Main.tile[i, j] = new Tile();

					//if (j > 400)
					//{
					//	Main.tile[i, j].active(true);
					//	Main.tile[i, j].type = TileID.SandstoneBrick;
					//	Main.tile[i, j].frameX = -1;
					//	Main.tile[i, j].frameY = -1;
					//}
				}
			}

			//for (int i = Main.maxTilesX - 500; i < Main.maxTilesX - 460; i++)
			//{
			//	for (int j = 0; j < Main.maxTilesY; j++)
			//	{
			//		Main.tile[i, j].active(true);
			//		Main.tile[i, j].type = TileID.Stone;
			//		Main.tile[i, j].frameX = -1;
			//		Main.tile[i, j].frameY = -1;
			//	}
			//}

			int numIslandHouses = 0;
			int houseCount = 0;
			int skyLakes = 5;
			int[] fihX = new int[30];
			int[] fihY = new int[30];

			bool[] skyLake = new bool[30];

			Rectangle[] fihs = new Rectangle[30];

			for (int k = 0; k < 20; k++)
			{
				int numTrys = 1000;
				int xPos = WorldGen.genRand.Next((int)Main.maxTilesX - 400, Main.maxTilesX - 100);
				//	while (--numTrys > 0)
				{
					bool flag2 = true;
					for (int l = 0; l < numIslandHouses; l++)
					{
						//if (Rectangle.Intersect(fihs[l], new Rectangle(num2 - 180,, 360, 50);
						//if (num2 > fihX[l] - 180 && num2 < fihX[l] + 180)
						//{
						//	flag2 = false;
						//	break;
						//}
					}
					if (flag2)
					{
						//flag2 = false;
						//int num3 = 0;
						//int num4 = 200;
						//while ((double)num4 < Main.worldSurface)
						//{
						//	if (Main.tile[num2, num4].active())
						//	{
						//		num3 = num4;
						//		flag2 = true;
						//		break;
						//	}
						//	num4++;
						//}
						//if (flag2)
						{
							int yPos = WorldGen.genRand.Next(100, Main.maxTilesY - 100);
							//num5 = Math.Min(num5, (int)WorldGen.worldSurfaceLow - 50);
							if (k < skyLakes)
							{
								skyLake[numIslandHouses] = true;
								WorldGen.CloudLake(xPos, yPos);
							}
							else
							{
								WorldGen.CloudIsland(xPos, yPos);
							}
							fihX[numIslandHouses] = xPos;
							fihY[numIslandHouses] = yPos;
							numIslandHouses++;
						}
					}
				}
			}

			for (int k = 0; k < numIslandHouses; k++)
			{
				if (!skyLake[k])
				{
					WorldGen.IslandHouse(fihX[k], fihY[k]);
				}
			}



			//	Main.rightWorld = (Main.maxTilesX - 1500) * 16;

			//		int a = 2;
		}
		*/

		protected T[,] ResizeArray<T>(T[,] original, int x, int y)
		{
			T[,] newArray = new T[x, y];
			int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
			int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

			int a = original.GetLength(0);
			int b = original.GetLength(1);
			int c = newArray.GetLength(0);
			int d = newArray.GetLength(1);
			DebugText($"Resize Array: Ox {a} 0y {b} Nx {c} Ny {d}");


			for (int i = 0; i < minX; ++i)
				Array.Copy(original, i * original.GetLength(1), newArray, i * newArray.GetLength(1), minY - 1);

			return newArray;
		}
	}

	public class Dimension
	{
		internal const int saveversion = 0;

		// From Load
		internal int index;
		internal string modname;
		internal string areaname;
		internal Rectangle area;

		// From Call.
		internal Action<Rectangle> generateCode;

		public Dimension(int index, string modname, string areaname, Rectangle area)
		{
			this.index = index;
			this.modname = modname;
			this.areaname = areaname;
			this.area = area;
		}

		public Dimension(string modname, string areaname, Action<Rectangle> generateCode)
		{
			this.modname = modname;
			this.areaname = areaname;
			this.generateCode = generateCode;
		}

		public override string ToString()
		{
			return $"I:{index} MN:{modname} AN:{areaname} L:{area.Left} R:{area.Right} T:{area.Top} B:{area.Bottom}";
		}
	}
}




//// TODO, check if initializing at world gen?
//DebugText("1 WorldRight " + Main.rightWorld);
//DebugText("1 maxTilesX " + Main.maxTilesX);
//// Setworld size, projectiles going inactive.
//Main.rightWorld = 4200 * 16;
//Main.leftWorld = 0;

//DebugText("2 WorldRight " + Main.rightWorld);
//DebugText("2 maxTilesX " + Main.maxTilesX);



// Wait, does/should this happen on client?
// Most should happen before anything is even loaded.
//internal object Call(object[] args)
//{
//	try
//	{
//		string message = args[0] as string;
//		if (message == "RegisterDimension_1") // Adds the name, areaname, requested size?, and generator funtion?
//		{
//			RegisterDimension(args[1] as string, args[2] as string, args[3] as Action<Rectangle>);
//		}
//		else if (message == "DimensionGenerated_1") // Does my dimension exist in this world? -- during gameplay.
//		{
//			return DimensionGenerated(args[1] as string, args[2] as string);
//		}
//		else if (message == "DimensionRectangle_1") // If so, tell me the rectangle -- during gameplay, during generation
//		{
//			return DimensionRectangle(args[1] as string, args[2] as string);
//		}
//		else if (message == "DimensionSwap_1") // If so, tell me the rectangle -- during gameplay, during generation
//		{
//			DimensionSwap(args[1] as string, args[2] as string);
//		}
//		else if (message == "SetSection_1") // clients dont have the sections?
//		{
//			Rectangle a = (Rectangle)args[1];
//			SetSection(new Dimension(0, "", "", a));
//		}
//		else
//		{
//			DebugText("Unknown message: " + message);
//		}
//	}
//	catch
//	{
//		DebugText("Error parsing Call");
//	}
//	return null;
//}

//internal void CheckReceived()
//{
//	//	DebugText($"Main.ded:{Main.dedServ} Main.myPlayer:{Main.myPlayer} PostUpdate");

//	if (!received && Main.netMode == 1 && !Main.gameMenu)
//	{
//		count++;
//		if (count > 60)
//		{
//			count = 0;
//			DebugText($"Main.ded:{Main.dedServ} Main.myPlayer:{Main.myPlayer} RequestDimensionData Start");
//			var p = mod.GetPacket();
//			p.Write((byte)MessageType.RequestDimensionData);
//			p.Send();
//			//received = true;
//			DebugText($"Main.ded:{Main.dedServ} Main.myPlayer:{Main.myPlayer} RequestDimensionData Sent");
//		}
//	}
//}

//internal void PlayerJoined(int playerNumber)
//{
//	DebugText("Player Joined number " + playerNumber);
//	try
//	{
//		var p = mod.GetPacket();
//		p.Write((byte)MessageType.DimensionsData);
//		p.Write(existsInWorldDimensions.Count);
//		for (int i = 0; i < existsInWorldDimensions.Count; i++)
//		{
//			p.Write(Dimension.saveversion);
//			p.Write(i);
//			p.Write(existsInWorldDimensions[i].modname);
//			p.Write(existsInWorldDimensions[i].areaname);
//			p.Write(existsInWorldDimensions[i].area.X);
//			p.Write(existsInWorldDimensions[i].area.Y);
//			p.Write(existsInWorldDimensions[i].area.Width);
//			p.Write(existsInWorldDimensions[i].area.Height);
//			DebugText($"ded:{Main.dedServ} {existsInWorldDimensions[i]}");
//		}
//		p.Send(playerNumber);
//	}
//	catch
//	{
//		DebugText("PJ Catch");
//	}
//}

//internal void ReceiveDimensionData(BinaryReader reader)
//{
//	if (Main.dedServ)
//	{
//		DebugText("ReceiveDimensionData on dedserv");
//	}
//	if (existsInWorldDimensions.Count > 0)
//	{
//		DebugText("ReceiveDimensionData existsCount: " + existsInWorldDimensions.Count);
//	}
//	DebugText("Receving existsInWorldDimensions");

//	int count = reader.ReadInt32();
//	for (int i = 0; i < count; i++)
//	{
//		int worldSectionVersion = reader.ReadInt32();
//		if (worldSectionVersion == 0)
//		{
//			int index = reader.ReadInt32();
//			string modname = reader.ReadString();
//			string areaname = reader.ReadString();
//			int x = reader.ReadInt32();
//			int y = reader.ReadInt32();
//			int width = reader.ReadInt32();
//			int height = reader.ReadInt32();
//			Rectangle area = new Rectangle(x, y, width, height);
//			existsInWorldDimensions.Add(new Dimension(index, modname, areaname, area));
//		}
//		else
//		{
//			DebugText("Unhandled worldSectionVersion during ReceiveDimensionData: " + worldSectionVersion);
//		}
//	}
//	DebugText("Received existsInWorldDimensions: " + existsInWorldDimensions.Count);
//	//received = true;
//}