using System;

namespace Extensions
{
    public interface IParametrosGenericos
    {
        long? DomainID { get; set; }

        Guid? TokenID { get; set; }

        long? UnityID { get; set; }

        string Culture { get; set; }

    }
}
