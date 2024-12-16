using Newtonsoft.Json;
using System.Diagnostics;
using ServerApp;
using SLELibrary;
using SolverApp;
using System.Net;

namespace LoadTesting
{
	public class LoadTest
	{
		const string IPADDRESS = "127.0.0.1";
		const int PORT = 8080;

		public static void Main()
		{
            Console.ForegroundColor = ConsoleColor.Black;
			Console.BackgroundColor = ConsoleColor.White;
			Console.Clear();

/*			List<int> numbersOfClients = new List<int>() { 5, 10, 15, 20, 25 };
			TestDifferentNumbersOfClients(numbersOfClients);*/

			List<string> files = new List<string>() { "D:\\Course Project (DIS)\\sle1.json", "D:\\Course Project (DIS)\\sle2.json",
				"D:\\Course Project (DIS)\\sle3.json", "D:\\Course Project (DIS)\\sle4.json" };
			TestDifferentSLE(files);
		}

		public static void TestDifferentNumbersOfClients(List<int> numbersOfClients)
		{
			foreach (int number in numbersOfClients) 
			{
				Server server = new Server(IPADDRESS, PORT);
				server.Start();

				List<Solver> solvers = new List<Solver>();

				for (int i = 0; i < number; i++)
				{
					Solver solver = new Solver(IPADDRESS, PORT);
					solvers.Add(solver);
					ThreadPool.QueueUserWorkItem(_ => solver.Start());
					Thread.Sleep(100);
				}

				string json = File.ReadAllText("D:\\sle_small.json");
				SLE sle = JsonConvert.DeserializeObject<SLE>(json)!;

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

                sle.SeidelMethodParallel(0.0001, 10000, server.Solvers);

				stopwatch.Stop();

				long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				string logMessage = number.ToString() + "\t" + elapsedMilliseconds.ToString();
				File.AppendAllText("D:\\Course Project (DIS)\\testing different numbers of clients.txt", logMessage + Environment.NewLine);

				server.Stop();
			}
		}

		public static void TestDifferentSLE(List<string> files)
		{
			foreach (string file in files)
			{
				Server server = new Server(IPADDRESS, PORT);
				server.Start();

				List<Solver> solvers = new List<Solver>();

				for (int i = 0; i < 5; i++)
				{
					Solver solver = new Solver(IPADDRESS, PORT);
					solvers.Add(solver);
					ThreadPool.QueueUserWorkItem(_ => solver.Start());
					Thread.Sleep(100);
				}

				string json = File.ReadAllText(file);
				SLE sle = JsonConvert.DeserializeObject<SLE>(json)!;

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				sle.SeidelMethodParallel(0.0001, 10000, server.Solvers);

				stopwatch.Stop();

				long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				string logMessage = sle.A.Length + "\t" + elapsedMilliseconds.ToString();
				File.AppendAllText("D:\\Course Project (DIS)\\testing different sle.txt", logMessage + Environment.NewLine);

				server.Stop();
			}
		}
	}
}