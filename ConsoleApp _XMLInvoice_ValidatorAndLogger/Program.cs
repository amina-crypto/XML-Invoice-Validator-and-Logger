using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Serilog;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Application started.");

                // Ask the user to enter the folder path
                Console.WriteLine("Enter the path to the folder containing XML files:");
                string folderPath = Console.ReadLine();
                Log.Information("User provided folder path: {FolderPath}", folderPath);

                // Check if path exists
                if (!Directory.Exists(folderPath))
                {
                    Log.Error("Invalid directory: {FolderPath}", folderPath);
                    Console.WriteLine("Invalid directory.");
                    return;
                }

                // Ask the user to enter the XML file name
                Console.WriteLine("Enter the XML file name (with or without .xml extension):");
                string fileName = Console.ReadLine();
                Log.Information("User provided file name: {FileName}", fileName);

                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".xml";
                }

                string filePath = Path.Combine(folderPath, fileName);
                Log.Information("Constructed file path: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    Log.Error("File not found: {FilePath}", filePath);
                    Console.WriteLine("File not found.");
                    return;
                }

                Log.Information("Reading file: {FileName}", fileName);
                Console.WriteLine($"\nReading file: {fileName}");

                // Call the method to read XML nodes
                ReadXmlNodes(filePath);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unexpected error in Main method.");
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
            finally
            {
                Log.Information("Application ending.");
                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                Log.CloseAndFlush(); // Ensure all logs are written
            }
        }

        static void ReadXmlNodes(string filePath)
        {
            try
            {
                Log.Information("Starting to process XML file: {FilePath}", filePath);
                XDocument xmlDoc = XDocument.Load(filePath);

                // Debug: Log the root element
                Console.WriteLine($"Root element: {xmlDoc.Root.Name}");
                Log.Debug("Root element: {RootElement}", xmlDoc.Root.Name);

                // Extract values without using the namespace by matching LocalName
                var fatturaElettronicaBody = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "FatturaElettronicaBody");
                if (fatturaElettronicaBody == null)
                {
                    Log.Error("FatturaElettronicaBody not found in {FilePath}. Check the XML structure.", filePath);
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

                // Log extracted values
                Log.Information("Extracted values from {FilePath}: Numero={Numero}, Denominazione={Denominazione}, IdCodice={IdCodice}, Indirizzo={Indirizzo}, CAP={CAP}, Comune={Comune}, Provincia={Provincia}, Nazione={Nazione}",
                    filePath, numero ?? "Not found", denominazioneCessionario ?? "Not found", idCodiceCessionario ?? "Not found",
                    indirizzoCessionario ?? "Not found", capCessionario ?? "Not found", comuneCessionario ?? "Not found",
                    provinciaCessionario ?? "Not found", nazioneCessionario ?? "Not found");

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
                Log.Error(ex, "Error processing file {FilePath}", filePath);
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }
    }
}