using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;

namespace OpenBlam.Core.FileSystem
{
    public sealed class FileWatcher : IDisposable
    {
        private string originalPath;
        private FileSystemWatcher watcher;
        private List<FileWatcherCallback> callbacks;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public FileWatcher(string path)
        {
            this.callbacks = new List<FileWatcherCallback>();
            this.originalPath = path;
            this.watcher = new FileSystemWatcher(Path.GetDirectoryName(path))
            {
                Filter = Path.GetFileName(path)
            };

            //watcher.Changed += this.W_Changed;
            this.watcher.NotifyFilter = NotifyFilters.LastWrite;
            Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => this.watcher.Changed += h, h => this.watcher.Changed -= h)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(a => this.W_Changed(a.Sender, a.EventArgs));

            this.watcher.EnableRaisingEvents = true;

        }

        public IDisposable AddListener(Action<string> action)
        {
            return new FileWatcherCallback(action, this.callbacks);
        }

        public void Dispose()
        {
            this.watcher?.Dispose();

            foreach (var cb in this.callbacks)
                cb.Dispose();
        }

        private void W_Changed(object sender, FileSystemEventArgs e)
        {
            foreach (var cb in this.callbacks)
                cb.Invoke(this.originalPath);
        }

        private class FileWatcherCallback : IDisposable
        {
            private readonly Action<string> action;
            private readonly List<FileWatcherCallback> cbs;

            public FileWatcherCallback(Action<string> action, List<FileWatcherCallback> cbs)
            {
                this.action = action;
                this.cbs = cbs;

                cbs.Add(this);
            }

            public void Invoke(string path)
            {
                this.action(path);
            }

            public void Dispose()
            {
                try
                {
                    this.cbs.Remove(this);
                }
                catch (Exception) { }
            }
        }
    }
}
