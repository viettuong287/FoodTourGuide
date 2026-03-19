using SQLite;

namespace FoodTourGuide.Models
{
    public class PoI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Radius { get; set; }

        public int Priority { get; set; }

        public string Description { get; set; }

        public string AudioFile { get; set; }
    }
}