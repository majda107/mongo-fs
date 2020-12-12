using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoFS.Models
{
    public abstract class IFileModel
    {
        [BsonId] public ObjectId Id { get; set; }
        public ObjectId DriveId { get; set; }

        public string Name { get; set; }

        public DateTime Created = DateTime.Now;
    }
}