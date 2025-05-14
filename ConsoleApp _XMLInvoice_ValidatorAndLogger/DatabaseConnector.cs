using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Oracle.ManagedDataAccess.Client;


namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    public class DatabaseConnector : LoggableOperation
    {
        private readonly string ConnectionString;
        //private readonly string ConnectionString =
        //    "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=whitenetx-db.randstaditaly.it)(PORT=1524))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=WN12PROD)));User Id=S2N;Password=g_Fb34gDSfqmfNs_2;";
        public DatabaseConnector()
        {
            // Read the connection string from app.config
            ConnectionString = ConfigurationManager.ConnectionStrings["Production"]?.ConnectionString;
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new ConfigurationErrorsException("Connection string 'Production' not found in app.config.");
            }
        }
        public bool Connect()
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    ErrorLogger.Information("Successfully connected to Oracle database.");
                    SuccessLogger.Information("Successfully connected to Oracle database.");
                    TechnicalSuccessLogger.Information("Successfully connected to Oracle database. [Information] Successfully connected to Oracle database.");
                    TechnicalErrorLogger.Information("Successfully connected to Oracle database. [Information] Successfully connected to Oracle database.");
                    return true;
                }
            }
            catch (OracleException ex)
            {
                ErrorLogger.Error(ex, "Oracle database connection error: {ErrorMessage}", ex.Message);
                TechnicalErrorLogger.Error(ex, "Oracle database connection error at {Time} [Error] Oracle database connection error: \"{ErrorMessage}\"", DateTime.Now, ex.Message);
            
                OperationSuccessful = false;
                return false;
            }
            catch (Exception ex)
            {
                ErrorLogger.Error(ex, "Unexpected error connecting to database.");
                TechnicalErrorLogger.Error(ex, "Unexpected error connecting to database at {Time} [Error] Unexpected error connecting to database.", DateTime.Now);
               
                OperationSuccessful = false;
                return false;
            }
        }

        public (bool Success, InvoiceMatchDto Result) ExecuteQuery(Dictionary<string, string> xmlData)
        {
            string numero = null;
            string idCodiceCessionario = null;
            string capCessionario = null;
            string denominazione = null;
            string nazioneCessionario = null;
            string indirizzoCessionario = null;
            string comuneCessionario = null;
            string xmlFilePattern = null;

            try
            {
                numero = xmlData["Numero"]?.Trim();
                idCodiceCessionario = xmlData["IdCodice"]?.Trim();
                capCessionario = xmlData["CAP"];
                denominazione = xmlData["Denominazione"];
                nazioneCessionario = xmlData["Nazione"];
                indirizzoCessionario = xmlData["Indirizzo"];
                comuneCessionario = xmlData["Comune"];
                xmlFilePattern = xmlData["FileName"];
                string fullPattern = $"%{xmlFilePattern}%";
                string numeroLastThree = numero?.Length >= 3 ? numero.Substring(numero.Length - 3) : numero;
                int numeroParsed;
                if (string.IsNullOrEmpty(numeroLastThree) || !int.TryParse(numeroLastThree, out numeroParsed))
                {
                    ErrorLogger.Error("Invalid number format for Numero (last 3 digits): {Numero}", numeroLastThree);
                    TechnicalErrorLogger.Error("Invalid number format for Numero (last 3 digits): {Numero} at {Time} [Error] Invalid number format for field 'Numero'.", numeroLastThree, DateTime.Now);
                    OperationSuccessful = false;
                    return (false, null);
                }
                if (!decimal.TryParse(idCodiceCessionario, out decimal idCodiceParsed))
                {
                    ErrorLogger.Error("Invalid number format for IdCodice: {IdCodice}", idCodiceCessionario);
                    TechnicalErrorLogger.Error("Invalid number format for IdCodice: {IdCodice} at {Time} [Error] Invalid number format for field 'IdCodice'.", idCodiceCessionario, DateTime.Now);
                    OperationSuccessful = false;
                    return (false, null);
                }

                var parameters = new
                {
                    numero = numeroParsed,
                    cap = capCessionario,
                    nazione = nazioneCessionario,
                    indirizzo = indirizzoCessionario,
                    comune = comuneCessionario,
                    idCodice = idCodiceParsed,
                    denominazione = denominazione,
                    xmlFilePattern = fullPattern
                };

                TechnicalSuccessLogger.Information("Executing query with parameters: Numero={Numero}, CAP={CAP}, Nazione={Nazione}, Indirizzo={Indirizzo}, Comune={Comune}, IDCodice={IDCodice}, xmlFilePattern={xmlFilePattern} [Information] Executing query with parameters: Numero=\"{Numero}\", CAP=\"{CAP}\", Nazione=\"{Nazione}\", Indirizzo=\"{Indirizzo}\", Comune=\"{Comune}\", IDCodice=\"{IDCodice}\", xmlFilePattern=\"{xmlFilePattern}\"", numero, capCessionario, nazioneCessionario, indirizzoCessionario, comuneCessionario, idCodiceCessionario, xmlFilePattern);
                TechnicalErrorLogger.Information("Executing query with parameters: Numero={Numero}, CAP={CAP}, Nazione={Nazione}, Indirizzo={Indirizzo}, Comune={Comune}, IDCodice={IDCodice}, xmlFilePattern={xmlFilePattern} [Information] Executing query with parameters: Numero=\"{Numero}\", CAP=\"{CAP}\", Nazione=\"{Nazione}\", Indirizzo=\"{Indirizzo}\", Comune=\"{Comune}\", IDCodice=\"{IDCodice}\", xmlFilePattern=\"{xmlFilePattern}\"", numero, capCessionario, nazioneCessionario, indirizzoCessionario, comuneCessionario, idCodiceCessionario, xmlFilePattern);

                string query = @"
                    SELECT 
                        CASE WHEN i.invoice_number = :numero THEN 1 ELSE 0 END AS Numero,
                        CASE WHEN i.zip_code_invo = :cap THEN 1 ELSE 0 END AS CAP,
                        CASE WHEN rc.iso_cod_invo = :nazione THEN 1 ELSE 0 END AS Nazione,
                        CASE WHEN i.address_legal = :indirizzo THEN 1 ELSE 0 END AS Indirizzo,
                        CASE WHEN i.des_municipality_invo = :comune THEN 1 ELSE 0 END AS Comune,
                        CASE WHEN i.company_name = :denominazione THEN 1 ELSE 0 END AS Denominazione,
                        CASE WHEN i.fiscal_identity_code = :idCodice THEN 1 ELSE 0 END AS IDCodice,
                        ish.send_data AS FolderName
                    FROM 
                        INVO_SEND_HISTORY ish
                        INNER JOIN INVOICE i ON ish.invoice_id = i.invoice_id
                        INNER JOIN REFE_COUNTRY rc ON rc.refe_country_id = i.refe_country_invo_id
                    WHERE 
                        ish.send_data LIKE :xmlFilePattern
                        AND i.invoice_id = i.invoice_id 
                        AND rc.refe_country_id = i.refe_country_invo_id";

                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    TechnicalSuccessLogger.Information("Successfully opened connection for invoice validation.");
                    TechnicalErrorLogger.Information("Successfully opened connection for invoice validation.");
                    try
                    {
                        var result = connection.Query<InvoiceMatchDto>(query, parameters).ToList().SingleOrDefault();
                        if (result == null)
                        {
                            ErrorLogger.Error("No matching record found in the database for the given criteria.");
                            TechnicalErrorLogger.Error("No matching record found in the database for the given criteria at {Time} [Error] No matching records found in the database.", DateTime.Now);
                            OperationSuccessful = false;
                            return (false, null);
                        }

                        Console.WriteLine("\nQuery Results (Match Status):");
                        Console.WriteLine($"Numero Match: {(result.Numero == 1 ? "True" : "False")}");
                        Console.WriteLine($"CAP Match: {(result.CAP == 1 ? "True" : "False")}");
                        Console.WriteLine($"Nazione Match: {(result.Nazione == 1 ? "True" : "False")}");
                        Console.WriteLine($"Indirizzo Match: {(result.Indirizzo == 1 ? "True" : "False")}");
                        Console.WriteLine($"Comune Match: {(result.Comune == 1 ? "True" : "False")}");
                        Console.WriteLine($"Denominazione Match: {(result.Denominazione == 1 ? "True" : "False")}");
                        Console.WriteLine($"IDCodice Match: {(result.IDCodice == 1 ? "True" : "False")}");
                        Console.WriteLine($"Folder Name: {result.FolderName}");
                        //Scrive su technical logger 
                        TechnicalSuccessLogger.Information("\nQuery Results (Match Status):");
                        TechnicalSuccessLogger.Information("Numero Match: {Match}", result.Numero == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("CAP Match: {Match}", result.CAP == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("Nazione Match: {Match}", result.Nazione == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("Indirizzo Match: {Match}", result.Indirizzo == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("Comune Match: {Match}", result.Comune == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("Denominazione Match: {Match}", result.Denominazione == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("IDCodice Match: {Match}", result.IDCodice == 1 ? "True" : "False");
                        TechnicalSuccessLogger.Information("FolderName: {FolderName}", result.FolderName);


                        // Log XML nodes from ReadXmlNodes method (contained in xmlData)
                        var xmlNodesLog = "XML Nodes Read:\n" +
                                          $"Numero: {xmlData["Numero"] ?? "null"}\n" +
                                          $"IdCodice: {xmlData["IdCodice"] ?? "null"}\n" +
                                          $"CAP: {xmlData["CAP"] ?? "null"}\n" +
                                          $"Denominazione: {xmlData["Denominazione"] ?? "null"}\n" +
                                          $"Nazione: {xmlData["Nazione"] ?? "null"}\n" +
                                          $"Indirizzo: {xmlData["Indirizzo"] ?? "null"}\n" +
                                          $"Comune: {xmlData["Comune"] ?? "null"}\n" +
                                          $"FileName: {xmlData["FileName"] ?? "null"}";

                        // Log XML nodes followed by query results (same as console output)
                        TechnicalSuccessLogger.Information("{0}\n\nQuery Results (Match Status):\nNumero Match: {(result.Numero == 1 ? \"True\" : \"False\")}\nCAP Match: {(result.CAP == 1 ? \"True\" : \"False\")}\nNazione Match: {(result.Nazione == 1 ? \"True\" : \"False\")}\nIndirizzo Match: {(result.Indirizzo == 1 ? \"True\" : \"False\")}\nComune Match: {(result.Comune == 1 ? \"True\" : \"False\")}\nDenominazione Match: {(result.Denominazione == 1 ? \"True\" : \"False\")}\nIDCodice Match: {(result.IDCodice == 1 ? \"True\" : \"False\")}\nFolder Name: {result.FolderName}", xmlNodesLog, result.Numero, result.CAP, result.Nazione, result.Indirizzo, result.Comune, result.Denominazione, result.IDCodice, result.FolderName ?? "null");

                        return (true, result);
                    }
                    catch (OracleException ex)
                    {
                        ErrorLogger.Error(ex, "Oracle error during query execution: {ErrorMessage}", ex.Message);
                        TechnicalErrorLogger.Error(ex, "Oracle database query error at {Time} [Error] Oracle database query error: \"{ErrorMessage}\"", DateTime.Now, ex.Message);
                        OperationSuccessful = false;
                        return (false, null);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Error(ex, "Error executing database query.");
                TechnicalErrorLogger.Error(ex, "Error executing database query at {Time} [Error] Unexpected error executing database query.", DateTime.Now);
                OperationSuccessful = false;
                return (false, null);
            }
        }
    }
}