using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

class GridStatus
{
    public int GridPosition_x;
    public int GridPosition_y; 
}

public class GridSys : MonoBehaviour
{
    List<GridStatus> GridList=new List<GridStatus>();
    int[] GridScale = {16,16};
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < GridScale[0]; i++)
        {
            for(int j = 0; j < GridScale[1] ; j++)
            {
                GridStatus Cell=new GridStatus();
                Cell.GridPosition_x=i;
                Cell.GridPosition_y=j;
                GridList.Add(Cell);
            }
            
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
