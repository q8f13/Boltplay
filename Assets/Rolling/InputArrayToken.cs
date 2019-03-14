using UdpKit;
using UnityEngine;

public class InputArrayToken : Bolt.IProtocolToken
{
	private Vector2[] _inputs;
	public Vector2[] GetInputs{get{return _inputs;}}
	public int Count{get{return GetInputs.Length;}}

	public InputArrayToken(Vector2[] inputs)
	{
		_inputs = inputs;
	}

	public InputArrayToken(Vector2 input)
	{
		_inputs = new Vector2[]{input};
	}

	byte[] ConvertInputs()
	{
		byte[] byte_arr = new byte[_inputs.Length];

		for(int i=0;i<_inputs.Length;i++)
		{
			Vector2 ipt = _inputs[i];
			byte.Parse(ipt.ToString());
		}

		return byte_arr;
	}

	public void Read(UdpPacket packet)
	{
		int count = packet.ReadInt();
		byte[] byte_arr = packet.ReadByteArray(count);
		_inputs = new Vector2[count];
		string raw;
		string[] input_raw = new string[2];
		for(int i=0;i<count;i++)
		{
			raw = byte_arr[i].ToString();
			input_raw = raw.Substring(1, raw.Length - 1).Split(',');
			// _inputs[i] = Vector2
			_inputs[i].x = float.Parse(input_raw[0]);
			_inputs[i].y = float.Parse(input_raw[1]);
		}
	}

	public void Write(UdpPacket packet)
	{
		packet.WriteInt(_inputs.Length);
		packet.WriteByteArray(ConvertInputs());
		// throw new System.NotImplementedException();
	}
}