using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Board : MonoBehaviour
{
    public float timeBeetwenPieces = 0.05f; 
    //Guarda el ancho de la cuadricula
    public int widht;

    //Guarda el alto de la cuadricula
    public int height;

    //Objeto que se utiliza para los espacios vacios de la cuadricula
    public GameObject tileObject;

    //Permite agregar un nuevo numero al tamaño ortografico final de la camara
    public float cameraSizeOffset;

    //Permite agregar valor en posicion vertical a la camara
    public float cameraVerticalOffset;

    public GameObject[] availablePieces;

    Tile[,] tiles;
    Piece[,] pieces;

    Tile startTile;
    Tile endTile;

    bool swappingPieces = false;

    
    void Start()
    {
        tiles = new Tile [widht,height];
        pieces = new Piece [widht,height];
        PositionCamera();
        SetupBoard();
        StartCoroutine(SetUpPieces());
    }

    private IEnumerator SetUpPieces()
    {
        int maxIterations = 50;
        int currentIterations = 0;

        for (int x = 0; x < widht; x++)
        {
            for (int y = 0; y < height; y++)
            {
                yield return new WaitForSeconds(timeBeetwenPieces);
                if (pieces[x,y]== null)
                {
                    currentIterations = 0;
                    var newPiece = CreatePieceAt(x,y);
                    while(HasPreviousMatches(x,y))
                    {
                        ClearPieceAT(x,y);
                        newPiece = CreatePieceAt(x,y);
                        currentIterations++;
                        maxIterations++;
                        if (currentIterations>maxIterations)
                        {
                            break;
                        }
                    }
                }
                
            }
        }
        yield return null;
    }

    private void ClearPieceAT(int x, int y)
    {
        var pieceToClear = pieces[x,y];
        Destroy(pieceToClear.gameObject);
        pieces[x,y] = null;
    }

    private Piece CreatePieceAt(int x, int y)
    {
        var selectedPiece = availablePieces[UnityEngine.Random.Range(0,availablePieces.Length)];
        //Objeto temporal
        var o = Instantiate(selectedPiece,new Vector3(x,y,-5), quaternion.identity);
        o.transform.parent = transform;
        pieces[x,y] = o.GetComponent<Piece>();
        pieces[x,y].SetUp(x,y, this);
        return pieces[x,y];
    }

    //Ajustado de Camara
    private void PositionCamera()
    {
        float newPosX = (float)widht/2;
        float newPosY = (float)height/2;

        Camera.main.transform.position = new Vector3(newPosX - 0.5f,newPosY - 0.5f + cameraVerticalOffset,-10f);

        float horizontal = widht + 1;
        float vertical = (height/2)+1;
        //Operacion ternaria
        Camera.main.orthographicSize = horizontal > vertical?horizontal + cameraSizeOffset:vertical + cameraSizeOffset;
    }

    private void SetupBoard()
    {
        //Se crea dos for loops para el ancho y el alto de la cuadricula
        for (int x = 0; x < widht; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Objeto temporal
                var o = Instantiate(tileObject,new Vector3(x,y,-5), quaternion.identity);
                o.transform.parent = transform;
                tiles[x,y] = o.GetComponent<Tile>();
                tiles[x,y]?.SetUp(x,y, this);
            }
        }
    }

   public void TileDown(Tile tile_)
   {
        if (!swappingPieces)
        {
            startTile = tile_;
        }    
        
   }
   public void TileOver(Tile tile_)
   {
        if (!swappingPieces)
        {
            endTile = tile_;
        }
        
   }
   public void TileUp(Tile tile_)
   {
        if (!swappingPieces)
        {
            if (startTile != null && endTile !=null && IsCloseto(startTile,endTile))
            {
                StartCoroutine(SwapTiles());
            }
        }
        
        
   }

    IEnumerator SwapTiles()
    {
        swappingPieces = true;
        var StarPiece = pieces[startTile.x, startTile.y];
        var EndPiece =  pieces[endTile.x, endTile.y];

        StarPiece.Move(endTile.x,endTile.y);
        EndPiece.Move(startTile.x, startTile.y);

        pieces[startTile.x, startTile.y] = EndPiece;
        pieces[endTile.x, endTile.y] = StarPiece;

        yield return new WaitForSeconds(0.6f);

        
        var startMatches = GetmatchByPiece(startTile.x,startTile.y,3);
        var endMatches = GetmatchByPiece(endTile.x,endTile.y,3);

        var allMatches = startMatches.Union(endMatches).ToList();

       

        if (allMatches.Count==0)
        {
            StarPiece.Move(startTile.x, startTile.y);
            EndPiece.Move(endTile.x,endTile.y);
            pieces[startTile.x,startTile.y] = StarPiece;
            pieces[endTile.x,endTile.y] = EndPiece;
        }
        else
        {
            ClearPieces(allMatches);
        }

        startTile = null;
        endTile = null;
        swappingPieces = false;

        yield return null;
    }

    private void ClearPieces(List<Piece> piecesToClear)
    {
        piecesToClear.ForEach(piece =>
        {
            ClearPieceAT(piece.x,piece.y);
        });
        List<int> columns = GetColumns(piecesToClear);
        List<Piece> collapsedPieces = collapseColumns(columns, 0.3f);

        FindMatchRecursively(collapsedPieces);
    }

    private void FindMatchRecursively(List<Piece> collapsedPieces)
    {
       StartCoroutine(FindMatchRecursivelyCoroutine(collapsedPieces));
    }

    IEnumerator FindMatchRecursivelyCoroutine(List<Piece> collapsedPieces)
    {
        yield return new WaitForSeconds(1);
        List<Piece> newMatches = new List<Piece>();

        collapsedPieces.ForEach(piece =>
        {
            var matches = GetmatchByPiece(piece.x, piece.y,3);
            if (matches != null)
            {
                newMatches = newMatches.Union(matches).ToList();
                ClearPieces(matches);
            }
        });
        if (newMatches.Count>0)
        {
            var newCollapsedPieces = collapseColumns(GetColumns(newMatches),0.3f);
            FindMatchRecursively(newCollapsedPieces);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(SetUpPieces());
            swappingPieces = false;
        }
        yield return null;
    }

    private List<Piece> collapseColumns(List<int> columns, float timeToCollapse)
    {
        List<Piece> movingPieces = new List<Piece>();

        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            for (int y = 0; y < height; y++)
            {
                if (pieces[column, y] == null)
                {
                    for (int yPlus = y + 1; yPlus < height; yPlus++)
                    {
                        if (pieces[column,yPlus]!= null)
                        {
                            pieces[column,yPlus].Move(column,y);

                            pieces[column,y] = pieces[column,yPlus];

                            if (!movingPieces.Contains(pieces[column,y]))
                            {
                                movingPieces.Add(pieces[column,y]);
                            }
                            pieces[column,yPlus] = null;
                            break;
                        }
                    }
                }
            }
        }
        return movingPieces;
    }

    private List<int>  GetColumns(List<Piece> piecesToClear)
    {
        var result = new List<int>();

        piecesToClear.ForEach(piece =>
        {
            if (!result.Contains(piece.x))
            {
                result.Add(piece.x);
            }
        });

        return result;
    }

    //Lo que hara esta funcion es retornar un valor verdadero o falso si recibe 2 espacios
    //y los espacios estan cerca o lejos
    public bool IsCloseto(Tile start, Tile end)
    {
        if(Math.Abs((start.x - end.x))==1 && start.y == end.y)
        {
            return true;
        }
        if (Math.Abs((start.y-end.y))== 1 && start.x == end.x)
        {
            return true;
        }
        return false;
    }

    bool HasPreviousMatches(int posx, int posy)
    {
        var downMatches = GetMatchByDirection(posx,posy, new Vector2(0,-1),2);
        var leftMatches = GetMatchByDirection(posx,posy, new Vector2(-1,0),2);

        if (downMatches == null) downMatches = new List<Piece>();
        if (leftMatches == null) leftMatches = new List<Piece>();

        return(downMatches.Count > 0 || leftMatches.Count > 0);
    }

    public List<Piece> GetMatchByDirection(int xpos, int ypos, Vector2 direction, int minPieces=3)
    {
        List<Piece> matches = new List<Piece>();
        Piece startPiece = pieces[xpos,ypos];
        matches.Add(startPiece);

        int nextX;
        int nextY;
        int maxVal = widht>height?widht : height;

        for (int i = 1; i < maxVal; i++)
        {
            nextX = xpos + ((int)direction.x * i);
            nextY = ypos + ((int)direction.y * i);

            if (nextX >= 0 && nextX < widht && nextY >= 0 && nextY < height)
            {
                var nextPiece = pieces[nextX,nextY];
                if (nextPiece!= null && nextPiece.pieceType == startPiece.pieceType)
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
        
                }
            }    
        }

            if(matches.Count >=minPieces)
            {
                return matches;
            }
            return null;
        
    }

    public List<Piece> GetmatchByPiece(int xpos, int ypos, int minPieces = 3)
    {
        var upMatch = GetMatchByDirection(xpos,ypos,new Vector2(0,1),2);
        var downMatch = GetMatchByDirection(xpos,ypos,new Vector2(0,-1),2);
        var rigthMatch = GetMatchByDirection(xpos,ypos,new Vector2(1,0),2);
        var leftMatch = GetMatchByDirection(xpos,ypos,new Vector2(-1,0),2);

        if (upMatch == null) upMatch = new List<Piece>();
        if (downMatch == null) downMatch = new List<Piece>();
        if (rigthMatch == null) rigthMatch = new List<Piece>();
        if (leftMatch == null) leftMatch = new List<Piece>();

        var verticalMatches = upMatch.Union(downMatch).ToList();
        var horizontalMatches  = leftMatch.Union(rigthMatch).ToList();

        var foundMatches = new List<Piece>();

        if (verticalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(verticalMatches).ToList();
        }
        if (horizontalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(horizontalMatches).ToList();
        }
        return foundMatches;
    }
}
