namespace Ruzzie.SimpleReports.Types
{
    public enum ParameterFieldType
    {
        /// <summary>None</summary>
        None,

        ///<summary>Unsigned 8 bits integer</summary>
        U8,

        ///<summary>Unsigned 16 bits integer</summary>
        U16,

        ///<summary>Unsigned 32 bits integer</summary>
        U32,

        ///<summary>Unsigned 64 bits integer</summary>
        U64,

        ///<summary>Signed 64 bits integer</summary>
        I64,

        ///<summary>String</summary>
        S,
        // ReSharper disable once InconsistentNaming
        ///<summary>DateTime</summary>
        DT
    }
}