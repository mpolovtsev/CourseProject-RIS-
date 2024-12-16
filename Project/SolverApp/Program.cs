namespace SolverApp
{
	class Program
	{
		const string IPADDRESS = "127.0.0.1";
		const int PORT = 8080;

		public static void Main()
		{
			Solver solver = new Solver(IPADDRESS, PORT);
			solver.Start();

			Console.Read();
			solver.Stop();
		}
	}
}