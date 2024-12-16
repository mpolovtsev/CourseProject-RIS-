using Newtonsoft.Json;
using SLELibrary;

namespace UnitTesting
{
	[TestClass]
	public class UnitTest
	{
		[TestMethod]
		public void TestSeidelMethod()
		{
			string json = File.ReadAllText("D:\\Course Project (DIS)\\App\\Project\\UnitTesting\\test_sle.json");
			SLE sle = JsonConvert.DeserializeObject<SLE>(json)!;
			sle.SeidelMethod(0.0001, 10000);
			double[] solutionSeidelMethod = sle.X!;
			sle.GaussianMethod();
			double[] solutionGaussianMethod = sle.X!;

			for (int i = 0; i < solutionSeidelMethod.Length; i++)
				Assert.IsTrue(Math.Abs(solutionSeidelMethod[i] - solutionGaussianMethod[i]) < 0.1);
		}
	}
}