using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Writers
{
    public interface IReportDataWriter
    {
        /// gets the human-readable description of the current <see cref="IReportDataWriter"/>.
        string WriterName { get; }

        /// gets the  <see cref="ContentType"/> of the output format of the current <see cref="IReportDataWriter"/>.
        ContentType ContentType { get; }

        Task Write(IAsyncQueryResult data, Stream streamToWriteTo);
    }
}