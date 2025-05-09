using System;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using Dapper;

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

    class Program
    {
        private static readonly List<string> SuccessLogBuffer = new List<string>();
        private static readonly List<string> ErrorLogBuffer = new List<string>();
        private static bool OperationSuccessful = true;
        private static readonly string ConnectionString =
            "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=whitenetx-db.randstaditaly.it)(PORT=1524))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=WN12PROD)));User Id=S2N;Password=g_Fb34gDSfqmfNs_2;";

        static void Main(string[] args)
        {
            var successSink = new SuccessSink(SuccessLogBuffer);
            var errorSink = new ErrorSink(ErrorLogBuffer);

            var successLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(successSink)
                .CreateLogger();

            var errorLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(errorSink)
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger = errorLogger;

            try
            {
                Log.Information("Application started.");
                successLogger.Information("Application started.");
                ConnectToDatabase(successLogger);

                Console.WriteLine("Enter the path to the folder containing XML files:");
                string folderPath = Console.ReadLine();
                Log.Information("User provided folder path: {FolderPath}", folderPath);
                successLogger.Information("User provided folder path: {FolderPath}", folderPath);

                if (!Directory.Exists(folderPath))
                {
                    Log.Error("Invalid directory: {FolderPath}", folderPath);
                    OperationSuccessful = false;
                    return;
                }

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

                ReadXmlNodes(filePath, fileName, successLogger); // Pass fileName to ReadXmlNodes
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unexpected error in Main method.");
                OperationSuccessful = false;
            }
            finally
            {
                Log.Information("Application ending.");
                successLogger.Information("Application ending.");

                if (OperationSuccessful)
                {
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

        static void ReadXmlNodes(string filePath, string fileName, ILogger successLogger)
        {
            try
            {
                Log.Information("Starting to process XML file: {FilePath}", filePath);
                successLogger.Information("Starting to process XML file: {FilePath}", filePath);
                XDocument xmlDoc = XDocument.Load(filePath);

                Log.Debug("Root element: {RootElement}", xmlDoc.Root.Name);
                successLogger.Debug("Root element: {RootElement}", xmlDoc.Root.Name);

                var fatturaElettronicaBody = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "FatturaElettronicaBody");
                if (fatturaElettronicaBody == null)
                {
                    Log.Error("FatturaElettronicaBody not found in {FilePath}. Check the XML structure.", filePath);
                    OperationSuccessful = false;
                    return;
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

                // Debug logging for extracted values and their types
                Log.Information("Extracted values - Numero: {Numero} (Type: {TypeNumero}), Cap: {Cap} (Type: {TypeCap}), Nazione: {Nazione} (Type: {TypeNazione}), Indirizzo: {Indirizzo} (Type: {TypeIndirizzo}), Comune: {Comune} (Type: {TypeComune}), IdCodice: {IdCodice} (Type: {TypeIdCodice})",
                    numero ?? "Not found", numero?.GetType().Name ?? "null", capCessionario ?? "Not found", capCessionario?.GetType().Name ?? "null",
                    nazioneCessionario ?? "Not found", nazioneCessionario?.GetType().Name ?? "null", indirizzoCessionario ?? "Not found", indirizzoCessionario?.GetType().Name ?? "null",
                    comuneCessionario ?? "Not found", comuneCessionario?.GetType().Name ?? "null", idCodiceCessionario ?? "Not found", idCodiceCessionario?.GetType().Name ?? "null");

                successLogger.Information("Extracted values from {FilePath}: Numero={Numero}, Denominazione={Denominazione}, IdCodice={IdCodice}, Indirizzo={Indirizzo}, CAP={CAP}, Comune={Comune}, Provincia={Provincia}, Nazione={Nazione}",
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

                string xmlFilePattern = fileName;
                string fullPattern = xmlFilePattern; // Pre-concatenate wildcards

                if (string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(capCessionario) || string.IsNullOrEmpty(nazioneCessionario) ||
                    string.IsNullOrEmpty(indirizzoCessionario) || string.IsNullOrEmpty(comuneCessionario) || string.IsNullOrEmpty(idCodiceCessionario))
                {
                    Log.Error("One or more required XML values are missing. Cannot execute query.");
                    Console.WriteLine("One or more required XML values are missing. Cannot execute query.");
                    OperationSuccessful = false;
                    return;
                }

                // Explicitly convert parameters to match database types if needed
                var parameters = new
                {
                    numero = numero, // Ensure string type matches database
                    cap = capCessionario, // Ensure string type matches database
                    nazione = nazioneCessionario, // Ensure string type matches database
                    indirizzo = indirizzoCessionario, // Ensure string type matches database
                    comune = comuneCessionario, // Ensure string type matches database
                    idCodice = idCodiceCessionario, // Ensure string type matches database
                    xmlFilePattern = fullPattern // Ensure string type for LIKE
                };

                // Debug log for parameters
                Log.Information("Parameters - numero: {Numero}, cap: {Cap}, nazione: {Nazione}, indirizzo: {Indirizzo}, comune: {Comune}, idCodice: {IdCodice}, xmlFilePattern: {XmlFilePattern}",
                    parameters.numero, parameters.cap, parameters.nazione, parameters.indirizzo, parameters.comune, parameters.idCodice, parameters.xmlFilePattern);

                string query = @"
            SELECT 
                 i.invoice_id,
                CASE WHEN i.invoice_number = 280 THEN 1 ELSE 0 END AS Numero,
                CASE WHEN i.zip_code_invo = 42013 THEN 1 ELSE 0 END AS CAP,
                CASE WHEN rc.iso_cod_invo = 'IT' THEN 1 ELSE 0 END AS Nazione,
                CASE WHEN i.address_legal = 'VIA STATALE 467 45' THEN 1 ELSE 0 END AS Indirizzo,
                CASE WHEN i.des_municipality_invo = 'CASALGRANDE' THEN 1 ELSE 0 END AS Comune,
                CASE WHEN i.fiscal_identity_code = 00133450353 THEN 1 ELSE 0 END AS IDCodice,
                ish.send_data AS Folder_Name
            FROM 
                INVO_SEND_HISTORY ish
                INNER JOIN INVOICE i ON ish.invoice_id = i.invoice_id
                INNER JOIN REFE_COUNTRY rc ON rc.refe_country_id = i.refe_country_invo_id
            WHERE 
                 ish.send_data LIKE '%IT12730090151_DI%'--emri i file XML
                AND i.invoice_id = i.invoice_id 
                AND rc.refe_country_id = i.refe_country_invo_id";

                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    Log.Information("Executing query with parameters: Numero={Numero}, CAP={CAP}, Nazione={Nazione}, Indirizzo={Indirizzo}, Comune={Comune}, IDCodice={IDCodice}, xmlFilePattern={xmlFilePattern}",
                        parameters.numero, parameters.cap, parameters.nazione, parameters.indirizzo, parameters.comune, parameters.idCodice, parameters.xmlFilePattern);
                    successLogger.Information("Executing query with parameters: Numero={Numero}, CAP={CAP}, Nazione={Nazione}, Indirizzo={Indirizzo}, Comune={Comune}, IDCodice={IDCodice}, xmlFilePattern={xmlFilePattern}",
                        parameters.numero, parameters.cap, parameters.nazione, parameters.indirizzo, parameters.comune, parameters.idCodice, parameters.xmlFilePattern);

                    try
                    {
                        var result = connection.Query<InvoiceMatchDto>(query).ToList().SingleOrDefault();
                        if (result == null)
                        {
                            Log.Error("No matching record found in the database for the given criteria.");
                            Console.WriteLine("No matching record found in the database.");
                            OperationSuccessful = false;
                            return;
                        }

                        // Log and display the results
                        Log.Information("Query results: Numero={Numero}, CAP={CAP}, Nazione={Nazione}, Indirizzo={Indirizzo}, Comune={Comune}, IDCodice={IDCodice}, FolderName={FolderName}",
                            result.Numero, result.CAP, result.Nazione, result.Indirizzo, result.Comune, result.IDCodice, result.FolderName);
                        successLogger.Information("Query results: Numero={Numero}, CAP={CAP}, Nazione={Nazione}, Indirizzo={Indirizzo}, Comune={Comune}, IDCodice={IDCodice}, FolderName={FolderName}",
                            result.Numero, result.CAP, result.Nazione, result.Indirizzo, result.Comune, result.IDCodice, result.FolderName);

                        Console.WriteLine("\nQuery Results (Match Status):");
                        Console.WriteLine($"Numero Match: {(result.Numero == 1 ? "True" : "False")}");
                        Console.WriteLine($"CAP Match: {(result.CAP == 1 ? "True" : "False")}");
                        Console.WriteLine($"Nazione Match: {(result.Nazione == 1 ? "True" : "False")}");
                        Console.WriteLine($"Indirizzo Match: {(result.Indirizzo == 1 ? "True" : "False")}");
                        Console.WriteLine($"Comune Match: {(result.Comune == 1 ? "True" : "False")}");
                        Console.WriteLine($"IDCodice Match: {(result.IDCodice == 1 ? "True" : "False")}");
                        Console.WriteLine($"Folder Name: {result.FolderName}");
                    }
                    catch (OracleException ex)
                    {
                        Log.Error(ex, "Oracle error during query execution: {ErrorMessage}", ex.Message);
                        OperationSuccessful = false;
                        throw; // Re-throw to see the full stack trace
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing file {FilePath}", filePath);
                OperationSuccessful = false;
            }
        }

        static void ConnectToDatabase(ILogger successLogger)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    Log.Information("Successfully connected to Oracle database.");
                    successLogger.Information("Successfully connected to Oracle database.");
                }
            }
            catch (OracleException ex)
            {
                Log.Error(ex, "Oracle database connection error: {ErrorMessage}", ex.Message);
                OperationSuccessful = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error connecting to database.");
                OperationSuccessful = false;
            }
        }
    }

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
            _buffer.Add($"{logEvent.MessageTemplate} [{logEvent.Level}] {message}");
        }
    }

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
            _buffer.Add($"{logEvent.MessageTemplate} [{logEvent.Level}] {message}");
        }
    }
}