using System;
using Microsoft.AspNetCore.Components;

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

        public void FireDeleted(int size) => this.Deleted?.Invoke(this, size);
    }
}