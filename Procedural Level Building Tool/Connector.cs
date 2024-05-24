using System.Collections.Generic;
using ConnectorInfo;
#if UNITY_EDITOR
#endif
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

/*This class will act as a component. It will attach to an object and be positioned where the user wants.

The parameters here will be used by the generation manager when connecting the objects together*/



 [ExecuteInEditMode]
public class Connector : MonoBehaviour
{
	[SerializeField] private ConnectorColour colour = ConnectorColour.Red;
	[SerializeField] private int numPins = 1;
	[SerializeField] private float spacing = 0.9f;
	[SerializeField] private float scaleFactor = 0.25f;
	[SerializeField] private PinShape pinShape = PinShape.Default;
	[SerializeField] private List<Vector3> pinPosition;
	private bool isConnected = false;

	private int previousNumPins = 0;
	private Vector3 connectorScale;
	private BoxCollider col;
	private float headerLength = 0.5f;
	
	// OnEnable is called once, initialise the box collider properties here
	private void OnEnable()
	{
		previousNumPins = numPins;
		connectorScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
		pinPosition = new List<Vector3>();
	}

	private void OnDisable()
	{

	}
	

	//Called when something changes
	private void OnValidate() 
	{
		UpdatePins();
	}

	//Update the pin properties here
	private void UpdatePins()
	{
		/*If the user adds more pins, call the add pin function. The opposite for reducing pins*/
		if(previousNumPins < numPins) AddPins();
		if(previousNumPins > numPins) RemovePins();
		
		//Update the scale
		connectorScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
	}

	private void AddPins()
	{
		//Add new pins (difference)
		for (int i = previousNumPins; i < numPins; i++)
		{
			pinPosition.Add(Vector3.zero);
		}
		
		//Set prev pins to total pins
		previousNumPins = numPins;
	}

	private void RemovePins()
	{
		for (int i = previousNumPins; i > numPins; i--)
		{
			pinPosition.RemoveAt(pinPosition.Count - 1);
		}
		
		//Set prev pins to total pins
		previousNumPins = numPins;
	}

	/*Be careful because this function redraws every frame if a value on screen is changed*/
	private void OnDrawGizmos()
	{
		Gizmos.color = GetColour();

		//Calculate scaling factor so spacing between the pins stay the same regardless of scale
		float initialSpacing = 1f;
		float scaleSpacing = initialSpacing * connectorScale.x;
		float totalSpacing = (numPins - 1) * scaleSpacing;
		
		for (int i = 0; i < numPins; i++)
		{
			//Calculate the relative position of the pins based on the total width
			float relativeX = (i - (numPins - 1) / 2f) * (spacing * totalSpacing) / transform.lossyScale.x;
			
			// Update the Gizmos position based on the attached object's position and rotation, considering spacing
			Vector3 pinWorldPosition = transform.TransformPoint(new Vector3(relativeX, 0f, 0f) + pinPosition[i]);
			
			Gizmos.matrix = Matrix4x4.TRS(pinWorldPosition, transform.rotation * Quaternion.identity, Vector3.one);

			if (pinShape == PinShape.Square)
			{
				Gizmos.DrawWireCube(Vector3.zero, connectorScale);
			}

			if (pinShape == PinShape.Circle)
			{
				Gizmos.DrawWireSphere(Vector3.zero, connectorScale.x);
			}

			if (pinShape == PinShape.Triangle)
			{
				// Draw pyramid faces
				Gizmos.matrix = Matrix4x4.TRS(pinWorldPosition, transform.rotation * Quaternion.identity, Vector3.one);
				DrawPyramidFace(Vector3.up * (connectorScale.y / 2f), Vector3.forward * (connectorScale.z / 2f), Vector3.right * (connectorScale.x / 2f));
				DrawPyramidFace(Vector3.up * (connectorScale.y / 2f), -Vector3.forward * (connectorScale.z / 2f), Vector3.right * (connectorScale.x / 2f));
				DrawPyramidFace(Vector3.up * (connectorScale.y / 2f), -Vector3.forward * (connectorScale.z / 2f), -Vector3.right * (connectorScale.x / 2f));
				DrawPyramidFace(Vector3.up * (connectorScale.y / 2f), Vector3.forward * (connectorScale.z / 2f), -Vector3.right * (connectorScale.x / 2f));
			}
			
			Gizmos.matrix = Matrix4x4.identity;
			
			Gizmos.DrawRay(gameObject.transform.position, gameObject.transform.forward * headerLength);
			Gizmos.DrawRay(gameObject.transform.position, gameObject.transform.up * (headerLength / 2));
		}
	}
	
	private void DrawPyramidFace(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
	{
		Gizmos.DrawLine(vertex1, vertex2);
		Gizmos.DrawLine(vertex2, vertex3);
		Gizmos.DrawLine(vertex3, vertex1);
	}
}
