using Newtonsoft.Json;
using SLELibrary;
using SolverApp;

namespace ServerApp
{
	public class Program
	{
		const string IPADDRESS = "127.0.0.1";
		const int PORT = 8080;

		public static void Main()
		{
			Server server = new Server(IPADDRESS, PORT);
			server.Start();

			for (int i = 0; i < 30; i++)
			{
				Solver solver = new Solver(IPADDRESS, PORT);
				ThreadPool.QueueUserWorkItem(_ => solver.Start());
				Thread.Sleep(100);
			}

/*			Solver solver = new Solver(IPADDRESS, PORT);
			ThreadPool.QueueUserWorkItem(_ => solver.Start());*/

			Console.Read();
			server.Stop();
		}
	}
}