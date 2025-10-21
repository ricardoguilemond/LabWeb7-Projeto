namespace BLL
{
    public class PartBLL : IEquatable<PartBLL>
    {
        public int PartId { get; set; }
        public string? PartName { get; set; }

        public override string ToString()
        {
            return "ID: " + PartId + "   Name: " + PartName;
        }

        public override bool Equals(object? obj)
        {
            return (obj is not PartBLL objAsPart) ? false : Equals(objAsPart);
        }

        public bool Equals(PartBLL? other)
        {
            return other == null ? false : PartId.Equals(other.PartId);
        }

        public override int GetHashCode()
        {
            return PartId;
        }

        // Atenção: Poderá usar override com os operadores == and !=
        /*
         * EXEMPLO DE USO: Criando uma lista de partes nomeadas

           List<Part> parts = new List<Part>();
           parts.Add(new Part() { PartName = "caixa", PartId = 1234 });
           parts.Add(new Part() { PartName = "canetas", PartId = 1334 });

           Console.WriteLine();
           foreach (Part aPart in parts)
           {
               Console.WriteLine(aPart);
           }
          *
          *
          */
    }
}