using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoFS.Models;

namespace MongoFS.Services
{
    public class MongoService
    {
        private const string DATABASE = "mongofs";
        private const string DRIVE = "drive";
        private const string FOLDERS = "folders";
        private const string FILES = "files";

        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;

        public MongoService()
        {
            this._client = new MongoClient();
            this._database = this._client.GetDatabase(DATABASE);
        }

        public IQueryable<DriveModel> GetDrives()
        {
            var res = this._database.GetCollection<DriveModel>(DRIVE);
            return res.AsQueryable();
        }

        public void CreateDrive(DriveModel drive)
        {
            this._database.GetCollection<DriveModel>(DRIVE).InsertOne(drive);
        }

        public void RemoveDrive(ObjectId id)
        {
            this._database.GetCollection<DriveModel>(DRIVE).DeleteOne(f => f.Id == id);
        }


        public async Task<DriveModel> GetDrive(ObjectId drive)
        {
            return (await this._database.GetCollection<DriveModel>(DRIVE).FindAsync(f => f.Id == drive))
                .FirstOrDefault();
        }

        public async Task<IList<FolderModel>> GetFolders(ObjectId drive, ObjectId parent)
        {
            var res = await this._database.GetCollection<FolderModel>(FOLDERS)
                .FindAsync(f => f.DriveId == drive && f.ParentId == parent);

            // var res = await this._database.GetCollection<FolderModel>(FOLDERS)
            // .FindAsync(f => f.Id == drive);

            return res.ToList();
        }


        public async Task<IList<FileModel>> GetFolderFiles(ObjectId drive, ObjectId folder)
        {
            var res = await this._database.GetCollection<FileModel>(FILES)
                .FindAsync(f => f.DriveId == drive && f.FolderId == folder);

            return res.ToList();
        }

        public async Task<FolderModel> GetDriveFolder(ObjectId drive, ObjectId folder)
        {
            return (await this._database.GetCollection<FolderModel>(FOLDERS)
                .FindAsync(f => f.DriveId == drive && f.Id == folder)).FirstOrDefault();
        }


        public async Task<FileModel> GetFile(ObjectId fileId)
        {
            var res = (await this._database.GetCollection<FileModel>(FILES).FindAsync(f => f.Id == fileId))
                .FirstOrDefault();

            return res;
        }

        public async Task UpdateFile(FileModel file)
        {
            await this._database.GetCollection<FileModel>(FILES).ReplaceOneAsync(f => f.Id == file.Id, file);
        }


        // TODO register to parent folder (children)
        public async Task CreateFolder(ObjectId drive, ObjectId parent, string name)
        {
            await this._database.GetCollection<FolderModel>(FOLDERS).InsertOneAsync(new FolderModel()
            {
                Name = name,
                ParentId = parent,
                DriveId = drive
            });
        }


        public async Task CreateFile(ObjectId drive, ObjectId folder, string name)
        {
            await this._database.GetCollection<FileModel>(FILES).InsertOneAsync(new FileModel()
            {
                DriveId = drive,
                FolderId = folder,
                Name = name
            });
        }


        public async Task CreateFolderPath(ObjectId driveId, string path)
        {
            // REMOVE STARTING \ 
            if (path.StartsWith(Path.PathSeparator)) path = path.Substring(1, path.Length - 1);


            var split = path.Split(Path.PathSeparator);
            if (split.Length <= 0) return;

            var folders = this._database.GetCollection<FolderModel>(FOLDERS);
            // var rootPath = split.First();
            //
            //
            // var root = folders.Find(f => f.ParentId == ObjectId.Empty && f.Name == rootPath && f.DriveId == driveId)
            //     .FirstOrDefault();
            // if (root == null)
            // {
            //     root = new FolderModel {DriveId = driveId, Name = rootPath, ParentId = ObjectId.Empty};
            //     await folders.InsertOneAsync(root);
            // }


            FolderModel parent = null, entry = null;
            foreach (var folder in split)
            {
                if (parent == null || parent.Children.ContainsKey(folder))
                {
                    entry = (await folders.FindAsync(f =>
                        f.ParentId == (parent == null ? ObjectId.Empty : parent.Id) && f.DriveId == driveId &&
                        f.Name == folder)).FirstOrDefault();
                }
                else
                {
                    entry = new FolderModel
                        {ParentId = parent?.Id ?? ObjectId.Empty, Name = folder, DriveId = driveId};
                    await folders.InsertOneAsync(entry);
                }

                parent = entry;
            }
        }
    }
}