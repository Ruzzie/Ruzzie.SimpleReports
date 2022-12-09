using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ruzzie.SimpleReports.Run;

namespace Ruzzie.SimpleReports.Db;

public static class Config
{
    public static readonly ReadOnlyDictionary<Type, ColumnDataType> DefaultColumnTypeMapping =
        new(
            new Dictionary<Type, ColumnDataType>(17)
            {
                { typeof(bool), ColumnDataType.b }
              , { typeof(sbyte), ColumnDataType.i }
              , { typeof(short), ColumnDataType.i }
              , { typeof(int), ColumnDataType.i }
              , { typeof(long), ColumnDataType.i }
              , { typeof(byte), ColumnDataType.u }
              , { typeof(ushort), ColumnDataType.u }
              , { typeof(uint), ColumnDataType.u }
              , { typeof(ulong), ColumnDataType.u }
              , { typeof(float), ColumnDataType.f }
              , { typeof(double), ColumnDataType.f }
              , { typeof(decimal), ColumnDataType.f }
              , { typeof(string), ColumnDataType.s }
              , // nullable .. string??

                //{typeof(MySqlDateTime), ColumnDataType.tdt_s},
                { typeof(DateTimeOffset), ColumnDataType.tdt_s }
              , { typeof(DateTime), ColumnDataType.tdt_s },
            });
}