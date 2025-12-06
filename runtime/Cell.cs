using Estqes.MapGen.RegionGenerator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Estqes.MapGen 
{
    public class Cell
    {
        public Cell(int index, Vector3 center)
        {
            Index = index;
            Center = center;
            Neighbours = new List<Cell>();

        }

        public int Index { get; private set; }
        public Vector3 Center { get; private set; }
        public List<Cell> Neighbours { get; private set; }
        
        public void AddNeighbour(Cell cell)
        {
            if (!Neighbours.Contains(cell)) Neighbours.Add(cell);
            if (!cell.Neighbours.Contains(this)) cell.Neighbours.Add(this);
        }
        public HashSet<Cell> Traverse(int depth)
        {
            var visited = new HashSet<Cell>();
            var queue = new Queue<(Cell cell, int currentDepth)>();

            visited.Add(this);
            queue.Enqueue((this, 0));

            while (queue.Count > 0)
            {
                var (currentCell, currentDepth) = queue.Dequeue();

                if (currentDepth < depth)
                {
                    foreach (var neighbour in currentCell.Neighbours)
                    {
                        if (visited.Add(neighbour))
                        {
                            queue.Enqueue((neighbour, currentDepth + 1));
                        }
                    }
                }
            }

            return visited;
        }
    }
    public interface IRegionComponent
    {
        public Region Region { get; set; }
        public void AddedToCell();
    }
}
