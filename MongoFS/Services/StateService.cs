using System;
using Microsoft.AspNetCore.Components;
using MongoFS.Models;

namespace MongoFS.Services
{
    public class StateService
    {
        public static readonly string[] FILE_TYPES = new[]
        {
            "csv",
            "html",
            "txt",
            "vimscript",
            "css",
            "js",
            "rb",
            "go"
        };

        public static string ToMonacoType(string type) => type switch
        {
            "css" => "text/css",
            "html" => "text/html",
            _ => "text"
        };


        public event EventHandler<int> Deleted;
        public event EventHandler OnSelected;

        public void FireDeleted(int size) => this.Deleted?.Invoke(this, size);


        private IFileModel selected;

        public IFileModel Selected
        {
            get => this.selected;
            set
            {
                this.selected = value;
                this.OnSelected?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}