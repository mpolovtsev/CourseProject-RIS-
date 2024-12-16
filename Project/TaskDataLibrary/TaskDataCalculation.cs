namespace TaskDataLibrary
{
	public class TaskDataCalculation
	{
		public double[] Row { get; private set; }
		public double[] X { get; private set; }
		public double Sum { get; set; }

		public TaskDataCalculation(double[] row, double[] x)
		{
			Row = row;
			X = x;
		}
	}
}