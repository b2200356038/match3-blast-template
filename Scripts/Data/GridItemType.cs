// GridItemType.cs

using System.Collections.Generic;

public enum GridItemType
{
    Empty,
    RedCube,
    GreenCube,
    BlueCube,
    YellowCube,
    VerticalRocket,
    HorizontalRocket,
    Box,
    Stone,
    Vase
}

// GridItemHelper.cs


/// <summary>
/// Helper class for GridItemType operations and categorization
/// </summary>
public static class GridItemHelper
{
    // Cache for performance optimization
    private static readonly HashSet<GridItemType> CubeTypes = new HashSet<GridItemType>
    {
        GridItemType.RedCube,
        GridItemType.GreenCube,
        GridItemType.BlueCube,
        GridItemType.YellowCube
    };
    
    private static readonly HashSet<GridItemType> RocketTypes = new HashSet<GridItemType>
    {
        GridItemType.VerticalRocket,
        GridItemType.HorizontalRocket
    };
    
    private static readonly HashSet<GridItemType> ObstacleTypes = new HashSet<GridItemType>
    {
        GridItemType.Box,
        GridItemType.Stone,
        GridItemType.Vase
    };
    
    /// <summary>
    /// Checks if the item type is a cube
    /// </summary>
    public static bool IsCube(GridItemType type) => CubeTypes.Contains(type);
    
    /// <summary>
    /// Checks if the item type is a rocket
    /// </summary>
    public static bool IsRocket(GridItemType type) => RocketTypes.Contains(type);
    
    /// <summary>
    /// Checks if the item type is an obstacle
    /// </summary>
    public static bool IsObstacle(GridItemType type) => ObstacleTypes.Contains(type);
    
    /// <summary>
    /// Converts a string code from level data to a GridItemType
    /// </summary>
    public static GridItemType StringToGridItemType(string code)
    {
        if (string.IsNullOrEmpty(code))
            return GridItemType.Empty;
            
        switch (code.ToLower())
        {
            case "r": return GridItemType.RedCube;
            case "g": return GridItemType.GreenCube;
            case "b": return GridItemType.BlueCube;
            case "y": return GridItemType.YellowCube;
            case "vro": return GridItemType.VerticalRocket;
            case "hro": return GridItemType.HorizontalRocket;
            case "bo": return GridItemType.Box;
            case "s": return GridItemType.Stone;
            case "v": return GridItemType.Vase;
            case "rand": return GetRandomCubeType();
            default: return GridItemType.Empty;
        }
    }
    
    /// <summary>
    /// Returns a random cube type
    /// </summary>
    public static GridItemType GetRandomCubeType()
    {
        int random = UnityEngine.Random.Range(0, 4);
        switch (random)
        {
            case 0: return GridItemType.RedCube;
            case 1: return GridItemType.GreenCube;
            case 2: return GridItemType.BlueCube;
            default: return GridItemType.YellowCube;
        }
    }
    
    /// <summary>
    /// Gets the corresponding color for a cube type
    /// </summary>
    public static Grid.Items.Cubes.CubeItem.CubeColor GetCubeColor(GridItemType type)
    {
        switch (type)
        {
            case GridItemType.RedCube:
                return Grid.Items.Cubes.CubeItem.CubeColor.Red;
            case GridItemType.GreenCube:
                return Grid.Items.Cubes.CubeItem.CubeColor.Green;
            case GridItemType.BlueCube:
                return Grid.Items.Cubes.CubeItem.CubeColor.Blue;
            case GridItemType.YellowCube:
                return Grid.Items.Cubes.CubeItem.CubeColor.Yellow;
            default:
                return Grid.Items.Cubes.CubeItem.CubeColor.Red;
        }
    }
}