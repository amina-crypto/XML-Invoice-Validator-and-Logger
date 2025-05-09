using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    class ResultsDTO
    {
        public string InvoiceId { get; set; }
        public int Numero { get; set; } 
        public int CAP { get; set; }
        public string Nazione { get; set; } 
        public string Indirizzo { get; set; } 
        public string Comune { get; set; } 
        public int IDCodice { get; set; } 
        public string SendData { get; set; }
    }
}
