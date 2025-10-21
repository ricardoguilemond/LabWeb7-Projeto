using System;

namespace Extensions
{
    public abstract class ParametrosGenericos : IParametrosGenericos
    {
        public virtual long? DomainID { get; set; }
        public virtual long? UnityID { get; set; }
        public virtual Guid? TokenID { get; set; }
        public virtual string Culture { get; set; }
        public virtual string Filter { get; set; }
        public virtual string Sort { get; set; }
        public virtual int Skip { get; set; }
        public virtual int Take { get; set; }

        public ParametrosGenericos()
        {
            //DomainID = Utils.FindDomain();
            //UnityID = Utils.FindUnity();
            //TokenID = Utils.FindToken();
            //Culture = CultureInfo.CurrentCulture.Name;
            //Utils.PrepareLists(this);
        }

    }
}
