using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

internal class Timing
{
	private static TimeSpan ts = TimeSpan.Zero;

	public static int GetTimeSpan => (int)Math.Round((DateTime.Now - Now).TotalMilliseconds, MidpointRounding.AwayFromZero);

	public static DateTime Now
	{
		get
		{
			double value = 8.0 - (DateTime.Now - DateTime.UtcNow).TotalHours;
			return DateTime.Now.Add(ts).AddHours(value);
		}
	}

	public static DateTime LocalToUtc8(DateTime dt)
	{
		double value = 8.0 - (DateTime.Now - DateTime.UtcNow).TotalHours;
		return dt.AddHours(value);
	}

	public static void Resync()
	{
		Thread thread = new Thread(TimeSync);
		thread.IsBackground = true;
		thread.Start();
	}

	public static void Resync(DateTime dt)
	{
		ts = dt - DateTime.Now;
	}

	private static void TimeSync()
	{
		ts = GetNetworkTime("tick.stdtime.gov.tw") - DateTime.Now;
	}

	public static DateTime GetNetworkTime(string ntpServer)
	{
		byte[] array = new byte[48];
		array[0] = 27;
		try
		{
			IPEndPoint remoteEP = new IPEndPoint(Dns.GetHostEntry(ntpServer).AddressList[0], 123);
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.Connect(remoteEP);
				socket.Send(array);
				socket.Receive(array);
				socket.Close();
			}
		}
		catch (SocketException ex)
		{
			Logger.FileLog.Error("GetNetworkTime() " + ex.Message);
			return DateTime.Now;
		}
		long x = BitConverter.ToUInt32(array, 40);
		ulong x2 = BitConverter.ToUInt32(array, 44);
		long num = SwapEndianness((ulong)x);
		x2 = SwapEndianness(x2);
		ulong num2 = (ulong)(num * 1000 + (long)(x2 * 1000 / 4294967296uL));
		return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)num2).ToLocalTime();
	}

	private static uint SwapEndianness(ulong x)
	{
		return (uint)(((x & 0xFF) << 24) + ((x & 0xFF00) << 8) + ((x & 0xFF0000) >> 8) + ((x & 4278190080u) >> 24));
	}
}
