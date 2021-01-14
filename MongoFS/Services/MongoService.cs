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


        public async Task<DriveModel> GetDrive(ObjectId drive)
        {
            return (await this._database.GetCollection<DriveModel>(DRIVE).FindAsync(f => f.Id == drive))
                .FirstOrDefault();
        }

        // DELETION
        public async Task DeleteDrive(ObjectId drive)
        {
            await this._database.GetCollection<FileModel>(FILES).DeleteManyAsync(f => f.DriveId == drive);
            await this._database.GetCollection<FolderModel>(FOLDERS).DeleteManyAsync(f => f.DriveId == drive);
            await this._database.GetCollection<DriveModel>(DRIVE).DeleteOneAsync(d => d.Id == drive);
        }

        // DELETE FILE
        public async Task DeleteFile(ObjectId fileId)
        {
            var file = this._database.GetCollection<FileModel>(FILES).AsQueryable()
                .FirstOrDefault(f => f.Id == fileId);

            if (file == null) throw new Exception();

            await this._database.GetCollection<FileModel>(FILES).DeleteOneAsync(f => f.Id == fileId);
            await this.UpdateFolderSize(file.FolderId, file.DriveId, -file.Content?.Length ?? 0);


            await this._database.GetCollection<FolderModel>(FOLDERS).UpdateOneAsync(f => f.Id == file.FolderId,
                Builders<FolderModel>.Update.Pull(u => u.Files, fileId));
        }

        //REVIEW update disk size
        public async Task DeleteFolder(ObjectId folderId)
        {
            var folder = this._database.GetCollection<FolderModel>(FOLDERS).AsQueryable()
                .FirstOrDefault(f => f.Id == folderId);

            if (folder == null) return;

            // await Task.WhenAll(folder.Folders.Select(async f => await this.cascadeFolderDelete(f)));
            // await Task.WhenAll(folder.Files.Select(async f => await this.cascadeFolderDelete(f)));
            //
            //
            // await this._database.GetCollection<FolderModel>(FOLDERS)
            //     .DeleteManyAsync(f => folder.Folders.Contains(f.Id));
            // await this._database.GetCollection<FileModel>(FILES).DeleteManyAsync(f => folder.Files.Contains(f.Id));
            await this.cascadeFolderDelete(folderId);


            await this._database.GetCollection<FolderModel>(FOLDERS).UpdateOneAsync(f => f.Id == folder.ParentId,
                Builders<FolderModel>.Update.Pull(f => f.Folders, folderId));

            await this.UpdateFolderSize(folder.ParentId, folder.DriveId, -folder.Size);
            await this._database.GetCollection<FolderModel>(FOLDERS).DeleteOneAsync(f => f.Id == folderId);
        }

        // DIRTY WAY TO DELETE FOLDER
        private async Task cascadeFolderDelete(ObjectId folderId)
        {
            var folder = this._database.GetCollection<FolderModel>(FOLDERS).AsQueryable()
                .FirstOrDefault(f => f.Id == folderId);

            if (folder == null) return;
            await Task.WhenAll(folder.Folders.Select(async f => await this.cascadeFolderDelete(f)));
            await Task.WhenAll(folder.Files.Select(async f => await this.cascadeFolderDelete(f)));

            await this._database.GetCollection<FolderModel>(FOLDERS)
                .DeleteManyAsync(f => folder.Folders.Contains(f.Id));
            await this._database.GetCollection<FileModel>(FILES).DeleteManyAsync(f => folder.Files.Contains(f.Id));
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
            file.LastEdit = DateTime.Now;

            var current = this._database.GetCollection<FileModel>(FILES).AsQueryable()
                .FirstOrDefault(f => f.Id == file.Id);
            if (current == null) return;

            var inc = file.Content.Length - (current.Content?.Length ?? 0);

            // REPLACE THE FILE
            await this._database.GetCollection<FileModel>(FILES).ReplaceOneAsync(f => f.Id == file.Id, file);

            // UPDATE FOLDER SIZE CASCADE
            await this.UpdateFolderSize(file.FolderId, file.DriveId, inc);
        }


        // REVIEW register to parent folder (children)
        public async Task CreateFolder(ObjectId drive, ObjectId parent, string name)
        {
            var id = ObjectId.GenerateNewId();
            await this._database.GetCollection<FolderModel>(FOLDERS).InsertOneAsync(new FolderModel()
            {
                Id = id,
                Name = name,
                ParentId = parent,
                DriveId = drive
            });

            await this._database.GetCollection<FolderModel>(FOLDERS)
                .UpdateOneAsync(f => f.Id == parent,
                    Builders<FolderModel>.Update.Combine(
                        Builders<FolderModel>.Update.Push(f => f.Folders, id),
                        Builders<FolderModel>.Update.Set(f => f.LastEdit, DateTime.Now)
                    ));
        }


        public async Task CreateFile(ObjectId drive, ObjectId folder, string name, string type = "text")
        {
            var id = ObjectId.GenerateNewId();
            await this._database.GetCollection<FileModel>(FILES).InsertOneAsync(new FileModel()
            {
                Id = id,
                DriveId = drive,
                FolderId = folder,
                Name = name,
                Type = type
            });

            await this._database.GetCollection<FolderModel>(FOLDERS).UpdateOneAsync(f => f.Id == folder,
                Builders<FolderModel>.Update.Push(f => f.Files, id));

            // this._database.GetCollection<FolderModel>(FOLDERS).AsQueryable().Where(f => f.)
        }


        // SIZE UPDATING
        public async Task UpdateFolderSize(ObjectId folder, ObjectId drive, int inc)
        {
            await this._database.GetCollection<FolderModel>(FOLDERS).UpdateOneAsync(fi => fi.Id == folder,
                Builders<FolderModel>.Update.Inc(f => f.Size, inc));

            var f = this._database.GetCollection<FolderModel>(FOLDERS).AsQueryable()
                .FirstOrDefault(f => f.Id == folder);

            if (f != null && f.ParentId != ObjectId.Empty) await this.UpdateFolderSize(f.ParentId, drive, inc);
            else await this.UpdateDriveSize(drive, inc);
        }

        public async Task UpdateDriveSize(ObjectId drive, int inc)
        {
            await this._database.GetCollection<DriveModel>(DRIVE).UpdateOneAsync(d => d.Id == drive,
                Builders<DriveModel>.Update.Inc(d => d.Taken, inc));
        }


        public async Task CreateFolderPath(ObjectId driveId, string path)
        {
            // // REMOVE STARTING \ 
            // if (path.StartsWith(Path.PathSeparator)) path = path.Substring(1, path.Length - 1);
            //
            //
            // var split = path.Split(Path.PathSeparator);
            // if (split.Length <= 0) return;
            //
            // var folders = this._database.GetCollection<FolderModel>(FOLDERS);
            // // var rootPath = split.First();
            // //
            // //
            // // var root = folders.Find(f => f.ParentId == ObjectId.Empty && f.Name == rootPath && f.DriveId == driveId)
            // //     .FirstOrDefault();
            // // if (root == null)
            // // {
            // //     root = new FolderModel {DriveId = driveId, Name = rootPath, ParentId = ObjectId.Empty};
            // //     await folders.InsertOneAsync(root);
            // // }
            //
            //
            // FolderModel parent = null, entry = null;
            // foreach (var folder in split)
            // {
            //     if (parent == null || parent.Children.ContainsKey(folder))
            //     {
            //         entry = (await folders.FindAsync(f =>
            //             f.ParentId == (parent == null ? ObjectId.Empty : parent.Id) && f.DriveId == driveId &&
            //             f.Name == folder)).FirstOrDefault();
            //     }
            //     else
            //     {
            //         entry = new FolderModel
            //             {ParentId = parent?.Id ?? ObjectId.Empty, Name = folder, DriveId = driveId};
            //         await folders.InsertOneAsync(entry);
            //     }
            //
            //     parent = entry;
            // }
        }
    }
}