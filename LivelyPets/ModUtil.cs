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
    public int PickRandom(int[] options)
    {
      Random r = new Random();
      int rand = r.Next(0, options.Length - 1);
      return options[rand];
    }

    public bool DoRandom(double probability) {
      Random r = new Random();
      double rand = r.NextDouble();
      return rand <= probability;
    }

    //
    // ===== Path Finding =====
    //
    public List<Point> GetPath(GameLocation location, Vector2 from, Vector2 to)
    {
      var gridPos = new Vector2(MathHelper.Min(from.X, to.X), MathHelper.Min(from.Y, to.Y));
      var gridWidth = (int)Math.Abs(to.X - from.X);
      var gridHeight = (int)Math.Abs(to.Y - from.Y);
      bool f(Vector2 tile) => location.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport);

      bool[,] tileGrid = GenerateGrid(gridPos, gridWidth, gridHeight, f);

      Grid grid = new Grid(gridWidth, gridHeight, tileGrid);
      Point _from = new Point((int)from.X, (int)from.Y);
      Point _to = new Point((int)to.X, (int)to.Y);

      return PathFinder.FindPath(grid, _from, _to);
    }

    private bool[,] GenerateGrid(Vector2 gridPos, int width, int height, Func<Vector2, bool> f)
    {
      bool[,] grid = new bool[width, height];

      for (int x = (int)gridPos.X; x < width; x++)
        for (int y = (int)gridPos.Y; y < height; y++)
          grid[x, y] = f(new Vector2(x, y));

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
