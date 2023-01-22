using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Restoring
{
    public class RestoreProgressEventArgs : EventArgs
    {
        public RestoreProgressType ProgressType { get; set; }
        public string FileName { get; set; }
        public RestoreError Error { get; set; }

        public int Downloaded { get; set; }
        public int TotalToDownload { get; set; }

        public Exception Exception { get; set; }

        public static RestoreProgressEventArgs CreateError(RestoreError error, string fileName, Exception exception = null) => new RestoreProgressEventArgs()
        {
            ProgressType = RestoreProgressType.ErrorOccurred,
            FileName = fileName,
            Error = error,
            Exception = exception,
        };

        public static RestoreProgressEventArgs CreateComplete() => new RestoreProgressEventArgs()
        {
            ProgressType = RestoreProgressType.AllCompleted
        };

        public static RestoreProgressEventArgs CreateUpdate(RestoreProgressType type, string fileName) => new RestoreProgressEventArgs()
        {
            ProgressType = type,
            FileName = fileName
        };

        public static RestoreProgressEventArgs CreateDownload(string fileName, int downloaded, int total) => new RestoreProgressEventArgs()
        {
            ProgressType = RestoreProgressType.Download,
            Downloaded = downloaded,
            TotalToDownload = total,
            FileName = fileName
        };
    }
}