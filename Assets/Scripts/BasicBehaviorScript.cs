using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BasicBehaviorScript : MonoBehaviour {

	public NavGridScript navScript = null;

	public int xMin, xMax, yMin, yMax;
	public int coverWidth, coverHeight;

	private const float walkSpeed = 1f, runSpeed=2f, maxAcc=.1f;

	private NavGridScript.NavGridCell currentGoal=null;

	private Vector2 velocity;

	public bool canSeePlayer=false;
	public bool canFeelPlayer=false;

	private float timeSeenPlayer=0;

	NPCManager manager;

	private float timeSinceAttack = 0;
	float timeBetweenAttacks = 1.0f;

	private float timeSinceIncrease = 0;
	public float decreaseAlertRate = 6;

	public float halfFOV = 30;

	// Current path the character will travel
	List<NavGridScript.NavGridCell> path = null;

	private int alertLevel=0;// alert=0..3, 3 means npc is aware of player

	void Start () {
		navScript = GameObject.Find("Ground").GetComponent<NavGridScript>();
		manager = GameObject.Find("Ground").GetComponent<NPCManager>();
		path = new List<NavGridScript.NavGridCell>();
		velocity = new Vector2(0,0);

		NavGridScript.NavGridCell min = navScript.worldPositionToClosestNavGridCell(transform.position);
		NavGridScript.NavGridCell max = navScript.worldPositionToClosestNavGridCell(transform.position+new Vector3(coverWidth, 0, coverHeight));
		xMax = max.gridI;
		yMax = max.gridJ;
		xMin = min.gridI;
		yMin = min.gridJ;
	}

	// Update is called once per frame
	void Update () {
		NavGridScript.NavGridCell currentCell = navScript.worldPositionToClosestNavGridCell(this.transform.position);
		Vector3 playerLoc = GameObject.Find("First Person Controller").transform.position;

		//'visit' all close by cells, randomly decides when to do it (1/4 of time, should run fast enough that fine
		if(Random.Range(1,3)==1)
			for(int i=Mathf.Max(currentCell.gridI-3,0); i<=Mathf.Min(currentCell.gridI+3, NavGridScript.navGridResolution); i++)
			{
				for(int j=Mathf.Max(currentCell.gridJ-3,0); j<=Mathf.Min(currentCell.gridJ+3, NavGridScript.navGridResolution); j++)
				{
					navScript.navGrid[i,j].lastVisited=0;
				}
			}

		// Retrieve a new goal, should move this block into npc manager
		if(currentGoal == null || path.Count==0 || navScript.distanceFromCell(currentGoal, this.transform.position)<1)
		{
			setGoal(manager.getNewOrders(this));
		}
		//follow path:
		followPath();
//		Debug.Log(path.Count);

		//update if npc can see the player
		canSeePlayer = false;
		Vector3 playerDirection = playerLoc - this.transform.position;
		float pAngle = (Mathf.Atan2(playerDirection.x, playerDirection.z)*180/Mathf.PI)-90;
		float myAngle = velocityDirection();
		float angleBetween = Mathf.Abs(pAngle-myAngle)%360;
		if(angleBetween>180)
			angleBetween-=360;
		float horizontalDistance = Mathf.Sqrt(Mathf.Pow(playerDirection.x,2)+Mathf.Pow(playerDirection.z,2));
		if(Mathf.Abs(angleBetween)<=halfFOV && Mathf.Atan2(playerDirection.y, horizontalDistance)*180/Mathf.PI<60 &&
			playerDirection.magnitude<80)
		{
			canSeePlayer = !Physics.Raycast(this.transform.position, playerDirection, playerDirection.magnitude);
		}

		//update if npc can feel player
		canFeelPlayer = (navScript.flattenVec(playerLoc)-navScript.flattenVec(this.transform.position)).magnitude<1 &&
			(playerLoc.y-this.transform.position.y)<2;

		// update alertness based on sight
		float distanceAlwaysSeeCutoff = 8;
		if(canFeelPlayer)
			setAlertLevel(3, playerLoc);
		if(canSeePlayer && playerDirection.magnitude<distanceAlwaysSeeCutoff)
			setAlertLevel(3, playerLoc);
		else if(!awareOfPlayer() && canSeePlayer)
		{
			float newTimeSeen = timeSeenPlayer+Time.deltaTime;
			float distanceMod = ((playerDirection.magnitude-distanceAlwaysSeeCutoff)/10) + 1;

			float time1=.8f*distanceMod;
			float time2=2f*distanceMod;
			float time3=3f*distanceMod;
			if(timeSeenPlayer==0)
				increaseAlertLevel(playerLoc);
			else if(timeSeenPlayer<time1 && newTimeSeen>=time1 ||
				timeSeenPlayer<time2 && newTimeSeen>=time2 ||
				timeSeenPlayer<time3 && newTimeSeen>=time3)
				increaseAlertLevel(playerLoc);

			timeSeenPlayer=newTimeSeen;
		}
		else if(awareOfPlayer() && canSeePlayer)
			setAlertLevel(3, playerLoc);

		if(!canSeePlayer)
			timeSeenPlayer=0;

		// rotate npc
		if((manager.lastPlayerLocation-transform.position).magnitude<9 && awareOfPlayer())
			face (pAngle);
		else if(velocity.magnitude!=0)
			face (velocityDirection());

		if(canSeePlayer && awareOfPlayer())
		{
			float distToPlayer = navScript.flattenVec(playerDirection).magnitude;
			float groundAttackDistance = 4;
			float airAttackDistance = 7;
			if(distToPlayer<groundAttackDistance ||
				(!navScript.worldPositionToClosestNavGridCell(playerLoc).traversible &&
				distToPlayer<airAttackDistance))
				attackPlayer();
		}

		if(alertLevel>0 && alertLevel<3)
		{
			timeSinceIncrease+=Time.deltaTime;
			if(timeSinceIncrease>=decreaseAlertRate)
			{
				timeSinceIncrease=0;
				alertLevel--;
			}
		}
	}

	private void attackPlayer()
	{
		timeSinceAttack+=Time.deltaTime;
		if(timeSinceAttack>timeBetweenAttacks)
		{
			GameObject.Find("First Person Controller").GetComponent<FPSInputControllerC>().attack();
			timeSinceAttack = 0;
		}
	}

	/**
	 * Requires newGoal to be traversible
	 */
	public void setGoal(NavGridScript.NavGridCell newGoal)
	{
		currentGoal = newGoal;
		findPath();
	}

	public void investigateLocation(Vector3 loc)
	{
		setGoal(closestTraversibleCell(navScript.worldPositionToClosestNavGridCell(loc), 5));
	}

	/**
	 * Sound level: 0=nothing... dunno the rest
	 */
	public void hearSound(float noiseLevel, Vector3 loc)
	{
//		if(awareOfPlayer())
//			return;
		float alertModifier = 1+(alertLevel*.3f);
		noiseLevel*=alertModifier;

		if(noiseLevel>27)
			increaseAlertLevel(3, loc);
		else if(noiseLevel>13)
			increaseAlertLevel(2, loc);
		else if(noiseLevel>8)
			increaseAlertLevel(1, loc);
	}

	public void increaseAlertLevel(Vector3 loc)
	{
		timeSinceIncrease = 0;
		if(alertLevel<3)
			setAlertLevel(alertLevel+1, loc);
	}
	public void increaseAlertLevel(int level, Vector3 loc)
	{
		timeSinceIncrease = 0;
		if(level>alertLevel)
			setAlertLevel(level, loc);
	}

	public void setAlertLevel(int level, Vector3 loc)
	{
		timeSinceIncrease = 0;
		alertLevel = level;
		if(level==2 && loc.magnitude>1)
			investigateLocation(loc);
		if(alertLevel == 3 && loc.magnitude>1)
			manager.alertedToPlayer(loc);

		transform.FindChild("AlertIndicator").renderer.enabled = alertLevel!=0;

		if(alertLevel>0)
		{
			Color alertColor = Color.black;
			if(alertLevel==1)
				alertColor = Color.green;
			else if(alertLevel==2)
				alertColor = Color.yellow;
			else if(alertLevel==3)
				alertColor = Color.red;
			transform.FindChild("AlertIndicator").renderer.material.color = alertColor;
		}
	}

	public bool awareOfPlayer()
	{
		return alertLevel==3;
	}

	private void followPath()
	{
		if(path.Count==0)
			return;
		if(navScript.distanceFromCell(path[0], this.transform.position)<1)
			path.RemoveAt(0);
		if(path.Count==0)
			return;

		Vector3 dest3 = navScript.navGridCellWorldPosition(path[0]);

		float distance = (dest3-this.transform.position).magnitude;
		if(distance<=2 && path.Count>1)
		{
			Vector3 nextDest = navScript.navGridCellWorldPosition(path[1]);
			Vector3 nextDirection = (nextDest - dest3).normalized;
			dest3 -= nextDirection*(distance-2);
		}

		Vector3 direction3 = dest3-this.transform.position;

		if(direction3.magnitude<1f)
			velocity += Vector2.ClampMagnitude(-navScript.flattenVec(direction3), getAcceleration());
		else
			velocity += getAcceleration()*(navScript.flattenVec(direction3).normalized);
		velocity = Vector2.ClampMagnitude(velocity, getVelocity());
		if(velocity.magnitude<.05)
			velocity = new Vector2(0,0);
		this.transform.position += Time.deltaTime * navScript.expandVec(velocity);
	}

	public float getVelocity()
	{
		return awareOfPlayer()? runSpeed : walkSpeed;
	}

	private float getAcceleration()
	{
		if(awareOfPlayer())
			return maxAcc*1.5f;
		return maxAcc;
	}

	private void face(float direction)
	{
		this.transform.Rotate(0, Mathf.Min((direction-this.transform.rotation.eulerAngles.y), 1), 0);
	}

	private NavGridScript.NavGridCell closestTraversibleCell(NavGridScript.NavGridCell startingCell, int maxDist)
	{
		for(int d=0; d<=maxDist; d++)
			for(int i=Mathf.Max (startingCell.gridI-d, 0); i<=Mathf.Min (startingCell.gridI+d, NavGridScript.navGridResolution); i++)
				for(int j=Mathf.Max (startingCell.gridJ-d, 0); j<=Mathf.Min (startingCell.gridJ+d, NavGridScript.navGridResolution); j++)
					if(navScript.navGrid[i,j].traversible)
						return navScript.navGrid[i,j];

		return null;
	}

	private float velocityDirection()
	{
		return (Mathf.Atan2(velocity.x, velocity.y)*180/Mathf.PI)-90;
	}

	void findPath()
	{
		NavGridScript.NavGridCell start=navScript.worldPositionToClosestNavGridCell(this.transform.position);
		NavGridScript.NavGridCell goal=currentGoal;

		float heuristicMod = 1f;

		path.Clear();
		Heap open = new Heap(16);
		Dictionary<NavGridScript.NavGridCell, Node> closed = new Dictionary<NavGridScript.NavGridCell, Node>();

		Node startNode = new Node(start, null, 0, (int)(heuristicMod*heuristic(start)));
		open.Insert(startNode);

		Node current=null;
		int c=0;
		while(!open.IsEmpty())
		{
			c++;
			current=open.Dequeue();
			closed.Add(current.cell, current);
			if(current.cell==goal)
				break;

			List<NavGridScript.NavGridCell> neighbors = navScript.neighbors(current.cell);
			foreach(NavGridScript.NavGridCell newCell in neighbors)
			{
				if(!closed.ContainsKey(newCell))
				{
					if(!open.HeapDecreaseKey(newCell, current.costSoFar+1+(int)(heuristicMod*heuristic(newCell))))
					{
						Node newNode = new Node(newCell, current, current.costSoFar+1,
							current.costSoFar+1+(int)(heuristicMod*heuristic(newCell)));
						open.Insert(newNode);
					}
				}
			}
		}

		//if the current cell is not goal then something went wrong, wait till next time to recompute
		if(current!=null && current.cell == goal)
		{
			Node temp = current;
			while(temp.previous != null)
			{
				path.Insert(0,temp.cell);
				Vector3 tPos = navScript.navGridCellWorldPosition(temp.cell);
				Node next = temp.previous;

				//removes inbetween nodes that aren't needed
				while(next.previous != null)
				{
					Vector3 nPos = navScript.navGridCellWorldPosition(next.previous.cell);
					if(Physics.CapsuleCast(tPos, tPos+new Vector3(0, NavGridScript.npcHeight, 0), 1f, nPos-tPos,  System.Convert.ToSingle((nPos-tPos).magnitude)))
						break;
					next=next.previous;
				}

				temp = next;
			}
			path.Insert(0,temp.cell);
		}
		else
			currentGoal = null;
	}

	void OnDrawGizmos()
	{
		if(navScript == null)
			return;
		NavGridScript.NavGridCell cell = navScript.worldPositionToClosestNavGridCell(this.transform.position);
		Vector3 diff = new Vector3(0f,.2f,0f);
		Gizmos.color = Color.white;
		Gizmos.DrawCube(navScript.navGridCellWorldPosition(cell)+diff, new Vector3(navScript.cellSizeI, 0, navScript.cellSizeJ));

		if(canSeePlayer || canFeelPlayer || alertLevel>0)
		{
			if(alertLevel==1)
				Gizmos.color = Color.green;
			else if(alertLevel==2)
				Gizmos.color = Color.yellow;
			else if(alertLevel==3)
				Gizmos.color = Color.red;
			else
				Gizmos.color = Color.magenta;
//			Gizmos.DrawSphere(this.transform.position+new Vector3(0, 1, 0), .3f);
		}

		if(path != null && path.Count>0)
		{
			Gizmos.color = Color.blue;
			for(int i=0; i<path.Count-1; i++)
			{
				Vector3 qw = navScript.navGridCellWorldPosition(path[i]);
				Vector3 er = navScript.navGridCellWorldPosition(path[i+1]);
				Gizmos.DrawLine(qw, er);
			}

			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(navScript.navGridCellWorldPosition(path[path.Count-1]), 1);
		}
	}

	/**
	 * Heuristic function
	 * Left abstract if wanted to improve
	 * */
	private int heuristic(NavGridScript.NavGridCell c)
	{
		return Mathf.Abs(c.gridI-currentGoal.gridI) + Mathf.Abs(c.gridJ-currentGoal.gridJ);
	}
}


