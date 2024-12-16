using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using MatrixLibrary;
using SolverApp;
using TaskDataLibrary;

namespace SLELibrary
{
	public class SLE
	{
		[JsonProperty("system_matrix")]
		public double[][] A { get; private set; }
		[JsonProperty("free_members_vector")]
		public double[] B { get; private set; }
		[JsonIgnore]
		public double[]? X { get; private set; }
		[JsonProperty("roots")]
		public double[]? Result
		{
			get
			{
				if (X == null)
					return null;

				double[] result = new double[X.Length];

				for (int i = 0; i < X.Length; i++)
					result[i] = Math.Round(X[i], 2);
				
				return result;
			}
		}

		public SLE(double[][] a, double[] b)
		{
			A = a;
			B = b;
			X = new double[A[0].Length];
		}

		// Метод Зейделя
		public void SeidelMethod(double e, int n)
		{
			// Симметризация Гаусса
			double[][] a = Matrix.Multiply(Matrix.Transpose(A), A);
			double[] b = Matrix.Multiply(Matrix.Transpose(A), B);

			double divider;

			// Приведение к виду, удобному для итерации
			for (int i = 0; i < a.Length; i++)
			{
				divider = a[i][i];
				a[i][i] = 0;

				for (int j = 0; j < A[0].Length; j++)
					if (i != j)
						a[i][j] /= -divider;

				b[i] /= divider;
			}

			// Итеративное вычисление корней
			X = new double[a[0].Length];
			double[] prevX = new double[X.Length];
			int step = 0;
			double sum;
			bool flag;

			while (step < n)
			{
				step += 1;

				Array.Copy(X!, prevX, X!.Length);

				for (int i = 0; i < a.Length; i++)
				{
					sum = 0;

					for (int j = 0; j < i; j++)
						sum += a[i][j] * X[j];

					for (int j = i; j < a[0].Length; j++)
						sum += a[i][j] * prevX[j];

					X[i] = b[i] + sum;
				}

				flag = true;

				for (int i = 0; i < X.Length; i++)
					if (Math.Abs(X[i] - prevX[i]) > e)
						flag = false;

				if (flag)
					break;

				if (step == n)
					X = null;
			}
		}

		// Распараллеленный метод Зейделя
		public void SeidelMethodParallel(double e, int n, List<Solver> solvers)
		{
			// Симметризация Гаусса
			double[][] a = Matrix.Multiply(Matrix.Transpose(A), A);
			double[] b = Matrix.Multiply(Matrix.Transpose(A), B);

			// Приведение к виду, удобному для итерации
			int rowsPerSolver;
			Task[] tasks;
			double[][] aPart;
			double[] bPart;
			int leftBorder;
			int rightBorder;
			TaskDataNormalization taskDataNormalization;
			Mutex mutex = new Mutex();
			List<Solver> freeSolvers = solvers.Where(s => !s.IsBusy).ToList();

            if (freeSolvers.Count > a.Length)
			{
				rowsPerSolver = 1;
				tasks = new Task[a.Length];
			}
			else
			{
				rowsPerSolver = a.Length / freeSolvers.Count;
				tasks = new Task[freeSolvers.Count];
			}

			freeSolvers = freeSolvers[0..tasks.Length];

			for (int i = 0; i < tasks.Length; i++)
			{
				(aPart, bPart) = GetPartSLE(a, b, i, rowsPerSolver, tasks.Length);
				leftBorder = i * rowsPerSolver;
				rightBorder = (i == tasks.Length - 1) ? a.Length : leftBorder + rowsPerSolver;
				taskDataNormalization = new TaskDataNormalization(aPart, bPart, leftBorder, rightBorder);
				freeSolvers[i].IsBusy = true;
				byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataNormalization));
				byte[] message = Encoding.UTF8.GetBytes($"{data.Length, -20}").Concat(data).ToArray();
				Socket solver = freeSolvers[i].Socket;

				tasks[i] = Task.Run(() =>
				{
					solver.Send(message);
					TaskDataNormalization? answer = Read<TaskDataNormalization>(solver);

					mutex.WaitOne();

					for (int i = answer.StartIndex, j = 0; i < answer.EndIndex; i++, j++)
					{
						a[i] = answer.A[j];
						b[i] = answer.B[j];
					}

					mutex.ReleaseMutex();
				});
			}

			Task.WaitAll(tasks);

			// Итеративное вычисление корней
			X = new double[a[0].Length];
			double[] prevX = new double[a[0].Length];
			int elemsPerSolver;
			int step = 0;
			double sum;
			bool flag;
			double[] rowPart = [];
			double[] xPart;
			TaskDataCalculation taskDataCalculation;

			while (step < n)
			{
				Array.Copy(X!, prevX, X!.Length);
                Console.WriteLine(step);
                for (int i = 0; i < a.Length; i++)
				{
					sum = 0;

					if (freeSolvers.Count > i)
					{
						elemsPerSolver = 1;
						tasks = new Task[i];
					}
					else
					{
						elemsPerSolver = i / freeSolvers.Count;
						tasks = new Task[freeSolvers.Count];
					}

					for (int j = 0; j < tasks.Length; j++)
					{
						rowPart = GetPartRow(a[i][0..i], j, elemsPerSolver, tasks.Length);
						xPart = GetPartRow(X[0..i], j, elemsPerSolver, tasks.Length);

						if (rowPart.Length == 0)
							continue;

						taskDataCalculation = new TaskDataCalculation(rowPart, xPart);
						freeSolvers[j].IsBusy = true;
						byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataCalculation));
						byte[] message = Encoding.UTF8.GetBytes($"{data.Length, -20}").Concat(data).ToArray();
						Socket solver = freeSolvers[j].Socket;
						solver.Send(message);

						tasks[j] = Task.Run(() =>
						{
							TaskDataCalculation? answer = Read<TaskDataCalculation>(solver);

							mutex.WaitOne();
							sum += answer.Sum;
							mutex.ReleaseMutex();
						});
					}

