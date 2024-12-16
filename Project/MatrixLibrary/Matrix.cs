namespace MatrixLibrary
{
	public static class Matrix
	{
		public static double[][] Transpose(double[][] matrix)
		{
			double[][] result = new double[matrix[0].Length][];

			for (int i = 0; i < result.Length; i++)
				result[i] = new double[matrix.Length];

			for (int i = 0; i < matrix.Length; i++)
				for (int j = 0; j < matrix[0].Length; j++)
					result[j][i] = matrix[i][j];

			return result;
		}

		public static double[][] Multiply(double[][] a, double[][] b)
		{
			double[][] result = new double[a.Length][];

			for (int i = 0; i < a.Length; i++)
			{
				result[i] = new double[b[0].Length];

				for (int j = 0; j < b[0].Length; j++)
				{
					result[i][j] = 0;

					for (int k = 0; k < a[0].Length; k++)
						result[i][j] += a[i][k] * b[k][j];
				}
			}

			return result;
		}

		public static double[] Multiply(double[][] a, double[] b)
		{
			double[] result = new double[a.Length];

			for (int i = 0; i < a.Length; i++)
			{
				result[i] = 0;

				for (int j = 0; j < a[0].Length; j++)
					result[i] += a[i][j] * b[j];
			}

			return result;
		}
	}
}