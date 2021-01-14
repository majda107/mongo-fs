using System;
using Microsoft.AspNetCore.Components;

namespace MongoFS.Services
{
    public class StateService
    {
        public event EventHandler<int> Deleted;

        public void FireDeleted(int size) => this.Deleted?.Invoke(this, size);
    }
}