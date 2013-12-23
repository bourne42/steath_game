using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NavGridScript : MonoBehaviour
{
	public class NavGridCell
	{
		public bool traversible;
		public int gridI = 0;
		public int gridJ = 0;
		
		public int lastVisited=0;
		
		public bool equals(NavGridCell other)
		{
			return this.gridI==other.gridI && this.gridJ==other.gridJ;
		}
	}
	
	public List<NavGridCell> neighbors(NavGridCell c)
	{
		List<NavGridCell> ret = new List<NavGridCell>();
		if(c.gridI>0 && navGrid[c.gridI-1, c.gridJ].traversible)
			ret.Add(navGrid[c.gridI-1, c.gridJ]);
		if(c.gridJ>0 && navGrid[c.gridI, c.gridJ-1].traversible)
			ret.Add(navGrid[c.gridI, c.gridJ-1]);
		if(c.gridI<navGridResolution-1 && navGrid[c.gridI+1, c.gridJ].traversible)
			ret.Add(navGrid[c.gridI+1, c.gridJ]);
		if(c.gridI<navGridResolution-1 && navGrid[c.gridI, c.gridJ+1].traversible)
			ret.Add(navGrid[c.gridI, c.gridJ+1]);
		
		return ret;
	}
	
	public const int navGridResolution = 200;
	
	public NavGridCell[,] navGrid = new NavGridCell[navGridResolution,navGridResolution];
	
	public readonly Vector2 navGridScale = new Vector2(100,100);
	
	public const float npcHeight = 3;
	
	public Vector3 navGridPosition
	{
		get {return this.transform.position - new Vector3(0f,0f,0f);}//(new Vector3(this.transform.lossyScale.x/2, 0f, this.transform.lossyScale.z/2));}
		set {Debug.Log ("Trying to change nav grid position"); this.transform.position = value;}
	}
	// set programatically:
	public readonly float cellSizeI = 2;
	public readonly float cellSizeJ = 2;
	
	public Vector2 flattenVec(Vector3 vec)
	{
		return new Vector3(vec.x, vec.z);
	}
	public Vector3 expandVec(Vector2 vec)
	{
		return new Vector3(vec.x, 0, vec.y);
	}

	public float distanceFromCell(NavGridCell cell, Vector3 loc)
	{
		return (flattenVec(navGridCellWorldPosition(cell))-flattenVec(loc)).magnitude;
	}
	public float distanceFromCell(int i, int j, Vector3 loc)
	{
		return (flattenVec(navGridIndexToWorldPosition(i, j))-flattenVec(loc)).magnitude;
	}
	
	//returns world position of center of some nav grid cell
	public Vector3 navGridIndexToWorldPosition(int i, int j)
	{
		i-=navGridResolution/2;
		j-=navGridResolution/2;
		return this.navGridPosition + new Vector3(i*this.cellSizeI + this.cellSizeI, 0, j * this.cellSizeJ + this.cellSizeJ);
	}
	public Vector3 navGridCenterWorldPosition(int i, int j)
	{
		return this.navGridIndexToWorldPosition(i,j)+(new Vector3(cellSizeI/2, 0, cellSizeJ/2));
	}
	
	//returns world position of center of some nav grid cell
	public Vector3 navGridCellWorldPosition(NavGridCell ngc)
	{
		return navGridIndexToWorldPosition(ngc.gridI, ngc.gridJ);
	}
	
	//returns nav grid cell closest to some world position (if contained, the containing cell)
	public NavGridCell worldPositionToClosestNavGridCell(Vector3 worldPos)
	{
		//transform into space of grid, taking into consideration rounding for integer indexes
		Vector2 localPos = flattenVec(worldPos) - flattenVec(this.navGridPosition);
		int i = (int)(localPos.x / this.cellSizeI);
		int j = (int)(localPos.y / this.cellSizeJ);
		
		i += navGridResolution/2;
		j += navGridResolution/2;
		
		//this fixes some error in the above
		Vector3 dist = worldPos - navGridCenterWorldPosition(i,j);
		i+= (int)(dist.x / this.cellSizeI);
		j+= (int)(dist.z / this.cellSizeJ);
		
		if (i < 0) i = 0;
		if (i >= this.navGrid.GetLength(0)) i = this.navGrid.GetLength(0) - 1;
		if (j < 0) j = 0;
		if (j >= this.navGrid.GetLength(1)) j = this.navGrid.GetLength(1) - 1;
		
		return this.navGrid[i,j];
	}
	
	public void drawNavGrid()
	{
		//draw the navigation grid.  I suggest using different colors for different kinds of cells (traversible or not).
		//gizmo colors can be set with Gizmos.color before making a gizmo draw call
		Vector3 diff = new Vector3(0f,.2f,0f);
		
		uint i = 0, j = 0;
		for (i = 0; i < this.navGrid.GetLength(0); i++)
		{
			for (j = 0; j < this.navGrid.GetLength(1); j++)
			{
				NavGridCell cell = this.navGrid[i,j];
				
				//draw cube
				if (cell.traversible)
				{
					Gizmos.color = new Color(0, 1-(cell.lastVisited/400f), .4f, 1-(cell.lastVisited/400f));
					Gizmos.DrawWireCube(this.navGridCellWorldPosition(cell)+diff, 
						new Vector3(this.cellSizeI, 0, this.cellSizeJ));
	
				}
				else
				{
					Gizmos.color = Color.red;
					
					Gizmos.DrawCube(this.navGridCellWorldPosition(cell)+diff, new Vector3(this.cellSizeI, 0, this.cellSizeJ));
				}
			}//loop over j
		}//loop over i
	}
	
	NavGridScript()
	{
		this.cellSizeI = this.navGridScale.x / navGrid.GetLength(0);
		this.cellSizeJ = this.navGridScale.y / navGrid.GetLength(1);
		int i = 0, j = 0;
		for (i = 0; i < this.navGrid.GetLength(0); i++)
		{
			for (j = 0; j < this.navGrid.GetLength(1); j++)
			{
				this.navGrid[i,j] = new NavGridCell();
				this.navGrid[i,j].gridI = i;
				this.navGrid[i,j].gridJ = j;
				this.navGrid[i,j].traversible = true;
				this.navGrid[i,j].lastVisited = 0;
			}
		}
	}
	
	private bool intersects(int i, int j)
	{
		Vector3 center = this.navGridIndexToWorldPosition(i,j);//+new Vector3(cellSizeI/2,0, cellSizeJ/2);
		RaycastHit hit;
		float rad=.7f;
		
		return Physics.SphereCast(center, rad, new Vector3(0f,1f,0f), out hit, npcHeight) || Physics.CheckSphere(center,rad);
	}
	
	void navGridInit()
	{
		foreach (NavGridCell cell in this.navGrid)
			cell.traversible = !(intersects (cell.gridI, cell.gridJ));
		
		// color cubes randomly
		foreach(GameObject go in GameObject.FindGameObjectsWithTag("Obstacle"))
		{
			int b = Random.Range(0,255);
			int g = Random.Range(0,250-b);
			go.renderer.material.color = new Color((250-b-g)/255f, g/255f, b/255f);
		}
	}
	
	uint framecounter = 0;
	void OnDrawGizmos()	//called even in Editor mode
	{
		//refresh the grid every 30 frames.  In Editor mode, you may have to wiggle the view for this to update.
		//you might want to make this update more frequently.  my computer tolerated having it happen every frame but your mileage may vary.
		if (framecounter++ % 60 == 0)
		{
			this.navGridInit();
		}
		
		//Draw the grid
		this.drawNavGrid();
	}
	
	void Start()
	{
		this.navGridInit();
	}
	
	void Update()
	{
		foreach(NavGridCell cell in navGrid)
			if(cell.traversible)
				cell.lastVisited+=1;
	}
}