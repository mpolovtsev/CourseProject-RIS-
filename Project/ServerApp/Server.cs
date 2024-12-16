using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using SLELibrary;
using SolverApp;

namespace ServerApp
{
	public class Server
	{
		public IPAddress IpAddress { get; private set; }
		public int Port { get; private set; }
		Socket Socket { get; set; }
		public List<Solver> Solvers { get; set; }

		public Server(string ipAddress = "0.0.0.0", int port = 8080)
		{
			IpAddress = IPAddress.Parse(ipAddress);
			Port = port;
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Solvers = new List<Solver>();
		}

		public void Start()
		{
			IPEndPoint ipEndPoint = new IPEndPoint(IpAddress, Port);
			Socket.Bind(ipEndPoint);
			Socket.Listen();
			ThreadPool.QueueUserWorkItem(_ => AcceptClients());
			Console.WriteLine($"Сервер запущен по адресу http://{IpAddress}:{Port}...");
		}

		void AcceptClients()
		{
			while (true)
			{
				try
				{
					Socket clientSocket = Socket.Accept();
					ThreadPool.QueueUserWorkItem(ProcessClient!, clientSocket);
				}
				catch (SocketException)
				{
					break;
				}
			}
		}

		void ProcessClient(object state)
		{
			Socket clientSocket = (Socket)state;
			NetworkStream stream = new NetworkStream(clientSocket);
			string request = Read(stream);

			if (request.StartsWith("Solver client"))
			{
				Solvers.Add(new Solver(clientSocket));
				return;
			}

			string response = "";
			byte[] responseBytes;

			if (request.StartsWith("GET"))
				response = GetIndexPage();
			else if (request.StartsWith("POST"))
			{
				if (Solvers.Count == 0 || Solvers.All(s => s.IsBusy))
					response = GetResponseText("Нет доступных рабочих узлов для решения задачи.");
				else
				{
					string? body = GetHttpRequestBody(request);
                    SLE? sle = JsonConvert.DeserializeObject<SLE>(body,
						new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });

					if (sle == null)
						response = GetResponseText("Ошибка при чтении данных.");
					else
					{
						sle.SeidelMethodParallel(0.0001, 10000, Solvers);

						if (GetSourceHeader(request) == "Fields")
							response = GetResponseText(string.Join("\n", sle.Result));
						else if (GetSourceHeader(request) == "File")
							response = GetResponseJson(JsonConvert.SerializeObject(sle, Formatting.Indented));
					}
				}
			}

			responseBytes = Encoding.UTF8.GetBytes(response);
			stream.Write(responseBytes);
			clientSocket.Close();
		}

		string Read(NetworkStream stream)
		{
			StringBuilder data = new StringBuilder();
			byte[] buffer = new byte[1024];
			int length;

			do
			{
				length = stream.Read(buffer, 0, buffer.Length);
				data.Append(Encoding.UTF8.GetString(buffer, 0, length));
			}
			while (stream.DataAvailable);

			return data.ToString();
		}

		string? GetHttpRequestBody(string request)
		{
			int headersEndIndex = request.IndexOf("\r\n\r\n");
			string? body = headersEndIndex != -1 ? request.Substring(headersEndIndex + 4) : null;

			return body;
		}

		string? GetSourceHeader(string request)
		{
			string[] rows = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);

			foreach (string row in rows)
				if (row.StartsWith("Source:"))
					return row.Substring("Source:".Length).Trim();

			return null;
		}

		string GetIndexPage()
		{
			string response = File.ReadAllText("D:\\Course Project (DIS)\\App\\Project\\index.html");
			response = string.Concat(GetHeaders(200, "OK", "text/html; charset=utf-8", Encoding.UTF8.GetByteCount(response)), response);

			return response;
		}

		string GetResponseText(string answer)
		{
			string response = string.Concat(GetHeaders(200, "OK", "text/plain; charset=utf-8", Encoding.UTF8.GetByteCount(answer)), answer);

			return response;
		}

		string GetResponseJson(string answer)
		{
			string response = string.Concat(GetHeaders(200, "OK", "application/json", Encoding.UTF8.GetByteCount(answer)), answer);

			return response;
		}

		string GetHeaders(int code, string explanation, string contentType, int contentLength)
		{
			StringBuilder headers = new StringBuilder();
			headers.Append($"HTTP/1.1 {code} {explanation}").Append("\r\n");
			headers.Append($"Date: {DateTime.Now}").Append("\r\n");
			headers.Append($"Content-Type: {contentType}").Append("\r\n");
			headers.Append($"Content-Length: {contentLength}").Append("\r\n");
			headers.Append("\r\n");

			return headers.ToString();
		}

		public void Stop()
		{
			foreach (Solver solver in Solvers)
				solver.Socket.Send(Encoding.UTF8.GetBytes("Server is turned off"));

			Socket.Close();
		}
	}
}