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
                ErrorLogger.Information("Starting to process XML file: {FilePath}", filePath);
                SuccessLogger.Information("Starting to process XML file: {FilePath}", filePath);
                XDocument xmlDoc = XDocument.Load(filePath);

                ErrorLogger.Debug("Root element: {RootElement}", xmlDoc.Root.Name);
                SuccessLogger.Debug("Root element: {RootElement}", xmlDoc.Root.Name);

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

                ErrorLogger.Information("Extracted values - Numero: {Numero} (Type: {TypeNumero}), Cap: {Cap} (Type: {TypeCap}), Nazione: {Nazione} (Type: {TypeNazione}), Indirizzo: {Indirizzo} (Type: {TypeIndirizzo}), Comune: {Comune} (Type: {TypeComune}), IdCodice: {IdCodice} (Type: {TypeIdCodice})",
                    numero ?? "Not found", numero?.GetType().Name ?? "null", capCessionario ?? "Not found", capCessionario?.GetType().Name ?? "null",
                    nazioneCessionario ?? "Not found", nazioneCessionario?.GetType().Name ?? "null", indirizzoCessionario ?? "Not found", indirizzoCessionario?.GetType().Name ?? "null",
                    comuneCessionario ?? "Not found", comuneCessionario?.GetType().Name ?? "null", idCodiceCessionario ?? "Not found", idCodiceCessionario?.GetType().Name ?? "null");

                SuccessLogger.Information("Extracted values from {FilePath}: Numero={Numero}, Denominazione={Denominazione}, IdCodice={IdCodice}, Indirizzo={Indirizzo}, CAP={CAP}, Comune={Comune}, Provincia={Provincia}, Nazione={Nazione}",
                    filePath, numero ?? "Not found", denominazioneCessionario ?? "Not found", idCodiceCessionario ?? "Not found",
                    indirizzoCessionario ?? "Not found", capCessionario ?? "Not found", comuneCessionario ?? "Not found",
                    provinciaCessionario ?? "Not found", nazioneCessionario ?? "Not found");

                Console.WriteLine("\nExtracted Values (CessionarioCommittente):");
                Console.WriteLine($"Numero: {numero ?? "Not found"}");
                Console.WriteLine($"Indirizzo: {indirizzoCessionario ?? "Not found"}");
                Console.WriteLine($"CAP: {capCessionario ?? "Not found"}");
                Console.WriteLine($"Comune: {comuneCessionario ?? "Not found"}");
                Console.WriteLine($"Provincia: {provinciaCessionario ?? "Not found"}");
                Console.WriteLine($"Nazione: {nazioneCessionario ?? "Not found"}");
                Console.WriteLine($"Denominazione: {denominazioneCessionario ?? "Not found"}");
                Console.WriteLine($"IdCodice: {idCodiceCessionario ?? "Not found"}");

                data["Numero"] = numero;
                data["IdCodice"] = idCodiceCessionario;
                data["CAP"] = capCessionario;
                data["Nazione"] = nazioneCessionario;
                data["Indirizzo"] = indirizzoCessionario;
                data["Comune"] = comuneCessionario;
                data["FileName"] = fileName;

                if (string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(capCessionario) || string.IsNullOrEmpty(nazioneCessionario) ||
                    string.IsNullOrEmpty(indirizzoCessionario) || string.IsNullOrEmpty(comuneCessionario) || string.IsNullOrEmpty(idCodiceCessionario))
                {
                    ErrorLogger.Error("One or more required XML values are missing. Cannot proceed.");
                    Console.WriteLine("One or more required XML values are missing. Cannot proceed.");
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
