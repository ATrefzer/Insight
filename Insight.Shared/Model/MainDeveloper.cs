namespace Insight.Shared.Model
{
    public class MainDeveloper
    {
        public MainDeveloper(string mainDeveloper, double percent)
        {
            Developer = mainDeveloper;
            Percent = percent;
        }

        public string Developer { get; }
        public double Percent { get; }
    }
}