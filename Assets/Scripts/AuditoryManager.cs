using UnityEngine;
using System.Collections;

public class AuditoryManager : MonoBehaviour {
	
	CharacterMotorC playerMotor;
	NavGridScript navScript;
	BasicBehaviorScript[] npc;
	
	float crouchSoundDappener = .4f;
	float velocityToSound = 40.0f;
	
	// will only send a sound every frequencyToSend updates
	private float timeSinceSent = 0;
	private float timeToSend = 1.4f;
	
	// Use this for initialization
	void Start () {
		playerMotor = GameObject.Find("First Person Controller").GetComponent<CharacterMotorC>();
		navScript = GetComponent<NavGridScript>();
		GameObject[] npcObjects = GameObject.FindGameObjectsWithTag("NPC");
		npc = new BasicBehaviorScript[npcObjects.Length];
		for(int i=0; i<npcObjects.Length; i++)
			npc[i]=npcObjects[i].GetComponent<BasicBehaviorScript>();
	}
	
	// Update is called once per frame
	void Update () {
		if(playerMotor.isSlowestSpeed() || !playerMotor.isGrounded())
		{
			timeSinceSent = 0;
			return;
		}
//		Debug.Log(playerMotor.getVelocity().magnitude);
		
		float playerSpeed = playerMotor.getVelocity().magnitude;
		if(playerMotor.isCrouched())
			playerSpeed *= crouchSoundDappener;
		
		bool wasHeard=false;
		
		foreach(BasicBehaviorScript b  in npc)
		{
			float distance = Mathf.Pow((b.transform.position - playerMotor.transform.position).magnitude, 1.4f);
			float sound = velocityToSound * Mathf.Pow(playerSpeed,1.5f) / distance;
			if(sound>.1)
			{
				if(timeSinceSent == 0)
					b.hearSound(sound, playerMotor.transform.position);
				wasHeard=true;
			}
		}
		
		if(wasHeard)
		{
			timeSinceSent+=Time.deltaTime;
			if(timeSinceSent>=timeToSend)
				timeSinceSent=0;
		}
		else
			timeSinceSent=0;
	}
}
