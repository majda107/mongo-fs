using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoFS.Models
{
    public class FolderModel
    {
        [BsonId] public ObjectId Id { get; set; }


        public ObjectId DriveId { get; set; }

        public string Name { get; set; }

        public int Size { get; set; }


        public ObjectId ParentId { get; set; }

        // public Dictionary<string, ObjectId> Children { get; set; }
        public List<ObjectId> Folders { get; set; } = new List<ObjectId>();
        public List<ObjectId> Files { get; set; } = new List<ObjectId>();
    }
}