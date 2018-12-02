using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using static LivelyPets.ModUtil;

namespace LivelyPets
{
  class ModUtil
  {
    //
    // ====== RNG =====
    //
    public static int PickRandom(int[] options)
    {
      Random r = new Random();
      int rand = r.Next(0, options.Length - 1);
      return options[rand];
    }

    public static bool DoRandom(double probability) {
      Random r = new Random();
      double rand = r.NextDouble();
      return rand <= probability;
    }

    //
    // ===== Path Finding =====
    //
    public static List<int> GetPath(GameLocation location, Vector2 from, Vector2 to, Character pet)
    {
      var padding = 1;
      var gridPos = new Vector2(MathHelper.Min(from.X, to.X) - padding, MathHelper.Min(from.Y, to.Y) - padding);
      var width = (int)Math.Abs(to.X - from.X)+1 + padding;
      var height = (int)Math.Abs(to.Y - from.Y)+1 + padding;

      bool[,] tileGrid = new bool[width, height];

      for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
          tileGrid[x, y] = !pet.currentLocation.isCollidingPosition(new Rectangle((int)(gridPos.X + x)*Game1.tileSize, (int)(gridPos.Y + y)*Game1.tileSize, pet.GetBoundingBox().Width, pet.GetBoundingBox().Height), Game1.viewport, false, 0, false, pet) || (gridPos.X + x == to.X && gridPos.Y + y == to.Y);


      Grid grid = new Grid(width, height, tileGrid);
      Point _from = new Point((int)(from.X - gridPos.X), (int)(from.Y - gridPos.Y));
      Point _to = new Point((int)(to.X - gridPos.X), (int)(to.Y - gridPos.Y));

      var path = PathFinder.FindPath(grid, _from, _to);
      var pathDirections = new List<int>();

      // Translate the path in grid to list of directions to move through path
      for (int i = 0; i < path.Count - 1; i++)
      {
        // Left
        if (path[i + 1].X - path[i].X < 0)
          pathDirections.Add(3);
        // Right
        else if (path[i + 1].X - path[i].X > 0)
          pathDirections.Add(1);
        // Up
        if (path[i + 1].Y - path[i].Y < 0)
          pathDirections.Add(0);
        // Down
        else if (path[i + 1].Y - path[i].Y > 0)
          pathDirections.Add(2);
      }

      return pathDirections;
    }

    private void IsTileObstructed(GameLocation location, Vector2 tile)
    {
      var obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
      var isObjectPassable = obj == null ? true : obj.isPassable();

    }

    private static bool[,] GenerateGrid(Vector2 gridPos, int width, int height, Func<Vector2, bool> f)
    {
      bool[,] grid = new bool[width, height];

      for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
          grid[x, y] = f(new Vector2((gridPos.X + x), (gridPos.Y + y)));
        }

      return grid;
    }

    // Modified implementation of 2D A* PathFinding Algorithm from https://github.com/RonenNess/Unity-2d-pathfinding
    /**
    * Provide simple path-finding algorithm with support in penalties.
    * Heavily based on code from this tutorial: https://www.youtube.com/watch?v=mZfyt03LDH4
    * This is just a Unity port of the code from the tutorial + option to set penalty + nicer API.
    *
    * Original Code author: Sebastian Lague.
    * Modifications & API by: Ronen Ness.
    * Since: 2016.
    */
    internal class PathFinder
    {
      public static List<Point> FindPath(Grid grid, Point startPos, Point targetPos)
      {
        // find path
        List<Node> nodes_path = _ImpFindPath(grid, startPos, targetPos);

        // convert to a list of points and return
        List<Point> ret = new List<Point>();
        if (nodes_path != null)
        {
          foreach (Node node in nodes_path)
          {
            ret.Add(new Point(node.gridX, node.gridY));
          }
        }
        return ret;
      }

      // internal function to find path, don't use this one from outside
      private static List<Node> _ImpFindPath(Grid grid, Point startPos, Point targetPos)
      {
        Node startNode = grid.nodes[startPos.X, startPos.Y];
        Node targetNode = grid.nodes[targetPos.X, targetPos.Y];

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
          Node currentNode = openSet[0];
          for (int i = 1; i < openSet.Count; i++)
          {
            if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
            {
              currentNode = openSet[i];
            }
          }

          openSet.Remove(currentNode);
          closedSet.Add(currentNode);

          if (currentNode == targetNode)
          {
            return RetracePath(grid, startNode, targetNode);
          }

          foreach (Node neighbour in grid.GetNeighbours(currentNode))
          {
            if (!neighbour.walkable || closedSet.Contains(neighbour))
            {
              continue;
            }

            int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) * (int)(10.0f * neighbour.penalty);
            if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
            {
              neighbour.gCost = newMovementCostToNeighbour;
              neighbour.hCost = GetDistance(neighbour, targetNode);
              neighbour.parent = currentNode;

              if (!openSet.Contains(neighbour))
                openSet.Add(neighbour);
            }
          }
        }

        return null;
      }

      private static List<Node> RetracePath(Grid grid, Node startNode, Node endNode)
      {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
          path.Add(currentNode);
          currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
      }

      private static int GetDistance(Node nodeA, Node nodeB)
      {
        int dstX = Math.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Math.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
          return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
      }
    }

    internal class Grid
    {
      public Node[,] nodes;
      int gridSizeX, gridSizeY;

      public Grid(int width, int height, float[,] tiles_costs)
      {
        gridSizeX = width;
        gridSizeY = height;
        nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
          for (int y = 0; y < height; y++)
          {
            nodes[x, y] = new Node(tiles_costs[x, y], x, y);

          }
        }
      }

      public Grid(int width, int height, bool[,] walkable_tiles)
      {
        gridSizeX = width;
        gridSizeY = height;
        nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
          for (int y = 0; y < height; y++)
          {
            nodes[x, y] = new Node(walkable_tiles[x, y] ? 1.0f : 0.0f, x, y);
          }
        }
      }

      public List<Node> GetNeighbours(Node node)
      {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
          for (int y = -1; y <= 1; y++)
          {
            if (x == 0 && y == 0)
              continue;

            int checkX = node.gridX + x;
            int checkY = node.gridY + y;

            if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
            {
              neighbours.Add(nodes[checkX, checkY]);
            }
          }
        }

        return neighbours;
      }
    }
  }

  internal class Node
  {
    public bool walkable;
    public int gridX;
    public int gridY;
    public float penalty;

    // calculated values while finding path
    public int gCost;
    public int hCost;
    public Node parent;

    // create the node
    // _price - how much does it cost to pass this tile. less is better, but 0.0f is for non-walkable.
    // _gridX, _gridY - tile location in grid.
    public Node(float _price, int _gridX, int _gridY)
    {
      walkable = _price != 0.0f;
      penalty = _price;
      gridX = _gridX;
      gridY = _gridY;
    }

    public int fCost
    {
      get
      {
        return gCost + hCost;
      }
    }
  }
}
