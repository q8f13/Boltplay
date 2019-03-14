using UdpKit;
using UnityEngine;

public class InputArrayToken : Bolt.IProtocolToken
{
	private Vector2[] _inputs;
	public Vector2[] GetInputs{get{return _inputs;}}
	public int Count{get{return GetInputs.Length;}}

	private System.Text.StringBuilder _sb;

	public InputArrayToken()
	{
		_sb = new System.Text.StringBuilder(500);
	}

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
			byte_arr[i] = byte.Parse(ipt.ToString());
		}

		return byte_arr;
	}

	public void Read(UdpPacket packet)
	{
		int count = packet.ReadInt();
		// byte[] byte_arr = packet.ReadByteArray(count);
		string raw = packet.ReadString();
		// string[] input_raw = new string[2];
		string[] input_raw = raw.Split('|');

		Debug.Assert(count == input_raw.Length, "count should be the same");
		_inputs = new Vector2[count];
		string[] foo = new string[2];
		for(int i=0;i<count;i++)
		{
			// raw = byte_arr[i].ToString();
			// input_raw = raw.Substring(1, raw.Length - 1).Split(',');
			foo = input_raw[i].Substring(1, input_raw[i].Length - 1).Split(',');
			_inputs[i].x = float.Parse(foo[0]);
			_inputs[i].y = float.Parse(foo[1]);
			// _inputs[i].x = float.Parse(input_raw[0]);
			// _inputs[i].y = float.Parse(input_raw[1]);
		}
	}

	public void Write(UdpPacket packet)
	{
		int count = _inputs == null ? 0 : _inputs.Length;

		_sb.Remove(0, _sb.Length);

		for(int i=0;i<count;i++)
		{
			if(i > 0)
				_sb.Append('|');
			_sb.Append(_inputs[i].ToString());
		}

		try
		{
			packet.WriteInt(count);
			// packet.WriteByteArray(ConvertInputs());
			packet.WriteString(_sb.ToString());
		}
		catch (System.Exception e)
		{
			throw e;
		}

		// packet.write
		// throw new System.NotImplementedException();
	}
}