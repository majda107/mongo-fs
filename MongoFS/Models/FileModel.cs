using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoFS.Models
{
    public class FileModel
    {
        [BsonId] public ObjectId Id { get; set; }


        public ObjectId DriveId { get; set; }

        public string Name { get; set; }


        public ObjectId FolderId { get; set; }
    }
}