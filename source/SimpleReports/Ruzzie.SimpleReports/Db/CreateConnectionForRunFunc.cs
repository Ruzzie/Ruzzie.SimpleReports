using System;
using System.Data.Common;

namespace Ruzzie.SimpleReports.Db;

public delegate DbConnection CreateConnectionForRunFunc(ReadOnlySpan<(string Name, string Value)> runParams);