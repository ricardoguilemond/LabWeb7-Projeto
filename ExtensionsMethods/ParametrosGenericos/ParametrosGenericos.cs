namespace ExtensionsMethods.ParametrosGenericos
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ParametrosGenericos()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            //DomainID = Utils.FindDomain();
            //UnityID = Utils.FindUnity();
            //TokenID = Utils.FindToken();
            //Culture = CultureInfo.CurrentCulture.Name;
            //Utils.PrepareLists(this);
        }
    }
}