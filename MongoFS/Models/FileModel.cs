using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoFS.Models
{
    public class FileModel : IFileModel
    {
        // [BsonId] public ObjectId Id { get; set; }

        // public ObjectId DriveId { get; set; }

        // public string Name { get; set; }

        public string Type { get; set; } = "";
        public string Content { get; set; } = "";


        public ObjectId FolderId { get; set; }

        public DateTime LastEdit = DateTime.Now;
    }
}