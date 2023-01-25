using System.Collections.Generic;
using UnityEngine;

public class Field : MonoBehaviour
{
  private Tile[,] _grid;
  private bool _canDrawConnection = false;

  private List<Tile> _connections = new List<Tile>();
  private Tile _connectionTile;

  private List<int> _solvedConnections = new List<int>();

  private int _dimensionX = 0;
  private int _dimensionY = 0;
  private Dictionary<int, int> _amountToSolve = new Dictionary<int, int>();

  void Start()
  {
    _dimensionX = transform.childCount;
    _dimensionY = transform.GetChild(0).transform.childCount;
    _grid = new Tile[_dimensionX, _dimensionY];
    for (int y = 0; y < _dimensionX; y++)
    {
      var row = transform.GetChild(y).transform;
      row.gameObject.name = "" + y;
      for (int x = 0; x < _dimensionY; x++)
      {
        var tile = row.GetChild(x).GetComponent<Tile>();
        tile.gameObject.name = "" + x;
        tile.onSelected.AddListener(onTileSelected);

        if (tile.cid > Tile.UNPLAYABLE_INDEX)
        {
          if (_amountToSolve.ContainsKey(tile.cid))
            _amountToSolve[tile.cid] += 1;
          else _amountToSolve[tile.cid] = 1;
        }

        _grid[x, y] = tile;
      }
    }

    _OutputGrid();
  }

  void _OutputGrid()
  {
    var results = "";
    int dimension = transform.childCount;
    for (int y = 0; y < dimension; y++)
    {
      results += "{";
      var row = transform.GetChild(y).transform;
      for (int x = 0; x < row.childCount; x++)
      {
        var tile = _grid[x, y];
        if (x > 0) results += ",";
        results += tile.cid;
      }
      results += "}\n";
    }
    Debug.Log("Main -> Start: _grid: \n" + results);
  }

  Vector3 _mouseWorldPosition;
  int _mouseGridX, _mouseGridY;
  // Update is called once per frame
  void Update()
  {
    if (_canDrawConnection)
    {
      _mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      _mouseGridX = (int)Mathf.Floor(_mouseWorldPosition.x);
      _mouseGridY = (int)Mathf.Floor(_mouseWorldPosition.y);

      if (_CheckMouseOutsideGrid()) return;

      Tile hoverTile = _grid[_mouseGridX, _mouseGridY];
      Tile firstTile = _connections[0];
      bool isDifferentActiveTile = hoverTile.cid > 0 && hoverTile.cid != firstTile.cid;

      if (hoverTile.isHighlighted || hoverTile.isSolved || isDifferentActiveTile) return;

      Vector2 connectionTilePosition = _FindTileCoordinates(_connectionTile);
      bool isPositionDifferent = IsDifferentPosition(_mouseGridX, _mouseGridY, connectionTilePosition);

      Debug.Log("Field -> OnMouseDrag(" + isPositionDifferent + "): " + _mouseGridX + "|" + _mouseGridY);

      if (isPositionDifferent)
      {
        var deltaX = System.Math.Abs(connectionTilePosition.x - _mouseGridX);
        var deltaY = System.Math.Abs(connectionTilePosition.y - _mouseGridY);
        bool isShiftNotOnNext = deltaX > 1 || deltaY > 1;
        bool isShiftDiagonal = (deltaX > 0 && deltaY > 0);
        Debug.Log("Field -> OnMouseDrag: isShiftNotOnNext = " + isShiftNotOnNext + "| isShiftDiagonal = " + isShiftDiagonal);
        if (isShiftNotOnNext || isShiftDiagonal) return;

        hoverTile.Highlight();
        hoverTile.SetConnectionColor(_connectionTile.ConnectionColor);

        _connectionTile.ConnectionToSide(
          _mouseGridY > connectionTilePosition.y,
          _mouseGridX > connectionTilePosition.x,
          _mouseGridY < connectionTilePosition.y,
          _mouseGridX < connectionTilePosition.x
        );

        _connectionTile = hoverTile;
        _connections.Add(_connectionTile);

        if (_CheckIfTilesMatch(hoverTile, firstTile))
        {
          _connections.ForEach((tile) => tile.isSolved = true);
          _canDrawConnection = false;
          _amountToSolve.Remove(firstTile.cid);
          if (_amountToSolve.Keys.Count == 0)
          {
            Debug.Log("GAME COMPLETE");
          }
        }
      }
    }
  }

  bool _CheckIfTilesMatch(Tile tile, Tile another)
  {
    return tile.cid > 0 && another.cid == tile.cid;
  }

  bool _CheckMouseOutsideGrid()
  {
    return _mouseGridY >= _dimensionY || _mouseGridY < 0 || _mouseGridX >= _dimensionX || _mouseGridX < 0;
  }

  void onTileSelected(Tile tile)
  {
    Debug.Log("Field -> onTileSelected(" + tile.isSelected + "): " + _FindTileCoordinates(tile));
    if (tile.isSelected)
    {
      _connectionTile = tile;
      _connections = new List<Tile>();
      _connections.Add(_connectionTile);
      _canDrawConnection = true;
      _connectionTile.Highlight();
    }
    else
    {
      bool isFirstTileInConnection = _connectionTile == tile;
      if (isFirstTileInConnection) tile.HightlightReset();
      else if (!_CheckIfTilesMatch(_connectionTile, tile))
      {
        _ResetConnections();
      }
      _canDrawConnection = false;
    }
  }

  void _ResetConnections()
  {
    Debug.Log("Field -> _ResetConnections: _connections.Count = " + _connections.Count);
    _connections.ForEach((tile) =>
    {
      tile.ResetConnection();
      tile.HightlightReset();
    });
  }

  Vector2 _FindTileCoordinates(Tile tile)
  {
    // Debug.Log("Field -> _FindTileCoordinates: " + tile.gameObject.name + " | " + tile.gameObject.transform.parent.gameObject.name);
    int x = int.Parse(tile.gameObject.name);
    int y = int.Parse(tile.gameObject.transform.parent.gameObject.name);
    return new Vector2(x, y);
  }

  public bool IsDifferentPosition(int gridX, int gridY, Vector2 position)
  {
    return position.x != gridX || position.y != gridY;
  }

  private class Connection
  {
    public Tile tile;
    public Vector2 position;
    public Connection(Tile tile, Vector2 position)
    {
      this.tile = tile;
      this.position = position;
    }

    public bool IsDifferentPosition(int gridX, int gridY)
    {
      return this.position.x != gridX || this.position.y != gridY;
    }
  }
}
