using Newtonsoft.Json;
using MatrixLibrary;
using SLELibrary;

namespace GenerateSLEApp
{
	public class Program
	{
		public static void Main()
		{
			SLE sle = GenerateSLE(75);
			string json = JsonConvert.SerializeObject(new 
				{
					system_matrix = sle.A,
					free_members_vector = sle.B
				}, new JsonSerializerSettings
				{
					Formatting = Formatting.Indented
				});
			string filePath = "D:\\Course Project (DIS)\\sle4.json";

			File.WriteAllText(filePath, json);
		}

		public static SLE GenerateSLE(int n)
		{
			Random random = new Random();
			double[][] a = new double[n][];
			double[] b;
			double[] x = new double[n];

			for (int i = 0; i < n; i++)
				a[i] = new double[n];

			for (int i = 0; i < n; i++)
				for (int j = 0; j < n; j++)
					a[i][j] = Math.Round((2 * random.NextDouble() - 1) * 10, 2);

			for (int i = 0; i < n; i++)
				x[i] = Math.Round((2 * random.NextDouble() - 1) * 10, 2);

			b = Matrix.Multiply(a, x);

			/*Console.WriteLine("a:");
			foreach (double[] row in a)
			{
				Console.WriteLine();

				foreach (double elem in row)
					Console.Write(elem + " ");
			}

			Console.WriteLine();
			Console.WriteLine("b:");
			foreach (double elem in b)
				Console.Write(elem + " ");

			Console.WriteLine();
			Console.WriteLine("x:");
			foreach (double elem in x)
				Console.Write(elem + " ");*/

			return new SLE(a, b);
		}
	}
}