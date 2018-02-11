namespace Visualization.Controls.Data
{
    public class EdgeData
    {
        public string Node1;
        public string Node2;
        public double Strength; // [0...1]

        public EdgeData(string node1, string node2, double strength)
        {
            Node1 = node1;
            Node2 = node2;
            Strength = strength;
        }
    }
}