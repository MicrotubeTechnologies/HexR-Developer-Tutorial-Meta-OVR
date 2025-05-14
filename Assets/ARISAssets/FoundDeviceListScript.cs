using UnityEngine;
using System.Collections.Generic;

namespace ARIS
{
	public class FoundDeviceListScript : MonoBehaviour
	{
		static public List<DeviceObject> DeviceAddressList;
		static public List<DeviceObject> ReOrderedDeviceList;
		static public List<DeviceObject> SensorDeviceAddress;

		// Use this for initialization
		void Start()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
