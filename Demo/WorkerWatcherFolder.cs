using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Demo
{
    public class WorkerWatcherFolder : BackgroundService
    {
        private readonly ILogger<WorkerWatcherFolder> _logger;
        private FileSystemWatcher _folderWatcher;
        private readonly string _inputFolder;
        private readonly IServiceProvider _services;

        public WorkerWatcherFolder(ILogger<WorkerWatcherFolder> _logger, IOptions<AppSettings> settings, IServiceProvider services)
        {
            this._logger = _logger;
            this._inputFolder = settings.Value.InputFolder;
            this._services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Staring");
            if (!Directory.Exists(_inputFolder))
            {
                _logger.LogWarning($"Please make sure the inputFolder [{_inputFolder}] exists, then restart the service.");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Binding Events from Input Folder: {_inputFolder}");
            _folderWatcher = new FileSystemWatcher(_inputFolder, "*.TXT")
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName |
                                NotifyFilters.DirectoryName
            };
            _folderWatcher.Created += Input_OnChanged;
            _folderWatcher.EnableRaisingEvents = true;

            return base.StartAsync(cancellationToken);
        }
        
        protected void Input_OnChanged(object source, FileSystemEventArgs e)
        {

            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                _logger.LogInformation($"Inbound Change Event Triggered By [{e.FullPath}]");

                _logger.LogInformation("Done With Inbound Change Event");

                //_serviceA.Run();
                using (var scope = _services.CreateScope())
                {
                    var serviceA = scope.ServiceProvider.GetRequiredService<IServiceA>();
                    serviceA.Run();
                }

            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {

            _logger.LogInformation("Stopping Service");
            _folderWatcher.EnableRaisingEvents = false;
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing Service");
            _folderWatcher.Dispose();
            base.Dispose();
        }

    }
}
