using System;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Writers
{
    public class CsvTypedReportWriter : IReportDataWriter
    {
        public const            string           Name = "Csv";
        private static readonly ContentType      CsvContentType;
        private static readonly CsvConfiguration CsvConfiguration;


        static CsvTypedReportWriter()
        {
            var csvContentType = new ContentType("text/csv") {CharSet = "utf-8"};
            CsvContentType = csvContentType;

            // Format DateTime as ISO standard formatting with offset
            var dateFormatOptions = new TypeConverterOptions {Formats = new[] {"o"}};

            CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture);
            CsvConfiguration.TypeConverterOptionsCache.AddOptions<DateTime>(dateFormatOptions);
        }

        public string      WriterName  => Name;
        public ContentType ContentType => CsvContentType;

        /// <summary>
        /// Writes the csv data to the given steam and leaves the stream open.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="streamToWriteTo"></param>
        public async Task Write(IAsyncQueryResult data, Stream streamToWriteTo)
        {
            //We want to Dispose and close the newly created StreamWriter, but leave the underlying stream open that is for the caller to handle
             var streamWriter = new StreamWriter(streamToWriteTo, Encoding.UTF8, -1, leaveOpen: true);
             await using (streamWriter.ConfigureAwait(false))
             {

                 var columns = data.Columns;

                 var writer = new CsvWriter(streamWriter, CsvConfiguration, false);
                 await using (writer.ConfigureAwait(false))
                 {
                     var columnCount = columns.Count;
                     for (var i = 0; i < columnCount; i++)
                     {
                         var column = columns[i];

                         writer.WriteField($"{column.Name}@{column.Type}");
                     }

                     await writer.NextRecordAsync();

                     await writer.FlushAsync();
                     await WriteRowsAsync(data, columnCount, writer);
                     await writer.FlushAsync();
                 }
             }
        }

        private static async Task WriteRowsAsync(IAsyncQueryResult data,
                                                 int               columnCount,
                                                 CsvWriter         writer)
        {
            await foreach (var row in data.Rows)
            {
                WriteRowFields(row, columnCount, writer);
                await writer.NextRecordAsync();
            }
        }

        private static void WriteRowFields(in IDataRow row, int columnCount, CsvWriter writer)
        {
            var values = row.GetValues();
            for (var i = 0; i < columnCount; i++)
            {
                writer.WriteField(values[i]);
            }
        }
    }
}