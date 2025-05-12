using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    public class InvoiceMatchDto
    {
        public string InvoiceId { get; set; }
        public int Numero { get; set; }
        public int CAP { get; set; }
        public int Nazione { get; set; }
        public int Indirizzo { get; set; }
        public int Comune { get; set; }
        public int IDCodice { get; set; }
        public string FolderName { get; set; }
    }
}