public class Node
{
	public NavGridScript.NavGridCell cell;
	public Node previous;
	public int costSoFar;
	public int estimatedTotalCost;

	public Node(NavGridScript.NavGridCell cell)
	{
		this.cell = cell;
	}

	public Node(NavGridScript.NavGridCell cell, Node prev, int costSoFar, int estimatedTotalCost)
	{
		this.cell = cell;
		this.previous = prev;
		this.costSoFar = costSoFar;
		this.estimatedTotalCost = estimatedTotalCost;
	}
}

/**
 * Heap Code copied from internet and modified to be a min heap and accept Node class
 */
class Heap // Min Heap
{
    private Node[] heapArray;
    private int maxSize;
    private int currentSize;
    public Heap(int maxHeapSize)
    {
        maxSize = maxHeapSize;
        currentSize = 0;
        heapArray = new Node[maxSize];
    }
    public bool IsEmpty()
    { return currentSize==0; }
    public bool Insert(Node newNode)
    {
        if(currentSize==maxSize)
		{
			Node[] newArray = new Node[maxSize*2];
			for(int i=0; i<maxSize; i++)
				newArray[i]=heapArray[i];
			heapArray = newArray;
			maxSize*=2;
		}
        heapArray[currentSize] = newNode;
        CascadeUp(currentSize++);
        return true;
    }
    public void CascadeUp(int index)
    {
        int parent = (index-1) / 2;
        Node bottom = heapArray[index];
        while( index > 0 && heapArray[parent].estimatedTotalCost > bottom.estimatedTotalCost )
        {
            heapArray[index] = heapArray[parent];
            index = parent;
            parent = (parent-1) / 2;
        }
        heapArray[index] = bottom;
    }
    public Node Dequeue() // Remove maximum value node
    {
        Node root = heapArray[0];
        heapArray[0] = heapArray[--currentSize];
        CascadeDown(0);
        return root;
    }
    public void CascadeDown(int index)
    {
        int smallerChild;
        Node top = heapArray[index];
        while(index < currentSize/2)
        {
            int leftChild = 2*index+1;
            int rightChild = leftChild+1;
            if(rightChild < currentSize && heapArray[leftChild].estimatedTotalCost > heapArray[rightChild].estimatedTotalCost)
                smallerChild = rightChild;
            else
                smallerChild = leftChild;
            if( top.estimatedTotalCost <= heapArray[smallerChild].estimatedTotalCost )
                break;
            heapArray[index] = heapArray[smallerChild];
            index = smallerChild;
        }
        heapArray[index] = top;
    }
    public bool HeapDecreaseKey(NavGridScript.NavGridCell cell, int newTotalCost)
    {
		int index=-1;
		for(int i=0; i<currentSize; i++)
		{
			if(heapArray[i].cell==cell)
				index=i;
		}
        if(index<0 || index>=currentSize)
            return false;
		if(heapArray[index].estimatedTotalCost<newTotalCost)
			return true;
        heapArray[index].estimatedTotalCost = newTotalCost;
        CascadeUp(index);
        return true;
    }
}