					if (rowPart.Length != 0)
						Task.WaitAll(tasks);

					if (freeSolvers.Count > a.Length - i)
					{
						elemsPerSolver = 1;
						tasks = new Task[a.Length - i];
					}
					else
					{
						elemsPerSolver = (a.Length - i) / freeSolvers.Count;
						tasks = new Task[freeSolvers.Count];
					}

					for (int j = 0; j < tasks.Length; j++)
					{
						rowPart = GetPartRow(a[i][i..a.Length], j, elemsPerSolver, tasks.Length);
						xPart = GetPartRow(prevX[i..a.Length], j, elemsPerSolver, tasks.Length);

						if (rowPart.Length == 0)
							continue;

						taskDataCalculation = new TaskDataCalculation(rowPart, xPart);
						freeSolvers[j].IsBusy = true;
						byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskDataCalculation));
						byte[] message = Encoding.UTF8.GetBytes($"{data.Length, -20}").Concat(data).ToArray();
						Socket solver = freeSolvers[j].Socket;
						solver.Send(message);

						tasks[j] = Task.Run(() =>
						{
							TaskDataCalculation? answer = Read<TaskDataCalculation>(solver);

							mutex.WaitOne();
							sum += answer.Sum;
							mutex.ReleaseMutex();
						});
					}

					if (rowPart.Length != 0)
						Task.WaitAll(tasks);

					X[i] = b[i] + sum;
				}

				flag = true;

				for (int i = 0; i < X.Length; i++)
					if (Math.Abs(X[i] - prevX[i]) > e)
						flag = false;

				if (flag)
				{
					mutex.WaitOne();
					for (int i = 0; i < freeSolvers.Count; i++)
					{
						freeSolvers[i].Socket.Send(Encoding.UTF8.GetBytes("End of data transfer"));
						freeSolvers[i].IsBusy = false;
					}
					mutex.ReleaseMutex();

					break;
				}

				step += 1;

				if (step == n)
					X = null;
			}
		}

		// Метод Гаусса
		public void GaussianMethod()
		{
			double[][] a = new double[A.Length][];
			double[] b = new double[A.Length];
			X = new double[A[0].Length];
			Array.Copy(A, a, A.Length);
			Array.Copy(B, b, A.Length);
			double factor;

			// Приведение расширенной матрицы к треугольному виду (прямой ход)
			for (int i = 0; i < A[0].Length - 1; i++)
			{
				for (int j = i + 1; j < A.Length; j++)
				{
					factor = a[j][i] / a[i][i];

					for (int k = 0; k < A[0].Length; k++)
						a[j][k] -= a[i][k] * factor;

					b[j] -= b[i] * factor;
				}
			}

			double sum;

			// Вычисление корней (обратный ход)
			for (int i = A.Length - 1; i >= 0; i--)
			{
				sum = 0;

				for (int j = i + 1; j < A[0].Length; j++)
					sum += a[i][j] * X[j];

				X[i] = (b[i] - sum) / a[i][i];
			}
		}

		(double[][], double[]) GetPartSLE(double[][] a, double[] b, int number, int size, int length)
		{
			int leftBorder = number * size;
			int rightBorder = (number == length - 1) ? a.Length : leftBorder + size;
			double[][] aPart = a[leftBorder..rightBorder];
			double[] bPart = b[leftBorder..rightBorder];

			return (aPart, bPart);
		}

		double[] GetPartRow(double[] row, int number, int size, int length)
		{
			if (row.Length <= 1)
				return row;

			int leftBorder = number * size;
			int rightBorder = (number == length - 1) ? row.Length : leftBorder + size;
			double[] rowPart = row[leftBorder..rightBorder];

			return rowPart;
		}

		T? Read<T>(Socket socket)
		{
			byte[] lengthBuffer = new byte[20];
			int totalRead = 0;
			int bytesRead;

			try
			{
				while (totalRead < lengthBuffer.Length)
				{
					bytesRead = socket.Receive(lengthBuffer, totalRead, lengthBuffer.Length - totalRead, SocketFlags.None);
					totalRead += bytesRead;
				}
			}
			catch (SocketException)
			{
				return default;
			}

			int messageLength = int.Parse(Encoding.UTF8.GetString(lengthBuffer));
			byte[] messageBuffer = new byte[messageLength];
			totalRead = 0;
			StringBuilder requestBuilder = new StringBuilder();

			try
			{
				while (totalRead < messageLength)
				{
					bytesRead = socket.Receive(messageBuffer, totalRead, messageLength - totalRead, SocketFlags.None);
					requestBuilder.Append(Encoding.UTF8.GetString(messageBuffer));
					totalRead += bytesRead;
				}
			}
			catch (SocketException)
			{
				return default;
			}


			T? answer = JsonConvert.DeserializeObject<T>(requestBuilder.ToString(),
				new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
			
			return answer;
		}
	}
}