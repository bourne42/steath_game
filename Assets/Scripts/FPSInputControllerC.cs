using UnityEngine;
using System.Collections;

    // Require a character controller to be attached to the same game object

    [RequireComponent (typeof (CharacterMotorC))]

    //RequireComponent (CharacterMotor)

    [AddComponentMenu("Character/FPS Input Controller C")]
    //@script AddComponentMenu ("Character/FPS Input Controller")


public class FPSInputControllerC : MonoBehaviour 
{
    public CharacterMotorC cmotor;
	
	private int maxHealth = 10;
	private int health;
	private float timeToHeal=0;
	private float timeStartHealing=5;
	private float timePerHealth = 1;
	
	private ArrayList splatters;
	
	public Texture bloodTex;
	
	private float bloodWidth, bloodHeight;
	
	private class BloodSplatter
	{
		public int x,y;
		public float alpha;
		
		public BloodSplatter(int xi, int yi)
		{
			x=xi;
			y=yi;
			alpha=0;
		}
	}

    // Use this for initialization
    void Awake () 
    {
		health = maxHealth;
		splatters = new ArrayList();
		bloodWidth = Screen.width*.15f;
		bloodHeight = bloodWidth;
		
        cmotor = GetComponent<CharacterMotorC>();
    }
	
	private void addSplatter()
	{
		BloodSplatter newSplatter = new BloodSplatter(
			Random.Range(System.Convert.ToInt32(.2f*bloodWidth), 
			System.Convert.ToInt32(Screen.width - 1.2f*bloodWidth)), 
			Random.Range(System.Convert.ToInt32(.2f*bloodWidth), 
			System.Convert.ToInt32(Screen.height - 1.2f*bloodHeight)));
		splatters.Add(newSplatter);
	}
	
	private void increaseBloodAlpha()
	{
		foreach(BloodSplatter b in splatters)
			b.alpha+=.1f;
	}
	
	private void decreaseBloodAlpha()
	{
		for(int i=0; i<splatters.Count; i++)
		{
			BloodSplatter b = (BloodSplatter)splatters[i];
			b.alpha-=.1f;
			if(b.alpha<=0)
			{
				splatters.RemoveAt(i);
				i--;
			}
		}
	}

 	public void attack()
	{
		health--;
		if(health==0)
			Application.LoadLevel (0); 
		addSplatter();
		increaseBloodAlpha();
		timeToHeal = 0;
	}
	
	void OnGUI()
	{
		float closest=100;
		foreach(GameObject go in GameObject.FindGameObjectsWithTag("NPC"))
			closest = Mathf.Min(closest, (go.transform.position - transform.position).magnitude);
		GUI.TextField(new Rect(10, 10, 150, 20), string.Format("Cloesest NPC: {0}", closest));
		
		foreach(BloodSplatter b in splatters)
		{
			GUI.color = new Color(1, 1, 1, b.alpha);
			GUI.DrawTexture(new Rect(b.x, b.y, bloodWidth, bloodHeight), bloodTex);
		}
	}

    // Update is called once per frame
    void Update () 
    {
		//regenerate health
		if(health<maxHealth)
		{
			timeToHeal+=Time.deltaTime;
			if(timeToHeal>timeStartHealing)
			{
				health++;
				timeToHeal-=timePerHealth;
				decreaseBloodAlpha();
				if(health==maxHealth)
					splatters.Clear();
			}
		}
		
        // Get the input vector from keyboard or analog stick
        Vector3 directionVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (directionVector != Vector3.zero) 
        {
            // Get the length of the directon vector and then normalize it
            // Dividing by the length is cheaper than normalizing when we already have the length anyway
            float directionLength = directionVector.magnitude;
            directionVector = directionVector / directionLength;

            // Make sure the length is no bigger than 1
            directionLength = Mathf.Min(1, directionLength);

            // Make the input vector more sensitive towards the extremes and less sensitive in the middle
            // This makes it easier to control slow speeds when using analog sticks
            directionLength = directionLength * directionLength;

            // Multiply the normalized direction vector by the modified length
            directionVector = directionVector * directionLength;

        }

        // Apply the direction to the CharacterMotor
        cmotor.inputMoveDirection = transform.rotation * directionVector;
        cmotor.inputJump = Input.GetButton("Jump");
		
		if(Input.GetAxis("Mouse ScrollWheel")>0)
			cmotor.goFaster();
		if(Input.GetAxis("Mouse ScrollWheel")<0)
			cmotor.goSlower();
		if(Input.GetButtonDown("Crouch"))
			cmotor.toggleCrouch();
		
		cmotor.lean=0;
		if(Input.GetButton("LeanRight"))
			cmotor.lean=1;
		if(Input.GetButton("LeanLeft"))
			cmotor.lean=-1;
		
		if(Input.GetButtonDown("ResetAlert"))
			foreach(GameObject go in GameObject.FindGameObjectsWithTag("NPC"))
				go.GetComponent<BasicBehaviorScript>().setAlertLevel(0, Vector3.zero);
		
		if(Input.GetKeyDown("c"))
			Application.CaptureScreenshot(string.Format("Screenshot{0}.png", Random.Range(0,100000)));
		
//		Debug.Log((GameObject.Find("Objective").transform.position - transform.position).magnitude);
		if((GameObject.Find("Objective").transform.position - transform.position).magnitude<2)
		{
			Debug.Log("VICTORY!");
			Application.Quit();
		}
    }

}