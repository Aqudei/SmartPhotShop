using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartPhotShop.Models
{
    class Processor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private ConcurrentQueue<string> _queue = new ConcurrentQueue<string>(); 

        private readonly string _workDirectory;
        private readonly string _errorDirectory;
        private readonly string _successDirectory;
        private readonly string _outputDirectory;
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        public Processor(string workingDirectory)
        {
            _workDirectory = workingDirectory;

            _errorDirectory = System.IO.Path.Combine(_workDirectory, "Error");
            _successDirectory = System.IO.Path.Combine(_workDirectory, "Success");
            _outputDirectory = System.IO.Path.Combine(_workDirectory, "Output");

            EnsureDirectory(_errorDirectory);
            EnsureDirectory(_successDirectory);
            EnsureDirectory(_outputDirectory);

            _worker.DoWork += _worker_DoWork;
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var psApp = new Photoshop.Application();


        }

        private void EnsureDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error creating directory {0}", directory);
            }
        }

        public void Start()
        {
            var fsWatcher = new FileSystemWatcher(_workDirectory);
            fsWatcher.Created += (s, e) =>
            {
                _queue.Enqueue(e.FullPath);
            };
            fsWatcher.EnableRaisingEvents = true;



        }

        private void Watcher()
        {

        }
    }
}
