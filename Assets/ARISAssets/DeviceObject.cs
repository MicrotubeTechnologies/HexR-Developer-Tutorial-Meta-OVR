namespace ARIS
{
	public class DeviceObject
	{
		public string Address;
		public string Name;
		public string Model;
		public string Rssi;

		public DeviceObject()
		{
			Address = "";
			Name = "";
			Model = "";
			Rssi = "";
		}

		public DeviceObject(string address, string name, string model, string rssi)
		{
			Address = address;
			Name = name;
			Model = model;
			Rssi = rssi;
		}
	}
}
