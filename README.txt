Stealth Game
Final project for video game class
Created with unity

Instruction:
Move around environment and get to objective marked with a white sphere. There is a sparkling sphere directly above it (to see from far away). 
Enemies have varying level of alertness, green is minor, yellow is worse, and red means they are actively working together to attack you. When they get close enough you take damage. Health regenerates.
Crouching is significantly quieter.
Colors are randomly generated. Enemies are white. 

Features:
Head bobbing
Multiple player speeds
Enemies have multiple levels of alertness and respond differently (a bit) based on level
Will search last area the player was when lost
Gradual decrease in alert level


Controls:
wasd to move
Mouse wheel to adjust speed
Cntrl to toggle crouch
Space to jump
Q/E to lean left and right, this does not effect if the enemies can see you
Debug: R to reset alert levels of NPC's (lowest level)
C to screen caputure. Uses a random integer so has (low) chance of overwriting


Description of classes:
AuditoryManager: Takes in CharacterMotor and detects how much sound it (the player) makes and sends data to NPC's. Doesn't accound for obstacles, just distance.
BasicBehaviorScript: Individual AI. Handles path finding, movement, attacking, vision...
CharacterMotorC: Took CharacterMotor and converted from javascript into #. Added code for head bobbing, crouching, multiple speeds, and leaning.
FPSInputController: Started as C# version of FPSInputController (js). Handles different key inputs, taking damage, winning, and GUI.
NavGridScript: most code from Project2- builds grid from obstacles
NPCManager: Part of the AI that coordinates the multiple AI's. Provides targets for them to seek when they want a new objective. Recieves information about seeing the player and handle's it accordingly.

Notes:
No audio
The audio detection was acting a bit weird, didn't have time to debug but the code is there, was working at one point
A good amount of poorly organized code, my variables mixed in with predefined. 
Not great separation of public and private variables
I didn't make the only level very difficult or robust. I generally didn't test the final game for fairness because of time. 
