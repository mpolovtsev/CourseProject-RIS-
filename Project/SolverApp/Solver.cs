using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using TaskDataLibrary;

namespace SolverApp
{
	public class Solver
	{
		public IPAddress IpAddress { get; private set; }
		public int Port { get; private set; }
		public Socket Socket { get; private set; }
		public bool IsBusy { get; set; }

		public Solver(string ipAddress = "0.0.0.0", int port = 8080)
		{
			IpAddress = IPAddress.Parse(ipAddress);
			Port = port;
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IsBusy = false;
		}

		public Solver(Socket socket)
		{
			Socket = socket;
			IsBusy = false;
		}

		public void Start()
		{
			Socket.Connect(new IPEndPoint(IpAddress, Port));
			Console.WriteLine($"Клиент подключен к серверу по адресу {IpAddress}:{Port}");

			byte[] message = Encoding.UTF8.GetBytes("Solver client");
			Socket.Send(message);

			GetTask();
		}

		void GetTask()
		{
			string request;
			TaskDataNormalization? taskDataNormalization;
			TaskDataCalculation? taskDataCalculation;
			byte[] data;
			byte[] message;

			while (true)
			{
				request = Read();

				if (request == "Server is turned off")
				{
					Stop();
					return;
				}

				if (request == null)
					continue;

				taskDataNormalization = JsonConvert.DeserializeObject<TaskDataNormalization>(request,
					new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
				ConvertToIterativeForm(taskDataNormalization);

                data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataNormalization));
				message = Encoding.UTF8.GetBytes($"{data.Length, -20}").Concat(data).ToArray();
				Socket.Send(message);

				while (true)
				{
					request = Read();

					if (request == "End of data transfer")
						break;

					if (request == "Server is turned off")
					{
						Stop();
						return;
					}

					taskDataCalculation = JsonConvert.DeserializeObject<TaskDataCalculation>(request,
						new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
					CalcSum(taskDataCalculation);

					data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataCalculation));
					message = Encoding.UTF8.GetBytes($"{data.Length,-20}").Concat(data).ToArray();
					Socket.Send(message);
				}
			}
		}

		public string Read()
		{
			byte[] startBuffer = new byte[20];
			int totalRead = 0;
			int bytesRead;

			try
			{
				while (totalRead < startBuffer.Length)
				{
					bytesRead = Socket.Receive(startBuffer, totalRead, startBuffer.Length - totalRead, SocketFlags.None);
					totalRead += bytesRead;
				}
			}
			catch(SocketException)
			{
				Stop();
				return null;
			}

			string startRequest = Encoding.UTF8.GetString(startBuffer);

			if (startRequest.StartsWith("End"))
				return "End of data transfer";

			if (startRequest.StartsWith("Server"))
				return "Server is turned off";

			int messageLength = int.Parse(startRequest);
			byte[] messageBuffer = new byte[messageLength];
			totalRead = 0;
			StringBuilder requestBuilder = new StringBuilder();

			try
			{
				while (totalRead < messageLength)
				{
					bytesRead = Socket.Receive(messageBuffer, totalRead, messageLength - totalRead, SocketFlags.None);
					requestBuilder.Append(Encoding.UTF8.GetString(messageBuffer));
					totalRead += bytesRead;
				}
			}
			catch(SocketException)
			{
				Stop();
				return null;
			}

			return requestBuilder.ToString();
		}

		void ConvertToIterativeForm(TaskDataNormalization taskData)
		{
			double divider;

			for (int i = 0; i < taskData.A.Length; i++)
			{
				divider = taskData.A[i][taskData.Index];
				taskData.A[i][taskData.Index] = 0;

				for (int j = 0; j < taskData.A[0].Length; j++)
					if (taskData.Index != j)
						taskData.A[i][j] /= -divider;

				taskData.B[i] /= divider;
				taskData.Index++;
			}
		}

		void CalcSum(TaskDataCalculation taskData)
		{
			for (int i = 0; i < taskData.Row.Length; i++)
				taskData.Sum += taskData.Row[i] * taskData.X[i];
		}

		public void Stop()
		{
			Socket.Shutdown(SocketShutdown.Both);
			Socket.Close();
		}
	}
}