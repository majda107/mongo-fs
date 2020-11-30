using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoFS.Models
{
    public class DriveModel
    {
        [BsonId] public ObjectId Id { get; set; }

        public string Name { get; set; }

        public int Capacity { get; set; }
    }
}