using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    class Program
    {
        // Buffers to hold logs until we confirm success or failure
        private static readonly List<string> SuccessLogBuffer = new List<string>();
        private static readonly List<string> ErrorLogBuffer = new List<string>();
        private static bool OperationSuccessful = true; // Flag to track success

        static void Main(string[] args)
        {
            // Configure sinks to buffer logs
            var successSink = new SuccessSink(SuccessLogBuffer);
            var errorSink = new ErrorSink(ErrorLogBuffer);

            // Success logger (writes to buffer)
            var successLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(successSink)
                .CreateLogger();

            // Error logger (writes to buffer)
            var errorLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(errorSink)
                .WriteTo.Console() // Optional: Keep console output for debugging
                .CreateLogger();

            // Set the global logger to the error logger for now
            Log.Logger = errorLogger;

            try
            {
                Log.Information("Application started.");
                successLogger.Information("Application started.");

                // Ask the user to enter the folder path
                Console.WriteLine("Enter the path to the folder containing XML files:");
                string folderPath = Console.ReadLine();
                Log.Information("User provided folder path: {FolderPath}", folderPath);
                successLogger.Information("User provided folder path: {FolderPath}", folderPath);

                // Check if path exists
                if (!Directory.Exists(folderPath))
                {
                    Log.Error("Invalid directory: {FolderPath}", folderPath);
                    Console.WriteLine("Invalid directory.");
                    OperationSuccessful = false;
                    return;
                }

                // Ask the user to enter the XML file name
                Console.WriteLine("Enter the XML file name (with or without .xml extension):");
                string fileName = Console.ReadLine();
                Log.Information("User provided file name: {FileName}", fileName);
                successLogger.Information("User provided file name: {FileName}", fileName);

                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".xml";
                }

                string filePath = Path.Combine(folderPath, fileName);
                Log.Information("Constructed file path: {FilePath}", filePath);
                successLogger.Information("Constructed file path: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    Log.Error("File not found: {FilePath}", filePath);
                    Console.WriteLine("File not found.");
                    OperationSuccessful = false;
                    return;
                }

                Log.Information("Reading file: {FileName}", fileName);
                successLogger.Information("Reading file: {FileName}", fileName);
                Console.WriteLine($"\nReading file: {fileName}");

                // Call the method to read XML nodes
                ReadXmlNodes(filePath, successLogger);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unexpected error in Main method.");
                Console.WriteLine($"Unexpected error: {ex.Message}");
                OperationSuccessful = false;
            }
            finally
            {
                Log.Information("Application ending.");
                successLogger.Information("Application ending.");

                // Write logs to the appropriate file based on success/failure
                if (OperationSuccessful)
                {
                    // Write success logs to file
                    using (var successFileLogger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.File("logs/success-log-.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger())
                    {
                        foreach (var logEntry in SuccessLogBuffer)
                        {
                            successFileLogger.Information(logEntry);
                        }
                    }
                }
                else
                {
                    // Write error logs to file
                    using (var errorFileLogger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.File("logs/error-log-.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger())
                    {
                        foreach (var logEntry in ErrorLogBuffer)
                        {
                            errorFileLogger.Information(logEntry);
                        }
                    }
                }

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                Log.CloseAndFlush();
            }
        }

        static void ReadXmlNodes(string filePath, ILogger successLogger)
        {
            try
            {
                Log.Information("Starting to process XML file: {FilePath}", filePath);
                successLogger.Information("Starting to process XML file: {FilePath}", filePath);
                XDocument xmlDoc = XDocument.Load(filePath);

                // Debug: Log the root element
                Log.Debug("Root element: {RootElement}", xmlDoc.Root.Name);
                successLogger.Debug("Root element: {RootElement}", xmlDoc.Root.Name);

                // Extract values without using the namespace by matching LocalName
                var fatturaElettronicaBody = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "FatturaElettronicaBody");
                if (fatturaElettronicaBody == null)
                {
                    Log.Error("FatturaElettronicaBody not found in {FilePath}. Check the XML structure.", filePath);
                    Console.WriteLine("FatturaElettronicaBody not found. Check the XML structure.");
                    OperationSuccessful = false;
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

                successLogger.Information("Extracted values from {FilePath}: Numero={Numero}, Denominazione={Denominazione}, IdCodice={IdCodice}, Indirizzo={Indirizzo}, CAP={CAP}, Comune={Comune}, Provincia={Provincia}, Nazione={Nazione}",
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
                OperationSuccessful = false;
            }
        }
    }

    // Custom sink to buffer success logs
    public class SuccessSink : ILogEventSink
    {
        private readonly List<string> _buffer;

        public SuccessSink(List<string> buffer)
        {
            _buffer = buffer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            _buffer.Add($"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{logEvent.Level}] {message}");
        }
    }

    // Custom sink to buffer error logs
    public class ErrorSink : ILogEventSink
    {
        private readonly List<string> _buffer;

        public ErrorSink(List<string> buffer)
        {
            _buffer = buffer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            _buffer.Add($"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{logEvent.Level}] {message}");
        }
    }
}