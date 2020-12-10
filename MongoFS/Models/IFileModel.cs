using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoFS.Models
{
    public abstract class IFileModel
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Name { get; set; }

    }
}