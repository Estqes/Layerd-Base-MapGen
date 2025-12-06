using UnityEngine;

namespace Estqes.MapGen 
{
    public class SimpleLayer<T> : IMapLayer<T>
    {
        public T[] Layer { get; set; }
    }

    public interface IGrid
    {
        public bool IsCreated();
        public void SetSize(int size);
        public void GenerateGrid();
        public Cell GetCell(int index);
        public Cell[] GetAllCells();
        public int CalculateIndex(Vector3 positon);
    }
    public class RectangleGrid : IGrid
    {
        private Cell[] _cells;
        public float Ratio { get; }

        public RectangleGrid(float ratio)
        {
            Ratio = ratio;
        }

        public Vector2Int Size { get; private set; }
        public int CalculateIndex(Vector3 positon)
        {
            return ((int)(positon.x) + (int)(positon.y) * Size.x);
        }

        private int GetIndex(int x, int y)
        {
            return x + y * Size.x;
        }

        public void GenerateGrid()
        {
            _cells = new Cell[Size.x * Size.y];

            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    var index = GetIndex(x, y);
                    _cells[index] = new Cell(index, new Vector3(x, y, 0));
                }
            }

            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    Cell currentCell = _cells[GetIndex(x, y)];

                    if (x < Size.x - 1)
                    {
                        Cell rightNeighbour = _cells[GetIndex(x + 1, y)];
                        currentCell.AddNeighbour(rightNeighbour);

                        if(y > 1)
                        {
                            Cell rightDownNeighbour = _cells[GetIndex(x + 1, y - 1)];
                            currentCell.AddNeighbour(rightDownNeighbour);
                        }
                    }

                    if (y < Size.y - 1)
                    {
                        Cell topNeighbour = _cells[GetIndex(x, y + 1)];
                        currentCell.AddNeighbour(topNeighbour);
                        if (x < Size.x - 1)
                        {
                            Cell rightUpNeighbour = _cells[GetIndex(x + 1, y + 1)];
                            currentCell.AddNeighbour(rightUpNeighbour);
                        }
                    }
                }
            }
        }

        public Cell GetCell(int index)
        {
            if (index < 0 || index >= _cells.Length) return null;
            return _cells[index];
        }

        public Cell[] GetAllCells()
        {
            return _cells;
        }

        public bool IsCreated()
        {
            return _cells != null && _cells.Length > 0;
        }

        public void SetSize(int size)
        {
            Size = new Vector2Int(size, (int)(size * Ratio));
        }
    }
}
