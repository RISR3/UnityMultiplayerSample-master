using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//NetworkCharacter Script

public class NetworkCharacter : MonoBehaviour
{
	
	private string networkID = "";
	private Vector3 inputVector;
	private bool moveable = false;
	private void Start()
	{
		if (moveable)
			InvokeRepeating("UpdateInput", 1, 0.016f);
	}
	private void Update()
	{
		if (!moveable)
			return;

		inputVector = Vector3.zero;

		if (Input.GetKey(KeyCode.W)) //W button
		{
			inputVector.z += 1;
		}
		if (Input.GetKey(KeyCode.S)) //S button
		{
			inputVector.z -= 1;
		}
		if (Input.GetKey(KeyCode.A)) //A Button
		{
			inputVector.y -= 1;
		}
		if (Input.GetKey(KeyCode.D)) //D Button
		{
			inputVector.y += 1;
		}
	}
	public void SetNetworkID(string id)
	{
		networkID = id;
	}
	public void Setmoveable(bool movement)
	{
		moveable = movement;
	}
	public void UpdateInput()
	{
		NetworkClient.instance.SendInput(inputVector);
	}
}
