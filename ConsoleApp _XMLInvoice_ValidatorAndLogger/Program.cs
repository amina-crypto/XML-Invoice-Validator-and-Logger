using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ask the user to enter the folder path
            Console.WriteLine("Enter the path to the folder containing XML files:");
            string folderPath = Console.ReadLine();

            // Check if path exists
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Invalid directory.");
                return;
            }

            // Ask the user to enter the XML file name
            Console.WriteLine("Enter the XML file name (with or without .xml extension):");
            string fileName = Console.ReadLine();

            if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".xml";
            }

            string filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            Console.WriteLine($"\nReading file: {fileName}");

            // Call the method to read XML nodes
            ReadXmlNodes(filePath);
            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }

        static void ReadXmlNodes(string filePath)
        {
            try
            {
                XDocument xmlDoc = XDocument.Load(filePath);

                // Debug: Check the root element
                Console.WriteLine($"Root element: {xmlDoc.Root.Name}");

                // Extract values without using the namespace by matching LocalName
                var fatturaElettronicaBody = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "FatturaElettronicaBody");
                if (fatturaElettronicaBody == null)
                {
                    Console.WriteLine("FatturaElettronicaBody not found. Check the XML structure.");
                    return;
                }

                // Extract Numero from DatiGeneraliDocumento
                string numero = fatturaElettronicaBody.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "DatiGenerali")?
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "DatiGeneraliDocumento")?
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "Numero")?.Value;

                // Extract CessionarioCommittente details
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

                // Display values
                Console.WriteLine("\nExtracted Values (CessionarioCommittente):");
                Console.WriteLine($"Numero: {numero ?? "Not found"}");
                Console.WriteLine($"Indirizzo: {indirizzoCessionario ?? "Not found"}");
                Console.WriteLine($"CAP: {capCessionario ?? "Not found"}");
                Console.WriteLine($"Comune: {comuneCessionario ?? "Not found"}");
                Console.WriteLine($"Provincia: {provinciaCessionario ?? "Not found"}");
                Console.WriteLine($"Nazione: {nazioneCessionario ?? "Not found"}");
                Console.WriteLine($"Denominazione: {denominazioneCessionario ?? "Not found"}");
                Console.WriteLine($"IdCodice: {idCodiceCessionario ?? "Not found"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }
    }
}