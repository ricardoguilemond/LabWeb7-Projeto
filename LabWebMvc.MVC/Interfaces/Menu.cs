namespace LabWebMvc.MVC.Interfaces
{
    public class Menu : IEquatable<Menu>
    {
        public int MenuId { get; set; }
        public string? Principal { get; set; }
        public string? Item { get; set; }
        public string? Subitem { get; set; }
        public string? Controle { get; set; }
        public string? Acao { get; set; }
        public string? Parametros { get; set; }

        public static string Montagem(Menu item)
        {  /* Montando o html com todas as regras das opções do Menu */
            return string.Concat(item.Principal, "|",
                                 item.Item, "|",
                                 item.Subitem, "|",
                                 item.Controle, "|",
                                 item.Acao, "|",
                                 item.Parametros);
        }

        public override bool Equals(object? obj)
        {
            if (obj != null)
            {
                if (obj is not Menu objAsPart)
                    return false;
                else
                    return Equals(objAsPart);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return MenuId;
        }

        public bool Equals(Menu? other)
        {
            if (other == null) return false;
            Type t = other.GetType();
            if (t.Equals(typeof(int))) return MenuId.Equals(other.MenuId);
            if (t.Equals(typeof(string))) return Principal != null ? Principal.Equals(other?.Principal) : false;
            return false;
        }
    }
}