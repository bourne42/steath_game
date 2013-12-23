using UnityEngine;
using System.Collections;

public class NPCManager : MonoBehaviour {
	
	CharacterMotorC playerMotor;
	NavGridScript navScript;
	BasicBehaviorScript[] npc;
	
	public Vector3 lastPlayerLocation;
	public Vector3 lastVelocity;
	NavGridScript.NavGridCell lastPlayerCell;
	
	enum State {Idle, TrapPlayer, GiveOrders, SearchForPlayer};
	private State state;
	
	private ArrayList keyPoints;
	private ArrayList attackers;
	private int nextToAssign=0;
	
	private float timeSinceSeen = 0;
	public float timeToGiveUp = 10;
	
	public int numToPursue = 2;
	
	// Use this for initialization
	void Start () {
		state = State.Idle;
		playerMotor = GameObject.Find("First Person Controller").GetComponent<CharacterMotorC>();
		navScript = GetComponent<NavGridScript>();
		GameObject[] npcObjects = GameObject.FindGameObjectsWithTag("NPC");
		npc = new BasicBehaviorScript[npcObjects.Length];
		for(int i=0; i<npcObjects.Length; i++)
			npc[i]=npcObjects[i].GetComponent<BasicBehaviorScript>();
		
		keyPoints = new ArrayList();
		attackers = new ArrayList();
	}
	
	private class CloseToPlayerSorter : IComparer
	{
		Vector3 loc;
		public CloseToPlayerSorter(Vector3 locIn)
		{
			loc=locIn;
		}
		int IComparer.Compare( object xo, object yo )  
		{
			BasicBehaviorScript x = (BasicBehaviorScript)xo;
			BasicBehaviorScript y = (BasicBehaviorScript)yo;
			return System.Convert.ToInt32((x.transform.position-loc).magnitude - (y.transform.position-loc).magnitude);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(state == State.TrapPlayer)
		{
			attackers.Clear();
			int numToGet = Mathf.Min(numToPursue, npc.Length);
			foreach(BasicBehaviorScript b in npc)
				attackers.Add(b);
			attackers.Sort(new CloseToPlayerSorter(lastPlayerLocation));
			attackers.RemoveRange(numToGet, attackers.Count-numToGet);
			
			nextToAssign = 0;
			state = State.GiveOrders;
		}
		else if(state == State.GiveOrders)
		{
			BasicBehaviorScript b = (BasicBehaviorScript)attackers[nextToAssign];
			b.setGoal(getNewOrders(b));
			nextToAssign++;
			if(nextToAssign==attackers.Count)
				state=State.SearchForPlayer;
		}
		else if(state == State.SearchForPlayer)
		{
//			Debug.Log (timeSinceSeen);
			if(timeSinceSeen>=timeToGiveUp)
			{
				state = State.Idle;
				foreach(BasicBehaviorScript b in npc)
					b.setAlertLevel(2, Vector3.zero);
			}
		}
		timeSinceSeen+=Time.deltaTime;
	}

	public NavGridScript.NavGridCell getNewOrders(BasicBehaviorScript npc)
	{
		if(state == State.Idle)
		{
			NavGridScript.NavGridCell maxCell=navScript.navGrid[npc.xMin, npc.yMin];
			float distToCell = (navScript.navGridCellWorldPosition(maxCell)-npc.transform.position).magnitude;
			for(int i=npc.xMin; i<=npc.xMax; i++)
			{
				for(int j=npc.yMin; j<=npc.yMax; j++)
				{
					if(!navScript.navGrid[i,j].traversible)
						continue;
					float distToNewCell = (navScript.navGridIndexToWorldPosition(i,j)-npc.transform.position).magnitude;
					if(navScript.navGrid[i,j].lastVisited + (distToNewCell*npc.getVelocity()) > 
						maxCell.lastVisited+(distToCell*npc.getVelocity()))
					{
						maxCell = navScript.navGrid[i,j];
						distToCell = distToNewCell;
					}
				}
			}
			return maxCell;
		}
		
		NavGridScript.NavGridCell toReturn = lastPlayerCell;
		while(toReturn.lastVisited<4 || 
			(navScript.navGridCellWorldPosition(toReturn)-npc.transform.position).magnitude<2 ||
			!toReturn.traversible)
		{
			int range=Mathf.Min(6, System.Convert.ToInt32(timeToGiveUp * 3));
			int i=Random.Range(lastPlayerCell.gridI-range,lastPlayerCell.gridI+range);
			int j=Random.Range(lastPlayerCell.gridJ-range,lastPlayerCell.gridJ+range);
			if(i>=0 && j>=0 && i<NavGridScript.navGridResolution && j<NavGridScript.navGridResolution)
				toReturn = navScript.navGrid[i,j];
		}
		
		return toReturn;
	}
	
	public void alertedToPlayer(Vector3 playerLocation)
	{
		float difference = (lastPlayerLocation-playerLocation).magnitude;
		lastPlayerLocation = playerLocation;
		lastPlayerCell = navScript.worldPositionToClosestNavGridCell(lastPlayerLocation);
		lastVelocity = playerMotor.getVelocity();
		timeSinceSeen=0;
		
		if(state == State.Idle || state == State.SearchForPlayer || difference>1)
		{
			state = State.TrapPlayer;
		}
		
		foreach(BasicBehaviorScript b in npc)
			b.setAlertLevel(3, Vector3.zero);
	}
}
