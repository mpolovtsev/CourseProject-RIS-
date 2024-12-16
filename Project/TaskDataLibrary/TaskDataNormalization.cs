namespace TaskDataLibrary
{
	public class TaskDataNormalization
	{
		public double[][] A { get; private set; }
		public double[] B { get; private set; }
		public int Index { get; set; }
		public int StartIndex { get; private set; }
		public int EndIndex { get; private set; }

		public TaskDataNormalization(double[][] a, double[] b, int startIndex, int endIndex)
		{
			A = a;
			B = b;
			Index = startIndex;
			StartIndex = startIndex;
			EndIndex = endIndex;
		}
	}
}