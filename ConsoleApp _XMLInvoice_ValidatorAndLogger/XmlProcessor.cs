using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    public class XmlProcessor : LoggableOperation
    {
        public (bool Success, Dictionary<string, string> Data) ReadXmlNodes(string filePath, string fileName)
        {
            var data = new Dictionary<string, string>();
            try
            {
                ErrorLogger.Information("Starting to process XML file...");
                SuccessLogger.Information("Starting to process XML file...");
                XDocument xmlDoc = XDocument.Load(filePath);

                var fatturaElettronicaBody = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "FatturaElettronicaBody");
                if (fatturaElettronicaBody == null)
                {
                    ErrorLogger.Error("FatturaElettronicaBody not found in {FilePath}. Check the XML structure.", filePath);
                    OperationSuccessful = false;
                    return (false, data);
                }

                string numero = fatturaElettronicaBody.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "DatiGenerali")?
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "DatiGeneraliDocumento")?
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Numero")?.Value;

                var cessionario = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "CessionarioCommittente");
                var datiAnagraficiCessionario = cessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "DatiAnagrafici");
                string idCodiceCessionario = datiAnagraficiCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "IdFiscaleIVA")?
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "IdCodice")?.Value;
                string denominazioneCessionario = datiAnagraficiCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Anagrafica")?
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Denominazione")?.Value;

                var sedeCessionario = cessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Sede");
                string indirizzoCessionario = sedeCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Indirizzo")?.Value;
                string capCessionario = sedeCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "CAP")?.Value;
                string comuneCessionario = sedeCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Comune")?.Value;
                string provinciaCessionario = sedeCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Provincia")?.Value;
                string nazioneCessionario = sedeCessionario?.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Nazione")?.Value;

                data["Numero"] = numero;
                data["IdCodice"] = idCodiceCessionario;
                data["CAP"] = capCessionario;
                data["Denominazione"] = denominazioneCessionario;
                data["Nazione"] = nazioneCessionario;
                data["Indirizzo"] = indirizzoCessionario;
                data["Comune"] = comuneCessionario;
                //data["FileName"] = fileName;
                //TechnicalSuccessLogger.Information("Numero "data);


                if (string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(capCessionario) || string.IsNullOrEmpty(nazioneCessionario) ||
                    string.IsNullOrEmpty(indirizzoCessionario) || string.IsNullOrEmpty(comuneCessionario) || string.IsNullOrEmpty(idCodiceCessionario))
                {
                    ErrorLogger.Error("One or more required XML values are missing. Cannot proceed.");
                    OperationSuccessful = false;
                    return (false, data);
                }

                return (true, data);
            }
            catch (Exception ex)
            {
                ErrorLogger.Error(ex, "Error processing file {FilePath}", filePath);
                OperationSuccessful = false;
                return (false, data);
            }
        }
    }
}
