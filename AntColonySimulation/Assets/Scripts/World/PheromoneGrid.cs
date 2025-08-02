using UnityEngine;

[DisallowMultipleComponent]
public class PheromoneGrid : MonoBehaviour
{
    public GridSettings settings;
    public static PheromoneGrid Instance { get; private set; }

    private float[,] foodTrail;
    private float[,] homeLayer;
    private int[,]   foodSource;
    int W, H;

    void Awake()
    {
        if (!settings) settings = Resources.Load<GridSettings>("GridSettings");
        W = settings.width;  H = settings.height;
        foodTrail = new float[W, H];
        homeLayer = new float[W, H];
        foodSource= new int  [W, H];
        Instance = this;
    }

    public void Deposit(int x,int y,bool toHome)
    {
        if(!Inside(x,y)) return;
        if(toHome) homeLayer[x,y]+=settings.depositAmount;
        else       foodTrail[x,y]+=settings.depositAmount;
    }
    public float Sample(int x,int y,bool toHome) =>
        Inside(x,y)? (toHome? homeLayer[x,y]: foodTrail[x,y]) : 0f;

    public void AddFood(int x,int y,int amt){ if(Inside(x,y)) foodSource[x,y]+=amt; }
    public bool TakeFood(int x,int y){
        if(!Inside(x,y)||foodSource[x,y]<=0) return false;
        foodSource[x,y]--; return true;
    }
    public int FoodAt(int x,int y)=> Inside(x,y)? foodSource[x,y]:0;
    bool Inside(int x,int y)=> (x>=0 && x<W && y>=0 && y<H);

    void LateUpdate()
    {
        float evap = 1f - settings.evaporationRate*Time.deltaTime;
        float diff = settings.diffusionRate*Time.deltaTime;

        var tmpF = new float[W,H];
        var tmpH = new float[W,H];

        for(int x=0;x<W;x++)
        for(int y=0;y<H;y++)
        {
            foodTrail[x,y]*=evap;
            homeLayer[x,y]*=evap;

            float sF = foodTrail[x,y]*diff;
            float sH = homeLayer[x,y]*diff;
            foodTrail[x,y]-=sF;  homeLayer[x,y]-=sH;

            if(x>0)   { tmpF[x-1,y]+=sF*0.25f; tmpH[x-1,y]+=sH*0.25f; }
            if(x<W-1) { tmpF[x+1,y]+=sF*0.25f; tmpH[x+1,y]+=sH*0.25f; }
            if(y>0)   { tmpF[x,y-1]+=sF*0.25f; tmpH[x,y-1]+=sH*0.25f; }
            if(y<H-1) { tmpF[x,y+1]+=sF*0.25f; tmpH[x,y+1]+=sH*0.25f; }
        }
        for(int x=0;x<W;x++) for(int y=0;y<H;y++){
            foodTrail[x,y]+=tmpF[x,y];
            homeLayer[x,y]+=tmpH[x,y];
        }
    }

    public float[,] FoodTrail=>foodTrail;
    public float[,] Home     =>homeLayer;
    public int[,]   FoodSource=>foodSource;
}
