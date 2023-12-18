using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Libreria de DOTween
using DG.Tweening;
public class Piece : MonoBehaviour
{
    public int x;
    public int y;
    public Board board;

    public enum type
    {
        elephant,
        giraffe,
        hippo,
        monkey,
        panda,
        parrot,
        penguin,
        pig,
        rabbit,
        snake
    };

    public type pieceType;
    public void SetUp(int x_, int y_, Board board_)
    {
        x = x_;
        y = y_;
        board = board_;
    }

    //Funcion para mover pieza
    public void Move(int desX, int desY)
    {
        transform.DOMove(new Vector3(desX,desY, -5),0.25f).SetEase(Ease.InOutCubic).onComplete = () =>
        {
            x = desX;
            y = desY;
        };
    }

    [ContextMenu("Test Move")]
    public void MoveTest()
    {
        Move(0,0);
    }
   
}
