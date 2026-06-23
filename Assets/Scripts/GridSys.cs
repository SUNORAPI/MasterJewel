using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

class GridStatus
{
    public int GridPosition_x;
    public int GridPosition_y; 
    public int[] PlayerExist;
    private float[] PlayerCoordinate_x;
    private float[] PlayerCoordinate_y;
}

public class GridSys : MonoBehaviour
{
    List<GridStatus> GridList=new List<GridStatus>();
    int[] GridScale = {16,16};
    private int PlayerIndex = 8;
    int GridSize = 2;
    int[] GridDistance_x;
    int[] GridDistance_y;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < GridScale[0]; i++)
        {
            for(int j = 0; j < GridScale[1]; j++)
            {
                GridStatus Cell=new GridStatus();
                Cell.GridPosition_x=i;
                Cell.GridPosition_y=j;
                for(int k = 0; k < PlayerIndex; k++)
                {
                    Cell.PlayerExist[k]=0;
                }
                GridList.Add(Cell);
            }
        }
        for(int i = 0; i < GridScale[0]; i++)
        {
            GridDistance_x[i]=i*GridSize;
        }
        for(int i = 0; i < GridScale[1]; i++)
        {
            GridDistance_y[i]=i*GridSize;
        }
        
    }

    // Update is called once per frame
    void Update()//プレイヤーの座標を取得してどのグリッドにいるか判定、リストに書き込む実装をする。
    {
       for(int i = 0; i < PlayerIndex ; i++)
        {
            for(int j = 0; j < GridScale[0]; j++)
            {
                for(int k = 0; k < GridScale[1]; k++)
                    if(PlayerCoordinate_x[i] <= GridDistance_x[j+1]&&PlayerCoordinate_x > GridDistance_x[j]&&PlayerCoordinate_y[i] <= GridDistance_y[k+1]&&PlayerCoordinate_y[i] > GridDistance_y[k])
                    {
                        
                    }
                    else
                    {
                        
                    }
            } 
        }
    }
}
