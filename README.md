# AlternateDimensions
Alternate Dimensions mod for Terraria via tModLoader

# Modding Interface
The following is the interface you can access as a modder refencing this mod.

```csharp
namespace AlternateDimensions
{
  public static class AlternateDimensionInterface
  {
      // Register a function to generate the dimensions. Do this in PostSetupContent.
      public static void RegisterDimension(string modname, string areaname, Action<Rectangle> generatorCode)

      // Query whether the dimension has been generated in this world
      public static bool DimensionGenerated(string modname, string areaname)

      // Don't use this.
	  public static void DimensionSwap(string modname, string areaname)

      // Should teleport the current player to the coordinates. Use relative to specify coordinates relative to the dimension teleported to.
	  public static void DimensionSwapTeleport(string modname, string areaname, int x, int y, bool relative = true)

      // Query the rectangulare dimensions of your dimensions
	  public static Rectangle DimensionRectangle(string modname, string areaname)

      // Query the current player's Dimension
	  public static void CurrentDimension(out string modname, out string areaname)
  }
}
```

# Notes
If your mod has a "strong" refence to this mod, meaning you never want the user to run your mod without this mod, make sure your build.txt includes:
```modReferences = AlternateDimensions```

For a "weak" reference, more info later, but basically you have to be more careful with programming. You'll also need something like ```weakReferences = AlternateDimensions@0.0.0.2```

